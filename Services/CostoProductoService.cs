using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Services;

public interface ICostoProductoService
{
    Task<ResultadoCosteo<CostoProductoDto>> CalcularAsync(
        int cuentaId, int productoId,
        List<InsumoDirectoDto>? insumosDirectos = null,
        List<int>? costosFijosIds = null);

    Task<ResultadoCosteo<CostoProductoDto>> CalcularPreviewAsync(
        int cuentaId, PreviewCostoProductoDto dto);
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

        return await CalcularNucleoAsync(
            negocioId.Value, producto.Id, producto.Nombre, producto.PrecioVenta,
            recetas, insumosDirectos ?? new(), costosFijosIds);
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
    // Por cada receta: costo total HU8.1 (insumos con conversión, merma,
    // proveedor principal y fijos prorrateados) × cantidad en el producto.
    // Insumos directos (empaques): mismo tratamiento de merma y proveedor.
    // costoTotalProducto = Σ (costoReceta × cantidad) + Σ (costo insumo directo)
    private async Task<ResultadoCosteo<CostoProductoDto>> CalcularNucleoAsync(
        int negocioId, int productoId, string nombre, decimal? precioVenta,
        List<RecetaEnProductoDto> recetas, List<InsumoDirectoDto> directos,
        List<int>? costosFijosIds)
    {
        var dto = new CostoProductoDto
        {
            ProductoId = productoId,
            Producto = nombre,
            PrecioVenta = precioVenta
        };

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

        // 1) Recetas: cada una a costo total HU8.1 (sin fijos aquí; los fijos se prorratean una vez a nivel producto).
        foreach (var item in recetas)
        {
            if (!mapaRecetas.TryGetValue(item.RecetaId, out var receta))
            {
                dto.Advertencias.Add($"La receta #{item.RecetaId} no existe en tu negocio o está inactiva. No se costeó.");
                continue;
            }

            var costoReceta = CostoRecetaSinFijos(receta, principales, dto.Advertencias);
            var aportado = decimal.Round(costoReceta * item.Cantidad, 2);
            dto.Recetas.Add(new RecetaDetalleProductoDto
            {
                RecetaId = receta.Id,
                Nombre = receta.Nombre,
                Cantidad = item.Cantidad,
                CostoUnitario = decimal.Round(costoReceta, 2),
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
            var linea = CalculadoraCostos.Linea(
                insumo.Id, insumo.Nombre, d.Cantidad, unidad,
                insumo.UnidadMedida, insumo.PorcentajeMerma, cpu, FuenteDe(insumo, principales));
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

        // 3) Fijos prorrateados UNA vez a nivel producto (no por receta, para no multiplicarlos).
        var fijos = await _costeo.ListarCostosFijosAsync(negocioId, soloActivos: true);
        dto.SinCostosFijos = fijos.Count == 0;
        decimal fijoProrrateado = 0;
        if (dto.SinCostosFijos)
        {
            dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        }
        else
        {
            fijos = FiltrarFijos(fijos, costosFijosIds, dto.Advertencias);
            fijoProrrateado = CalculadoraCostos.FijoProrrateado(fijos.Select(c => (c.MontoMensual, c.ValorDivisor)));
            if (fijos.Any(c => c.ValorDivisor < 10m))
                dto.Advertencias.Add("Uno o más costos fijos tienen un valor divisor menor a 10: el prorrateo puede estar sobrestimado.");
        }

        dto.CostoTotal = decimal.Round(dto.CostoRecetas + dto.CostoInsumosDirectos + fijoProrrateado, 2);

        // Margen real coherente con el precio por suma: (precio − costo) ÷ costo × 100.
        if (dto.PrecioVenta.HasValue && dto.PrecioVenta.Value > 0 && dto.CostoTotal > 0)
            dto.MargenReal = decimal.Round((dto.PrecioVenta.Value - dto.CostoTotal) / dto.CostoTotal * 100m, 2);

        return ResultadoCosteo<CostoProductoDto>.Ok(dto);
    }

    // Costo total de UNA receta sin fijos (los fijos se suman una vez a nivel producto).
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
                ri.Cantidad, ri.UnidadMedida, i.UnidadMedida, cpu, i.PorcentajeMerma);
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

    private static List<CostoFijo> FiltrarFijos(List<CostoFijo> fijos, List<int>? ids, List<string> advertencias)
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
