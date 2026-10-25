using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public interface ICostoProductoService
{
    Task<ResultadoCosteo<CostoProductoDto>> CalcularAsync(
        int cuentaId, int productoId,
        List<InsumoDirectoDto>? insumosDirectos = null,
        List<int>? costosFijosIds = null);

    Task<ResultadoCosteo<CostoProductoDto>> CalcularPreviewAsync(
        int cuentaId, PreviewCostoProductoDto dto);

    // Calcula el costo de varios productos con una sola carga de datos.
    // Evita el N+1 del listado de menú y de los reportes que lo consumen.
    Task<Dictionary<int, CostoProductoDto>> CalcularLoteAsync(
        int negocioId, IReadOnlyCollection<ProductoMenu> productos,
        List<int>? costosFijosIds = null);
}

public sealed class CostoProductoService : ICostoProductoService
{
    private readonly IProductoMenuRepository _productos;
    private readonly ICosteoRepository _costeo;
    private readonly IInsumoRepository _insumos;

    public CostoProductoService(
        IProductoMenuRepository productos,
        ICosteoRepository costeo,
        IInsumoRepository insumos)
    {
        _productos = productos;
        _costeo = costeo;
        _insumos = insumos;
    }

    public async Task<ResultadoCosteo<CostoProductoDto>> CalcularAsync(
        int cuentaId, int productoId,
        List<InsumoDirectoDto>? insumosDirectos = null,
        List<int>? costosFijosIds = null)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<CostoProductoDto>.NoHallado("Aún no tienes un negocio registrado.");

        var producto = await _productos.ObtenerPorIdAsync(negocioId.Value, productoId);
        if (producto is null)
            return ResultadoCosteo<CostoProductoDto>.NoHallado("No se encontró el producto solicitado.");

        var recetas = producto.ProductoMenuReceta
            .Select(pr => new RecetaEnProductoDto { RecetaId = pr.RecetaId, Cantidad = pr.Cantidad })
            .ToList();

        // Si no llegan insumos directos explícitos (preview), se usan los
        // empaques guardados del producto.
        var directos = insumosDirectos ?? producto.ProductoMenuInsumos
            .Select(pi => new InsumoDirectoDto
            {
                InsumoId = pi.InsumoId,
                Cantidad = pi.Cantidad,
                UnidadMedida = pi.UnidadMedida
            })
            .ToList();

        return await CalcularNucleoAsync(
            negocioId.Value, producto.Id, producto.Nombre, producto.PrecioVenta,
            recetas, directos, costosFijosIds);
    }

    public async Task<ResultadoCosteo<CostoProductoDto>> CalcularPreviewAsync(
        int cuentaId, PreviewCostoProductoDto dto)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<CostoProductoDto>.NoHallado("Aún no tienes un negocio registrado.");

        var error = ValidarComposicion(dto.Recetas, dto.InsumosDirectos);
        if (error is not null)
            return ResultadoCosteo<CostoProductoDto>.Error(error);

        return await CalcularNucleoAsync(
            negocioId.Value, 0, "(vista previa)", null,
            dto.Recetas, dto.InsumosDirectos, dto.CostosFijosIds);
    }

    // ---------- Núcleo reutilizable ----------
    // Por cada receta: (costo de insumos de la receta ÷ porciones base) × porciones
    // del producto. Insumos directos (empaques): se costean con proveedor, sin merma de receta.
    // costoTotalProducto = Σ (costoPorPorcion × porciones) + Σ (costo insumo directo) + fijos prorrateados
    private async Task<ResultadoCosteo<CostoProductoDto>> CalcularNucleoAsync(
        int negocioId, int productoId, string nombre, decimal? precioVenta,
        List<RecetaEnProductoDto> recetas, List<InsumoDirectoDto> directos,
        List<int>? costosFijosIds)
    {
        if (recetas.Count == 0)
            return ResultadoCosteo<CostoProductoDto>.Error("El producto necesita al menos una receta.");

        // Carga recetas con sus insumos.
        var mapaRecetas = (await _productos.ObtenerRecetasConInsumosAsync(
                negocioId, recetas.Select(r => r.RecetaId).Distinct()))
            .ToDictionary(r => r.Id);

        // Todos los insumos involucrados (de recetas + directos) para una sola consulta de proveedor principal.
        var idsDirectos = directos.Select(d => d.InsumoId).ToList();
        var idsDeRecetas = mapaRecetas.Values.SelectMany(r => r.RecetaInsumos.Select(ri => ri.InsumoId)).ToList();
        var todosIds = idsDirectos.Concat(idsDeRecetas).Distinct().ToList();
        var principales = await _costeo.ObtenerCostosProveedorPrincipalAsync(todosIds);

        var mapaDirectos = idsDirectos.Count == 0
            ? new Dictionary<int, Insumo>()
            : (await _productos.ObtenerInsumosActivosAsync(negocioId, idsDirectos.Distinct()))
                .ToDictionary(i => i.Id);

        var fijosConfigurados = await _costeo.ListarCostosFijosAsync(negocioId, soloActivos: true);
        var advertenciasFijos = new List<string>();
        var fijosAplicados = fijosConfigurados.Count == 0
            ? new List<CostoFijo>()
            : SeleccionarFijos(fijosConfigurados, costosFijosIds, advertenciasFijos);

        var dto = CalcularNucleo(
            productoId, nombre, precioVenta, recetas, directos,
            mapaRecetas, principales, mapaDirectos,
            fijosConfigurados, fijosAplicados, advertenciasFijos);

        return dto is null
            ? ResultadoCosteo<CostoProductoDto>.Error("El producto necesita al menos una receta.")
            : ResultadoCosteo<CostoProductoDto>.Ok(dto);
    }

    // Cálculo síncrono con los datos ya precargados. Se usa tanto para un
    // producto como para el lote del listado de menú.
    private static CostoProductoDto? CalcularNucleo(
        int productoId, string nombre, decimal? precioVenta,
        List<RecetaEnProductoDto> recetas, List<InsumoDirectoDto> directos,
        Dictionary<int, Receta> mapaRecetas,
        Dictionary<int, PrecioProveedorPrincipal> principales,
        Dictionary<int, Insumo> mapaDirectos,
        List<CostoFijo> fijosConfigurados,
        List<CostoFijo> fijosAplicados,
        IReadOnlyList<string> advertenciasFijos)
    {
        if (recetas.Count == 0) return null;

        var dto = new CostoProductoDto
        {
            ProductoId = productoId,
            Producto = nombre,
            PrecioVenta = precioVenta
        };
        dto.Advertencias.AddRange(advertenciasFijos);

        // 1) Recetas: cada una a costo total HU8.1 (sin fijos aquí).
        // Además se acumulan las preparaciones equivalentes que consume el producto:
        // una receta completa = 1 preparación; media receta = 0,5 preparaciones.
        decimal preparacionesEquivalentes = 0;
        foreach (var item in recetas)
        {
            if (!mapaRecetas.TryGetValue(item.RecetaId, out var receta))
            {
                dto.Advertencias.Add($"La receta #{item.RecetaId} no existe en tu negocio o está inactiva. No se costeó.");
                continue;
            }

            var costoRecetaSinFijos = CostoRecetaSinFijos(receta, principales, dto.Advertencias);
            if (receta.PorcionesBase <= 0)
            {
                dto.Advertencias.Add($"La receta '{receta.Nombre}' no tiene porciones base; no se pudo costear.");
                continue;
            }

            // Cantidad = porciones de esta receta dentro del producto.
            var costoPorPorcion = costoRecetaSinFijos / receta.PorcionesBase;
            preparacionesEquivalentes += item.Cantidad / receta.PorcionesBase;
            var aportado = decimal.Round(costoPorPorcion * item.Cantidad, 2);
            dto.Recetas.Add(new RecetaDetalleProductoDto
            {
                RecetaId = receta.Id,
                Nombre = receta.Nombre,
                Cantidad = item.Cantidad,
                CostoUnitario = decimal.Round(costoPorPorcion, 2),
                CostoAportado = aportado
            });
        }

        // 2) Insumos directos (empaques): mismo tratamiento que HU10.3.
        foreach (var d in directos)
        {
            if (!mapaDirectos.TryGetValue(d.InsumoId, out var insumo))
            {
                dto.Advertencias.Add($"El insumo directo #{d.InsumoId} no existe en tu negocio o está inactivo. No se costeó.");
                continue;
            }

            var unidad = d.UnidadMedida.Trim().ToLowerInvariant();
            if (!ConversorUnidades.Compatibles(unidad, insumo.UnidadMedida))
            {
                dto.Advertencias.Add($"'{insumo.Nombre}': la unidad '{d.UnidadMedida}' no es compatible con '{insumo.UnidadMedida}'. No se costeó.");
                continue;
            }

            var cpu = CostoInsumoPorUnidad(insumo, principales, dto.Advertencias);
            // Los empaques/insumos directos no son una preparación: no tienen merma propia.
            var linea = CalculadoraCostos.Linea(
                insumo.Id, insumo.Nombre, d.Cantidad, unidad,
                insumo.UnidadMedida, 0m, cpu, FuenteDe(insumo, principales));
            var aportado = decimal.Round(linea.CostoInsumo, 2);
            dto.InsumosDirectos.Add(new InsumoDirectoDetalleDto
            {
                InsumoId = insumo.Id,
                Nombre = insumo.Nombre,
                Cantidad = d.Cantidad,
                UnidadReceta = unidad,
                UnidadInsumo = insumo.UnidadMedida,
                CostoAportado = aportado,
                FuenteCosto = linea.FuenteCosto
            });
        }

        dto.CostoRecetas = decimal.Round(dto.Recetas.Sum(r => r.CostoAportado), 2);
        dto.CostoInsumosDirectos = decimal.Round(dto.InsumosDirectos.Sum(i => i.CostoAportado), 2);
        dto.CostoDirecto = decimal.Round(dto.CostoRecetas + dto.CostoInsumosDirectos, 2);

        // 3) Fijos: la cuota es por preparación completa de receta. El producto
        // consume Σ (porciones usadas ÷ porciones base) preparaciones equivalentes,
        // así que se le asigna esa fracción de la cuota (reparto fino).
        dto.HayCostosFijosConfigurados = fijosConfigurados.Count > 0;
        dto.SinCostosFijos = fijosAplicados.Count == 0;
        decimal fijoProrrateado = 0;

        if (!dto.HayCostosFijosConfigurados)
        {
            dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        }
        else
        {
            var cuotaPorPreparacion = CalculadoraCostos.FijoProrrateado(
                fijosAplicados.Select(c => (c.MontoMensual, c.ValorDivisor)));
            fijoProrrateado = cuotaPorPreparacion * preparacionesEquivalentes;
            if (fijosAplicados.Any(c => c.ValorDivisor < 10m))
                dto.Advertencias.Add("Uno o más costos fijos tienen un valor divisor menor a 10: el prorrateo puede estar sobrestimado.");
            if (cuotaPorPreparacion > 0 && preparacionesEquivalentes <= 0)
                dto.Advertencias.Add("No se pudo determinar cuántas preparaciones equivalentes usa el producto; el costo fijo prorrateado es 0.");
        }

        dto.PreparacionesEquivalentes = decimal.Round(preparacionesEquivalentes, 4);
        dto.CostoFijoProrrateado = decimal.Round(fijoProrrateado, 2);
        dto.CostoTotal = decimal.Round(dto.CostoDirecto + dto.CostoFijoProrrateado, 2);

        // Margen directo: precio contra recetas + empaques, sin fijos.
        // Es la base del menú y de la sugerencia de precio por defecto.
        if (dto.PrecioVenta.HasValue && dto.PrecioVenta.Value > 0 && dto.CostoDirecto > 0)
            dto.MargenDirecto = decimal.Round((dto.PrecioVenta.Value - dto.CostoDirecto) / dto.PrecioVenta.Value * 100m, 2);

        // Margen real con costo completo: (precio − costo total) ÷ precio × 100.
        // Se conserva para el análisis y el detalle del producto.
        if (dto.PrecioVenta.HasValue && dto.PrecioVenta.Value > 0 && dto.CostoTotal > 0)
            dto.MargenReal = decimal.Round((dto.PrecioVenta.Value - dto.CostoTotal) / dto.PrecioVenta.Value * 100m, 2);

        return dto;
    }

    public async Task<Dictionary<int, CostoProductoDto>> CalcularLoteAsync(
        int negocioId, IReadOnlyCollection<ProductoMenu> productos,
        List<int>? costosFijosIds = null)
    {
        var resultado = new Dictionary<int, CostoProductoDto>();
        if (productos.Count == 0) return resultado;

        var recetasPorProducto = productos.ToDictionary(
            p => p.Id,
            p => p.ProductoMenuReceta.Select(pr => new RecetaEnProductoDto
            {
                RecetaId = pr.RecetaId,
                Cantidad = pr.Cantidad
            }).ToList());

        var directosPorProducto = productos.ToDictionary(
            p => p.Id,
            p => p.ProductoMenuInsumos.Select(pi => new InsumoDirectoDto
            {
                InsumoId = pi.InsumoId,
                Cantidad = pi.Cantidad,
                UnidadMedida = pi.UnidadMedida
            }).ToList());

        // Una sola consulta para todas las recetas y otra para todos los insumos.
        var recetaIds = recetasPorProducto.Values
            .SelectMany(r => r)
            .Select(r => r.RecetaId)
            .Distinct()
            .ToList();
        var mapaRecetas = (await _productos.ObtenerRecetasConInsumosAsync(negocioId, recetaIds))
            .ToDictionary(r => r.Id);

        var idsDirectos = directosPorProducto.Values
            .SelectMany(d => d)
            .Select(d => d.InsumoId)
            .Distinct()
            .ToList();
        var idsDeRecetas = mapaRecetas.Values
            .SelectMany(r => r.RecetaInsumos)
            .Select(ri => ri.InsumoId);
        var principales = await _costeo.ObtenerCostosProveedorPrincipalAsync(
            idsDirectos.Concat(idsDeRecetas).Distinct());

        var mapaDirectos = idsDirectos.Count == 0
            ? new Dictionary<int, Insumo>()
            : (await _productos.ObtenerInsumosActivosAsync(negocioId, idsDirectos))
                .ToDictionary(i => i.Id);

        var fijosConfigurados = await _costeo.ListarCostosFijosAsync(negocioId, soloActivos: true);
        var advertenciasFijos = new List<string>();
        var fijosAplicados = fijosConfigurados.Count == 0
            ? new List<CostoFijo>()
            : SeleccionarFijos(fijosConfigurados, costosFijosIds, advertenciasFijos);

        foreach (var p in productos)
        {
            var dto = CalcularNucleo(
                p.Id, p.Nombre, p.PrecioVenta,
                recetasPorProducto[p.Id], directosPorProducto[p.Id],
                mapaRecetas, principales, mapaDirectos,
                fijosConfigurados, fijosAplicados, advertenciasFijos);

            if (dto is not null) resultado[p.Id] = dto;
        }

        return resultado;
    }

    // Costo total de UNA receta sin fijos. Los fijos se reparten aparte,
    // según las preparaciones equivalentes que consume el producto.
    private static decimal CostoRecetaSinFijos(
        Receta receta,
        Dictionary<int, PrecioProveedorPrincipal> principales,
        List<string> advertencias)
    {
        decimal total = 0;
        foreach (var ri in receta.RecetaInsumos)
        {
            var i = ri.Insumo;
            if (!ConversorUnidades.Compatibles(ri.UnidadMedida, i.UnidadMedida))
            {
                advertencias.Add($"'{i.Nombre}' (receta '{receta.Nombre}'): unidad incompatible. No se costeó.");
                continue;
            }

            var cpu = CostoInsumoPorUnidad(i, principales, advertencias, receta.Nombre);
            total += ConversorUnidades.CostoLinea(
                ri.Cantidad, ri.UnidadMedida, i.UnidadMedida, cpu, ri.PorcentajeMerma);
        }
        return decimal.Round(total, 2);
    }

    private static decimal CostoInsumoPorUnidad(
        Insumo insumo,
        Dictionary<int, PrecioProveedorPrincipal> principales,
        List<string> advertencias,
        string? contexto = null)
    {
        if (principales.TryGetValue(insumo.Id, out var pp) &&
            CostoPorUnidadProveedor(pp, insumo.UnidadMedida) is { } cpu)
            return cpu;

        if (principales.ContainsKey(insumo.Id))
            advertencias.Add($"'{insumo.Nombre}'{(contexto is null ? "" : $" (receta '{contexto}')")}: no se pudo determinar su precio por unidad con los datos del proveedor; se usó su costo unitario.");
        else
            advertencias.Add($"'{insumo.Nombre}'{(contexto is null ? "" : $" (receta '{contexto}')")} no tiene proveedor principal; se usó su costo unitario.");

        return CostoRespaldoPorUnidad(insumo);
    }

    private static string FuenteDe(Insumo insumo, Dictionary<int, PrecioProveedorPrincipal> principales) =>
        principales.TryGetValue(insumo.Id, out var pp) &&
        CostoPorUnidadProveedor(pp, insumo.UnidadMedida) is not null
            ? "proveedor_principal" : "costo_unitario_respaldo";

    private static decimal? CostoPorUnidadProveedor(PrecioProveedorPrincipal pp, string unidadInsumo)
    {
        if (pp.Cantidad <= 0 || string.IsNullOrWhiteSpace(pp.UnidadMedida)) return null;
        var factor = ConversorUnidades.Convertir(1m, pp.UnidadMedida, unidadInsumo);
        return factor is null or 0 ? null : pp.Costo / pp.Cantidad / factor;
    }

    private static decimal CostoRespaldoPorUnidad(Insumo i) =>
        i.Cantidad.HasValue && i.Cantidad.Value > 0 ? i.CostoUnitario / i.Cantidad.Value : i.CostoUnitario;

    private static List<CostoFijo> SeleccionarFijos(List<CostoFijo> fijos, List<int>? ids, List<string> advertencias)
    {
        if (ids is null) return fijos;
        if (ids.Count == 0)
        {
            advertencias.Add("No seleccionaste ningún costo fijo; el costo fijo prorrateado es 0.");
            return new();
        }
        var sel = fijos.Where(c => ids.Contains(c.Id)).ToList();
        if (sel.Count == 0)
            advertencias.Add("Ninguno de los costos fijos seleccionados aplica al cálculo; el costo fijo prorrateado es 0.");
        else if (sel.Count < fijos.Count)
            advertencias.Add($"Costo fijo prorrateado calculado con {sel.Count} de {fijos.Count} costos fijos.");
        return sel;
    }

    private static string? ValidarComposicion(List<RecetaEnProductoDto> recetas, List<InsumoDirectoDto> directos)
    {
        if (recetas.Count == 0) return "El producto necesita al menos una receta.";
        if (recetas.Count > 50) return "Máximo 50 recetas por producto.";
        var ids = recetas.Select(r => r.RecetaId).ToList();
        if (ids.Distinct().Count() != ids.Count) return "Hay recetas repetidas en el producto.";
        foreach (var r in recetas)
        {
            if (r.RecetaId <= 0) return "Selecciona una receta válida.";
            if (r.Cantidad <= 0) return "La cantidad de cada receta debe ser mayor a 0.";
            if (decimal.Round(r.Cantidad, 3) != r.Cantidad) return "La cantidad de receta admite máximo 3 decimales.";
        }
        var idsIns = directos.Select(d => d.InsumoId).ToList();
        if (idsIns.Distinct().Count() != idsIns.Count) return "Hay insumos directos repetidos.";
        foreach (var d in directos)
        {
            if (d.InsumoId <= 0) return "Selecciona un insumo directo válido.";
            if (d.Cantidad <= 0) return "La cantidad de cada insumo directo debe ser mayor a 0.";
            if (!ConversorUnidades.EsValida(d.UnidadMedida)) return "La unidad del insumo directo no es válida.";
        }
        return null;
    }
}
