using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public interface ICostoRecetaService
{
    // HU8.2 inyecta esta interfaz y usa Datos.CostoPorPorcion.
    // costosFijosIds: null = todos los fijos activos; lista vacía = ninguno;
    // lista con IDs = solo esos costos fijos.
    Task<ResultadoCosteo<CostoRecetaDto>> CalcularAsync(int cuentaId, int recetaId, List<int>? costosFijosIds = null);
    Task<ResultadoCosteo<EscaladoRecetaDto>> CalcularEscaladoAsync(int cuentaId, int recetaId, int porcionesDeseadas, List<int>? costosFijosIds = null);
}

public sealed class CostoRecetaService : ICostoRecetaService
{
    private readonly ICosteoRepository _repo;
    private readonly IInsumoRepository _insumoRepository;

    public CostoRecetaService(ICosteoRepository repo, IInsumoRepository insumoRepository)
    {
        _repo = repo;
        _insumoRepository = insumoRepository;
    }

    public async Task<ResultadoCosteo<CostoRecetaDto>> CalcularAsync(int cuentaId, int recetaId, List<int>? costosFijosIds = null)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<CostoRecetaDto>.NoHallado("Aún no tienes un negocio registrado.");

        var receta = await _repo.ObtenerRecetaConInsumosAsync(negocioId.Value, recetaId);
        if (receta is null)
            return ResultadoCosteo<CostoRecetaDto>.NoHallado("No se encontró la receta solicitada.");

        var dto = new CostoRecetaDto { RecetaId = receta.Id, Receta = receta.Nombre, PorcionesBase = receta.PorcionesBase };
        var preparacionesBaseMes = await _repo.ObtenerPreparacionesBaseMesAsync(negocioId.Value);

        if (receta.RecetaInsumos.Count == 0)
            dto.Advertencias.Add("La receta no tiene ingredientes.");

        // 1) Costo por línea (proveedor principal; respaldo: Insumo.CostoUnitario)
        var principales = await _repo.ObtenerCostosProveedorPrincipalAsync(receta.RecetaInsumos.Select(ri => ri.InsumoId));
        foreach (var ri in receta.RecetaInsumos)
        {
            var i = ri.Insumo;
            if (!ConversorUnidades.Compatibles(ri.UnidadMedida, i.UnidadMedida))
            {
                dto.Advertencias.Add($"'{i.Nombre}': la unidad '{ri.UnidadMedida}' no es compatible con '{i.UnidadMedida}'. No se costeó.");
                continue;
            }

            decimal costo; string fuente;
            if (principales.TryGetValue(i.Id, out var pp) &&
                CostoPorUnidad(pp, i.UnidadMedida) is { } cpu)
            { costo = cpu; fuente = "proveedor_principal"; }
            else
            {
                if (principales.ContainsKey(i.Id))
                    dto.Advertencias.Add($"'{i.Nombre}': no se pudo determinar su precio por unidad con los datos del proveedor; se usó su costo unitario.");
                else
                    dto.Advertencias.Add($"'{i.Nombre}' no tiene proveedor principal; se usó su costo unitario.");
                costo = CostoRespaldoPorUnidad(i); fuente = "costo_unitario_respaldo";
            }
            if (costo <= 0) dto.Advertencias.Add($"'{i.Nombre}' tiene costo 0; el resultado puede estar subestimado.");

            dto.Lineas.Add(CalculadoraCostos.Linea(i.Id, i.Nombre, ri.Cantidad, ri.UnidadMedida,
                i.UnidadMedida, ri.PorcentajeMerma, costo, fuente));
        }

        // 2) Costos fijos prorrateados (solo los seleccionados por el usuario, si se indican)
        var fijosConfigurados = await _repo.ListarCostosFijosAsync(negocioId.Value, soloActivos: true);
        dto.HayCostosFijosConfigurados = fijosConfigurados.Count > 0;
        var fijos = fijosConfigurados;
        if (!dto.HayCostosFijosConfigurados)
            dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        else
            fijos = FiltrarFijosSeleccionados(fijos, costosFijosIds, dto.Advertencias);
        dto.SinCostosFijos = fijos.Count == 0;
        var fijoProrrateado = preparacionesBaseMes is > 0
            ? CalculadoraCostos.FijoProrrateado(fijos.Select(c => c.MontoMensual), preparacionesBaseMes.Value)
            : 0m;
        AdvertenciaPreparacionesBaseMes(preparacionesBaseMes, fijos, dto.Advertencias);

        // 3) Totales
        dto.CostoInsumos = decimal.Round(dto.Lineas.Sum(l => l.CostoInsumo), 2);
        dto.CostoMermaAplicada = decimal.Round(dto.Lineas.Sum(l => l.MermaExtra), 2);
        dto.CostoFijoProrrateado = decimal.Round(fijoProrrateado, 2);
        dto.CostoTotal = dto.CostoInsumos + dto.CostoFijoProrrateado;

        if (receta.PorcionesBase > 0) dto.CostoPorPorcion = decimal.Round(dto.CostoTotal / receta.PorcionesBase, 2);
        else dto.Advertencias.Add("La receta tiene 0 porciones; no se puede calcular el costo por porción.");

        foreach (var l in dto.Lineas)
        {
            l.CostoInsumo = decimal.Round(l.CostoInsumo, 2);
            l.MermaExtra = decimal.Round(l.MermaExtra, 2);
        }
        return ResultadoCosteo<CostoRecetaDto>.Ok(dto);
    }

    public async Task<ResultadoCosteo<EscaladoRecetaDto>> CalcularEscaladoAsync(int cuentaId, int recetaId, int porcionesDeseadas, List<int>? costosFijosIds = null)
    {
        if (porcionesDeseadas <= 0)
            return ResultadoCosteo<EscaladoRecetaDto>.Error("Las porciones deseadas deben ser mayores a 0.");

        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<EscaladoRecetaDto>.NoHallado("Aún no tienes un negocio registrado.");

        var receta = await _repo.ObtenerRecetaConInsumosAsync(negocioId.Value, recetaId);
        if (receta is null)
            return ResultadoCosteo<EscaladoRecetaDto>.NoHallado("No se encontró la receta solicitada.");

        if (receta.PorcionesBase <= 0)
            return ResultadoCosteo<EscaladoRecetaDto>.Error("La receta tiene 0 porciones base; no se puede escalar.");

        var factor = (decimal)porcionesDeseadas / receta.PorcionesBase;
        var dto = new EscaladoRecetaDto
        {
            RecetaId = receta.Id,
            Receta = receta.Nombre,
            PorcionesBase = receta.PorcionesBase,
            PorcionesDeseadas = porcionesDeseadas,
            FactorEscala = decimal.Round(factor, 4)
        };
        var preparacionesBaseMes = await _repo.ObtenerPreparacionesBaseMesAsync(negocioId.Value);

        if (receta.RecetaInsumos.Count == 0)
            dto.Advertencias.Add("La receta no tiene ingredientes.");

        var principales = await _repo.ObtenerCostosProveedorPrincipalAsync(receta.RecetaInsumos.Select(ri => ri.InsumoId));
        foreach (var ri in receta.RecetaInsumos)
        {
            var i = ri.Insumo;
            if (!ConversorUnidades.Compatibles(ri.UnidadMedida, i.UnidadMedida))
            {
                dto.Advertencias.Add($"'{i.Nombre}': la unidad '{ri.UnidadMedida}' no es compatible con '{i.UnidadMedida}'. No se costeó.");
                continue;
            }

            decimal costo; string fuente;
            if (principales.TryGetValue(i.Id, out var pp) &&
                CostoPorUnidad(pp, i.UnidadMedida) is { } cpu)
            { costo = cpu; fuente = "proveedor_principal"; }
            else
            {
                if (principales.ContainsKey(i.Id))
                    dto.Advertencias.Add($"'{i.Nombre}': no se pudo determinar su precio por unidad con los datos del proveedor; se usó su costo unitario.");
                else
                    dto.Advertencias.Add($"'{i.Nombre}' no tiene proveedor principal; se usó su costo unitario.");
                costo = CostoRespaldoPorUnidad(i); fuente = "costo_unitario_respaldo";
            }
            if (costo <= 0) dto.Advertencias.Add($"'{i.Nombre}' tiene costo 0; el resultado puede estar subestimado.");

            dto.Lineas.Add(CalculadoraCostos.Linea(i.Id, i.Nombre, ri.Cantidad * factor, ri.UnidadMedida,
                i.UnidadMedida, ri.PorcentajeMerma, costo, fuente));
        }

        var fijosConfigurados = await _repo.ListarCostosFijosAsync(negocioId.Value, soloActivos: true);
        dto.HayCostosFijosConfigurados = fijosConfigurados.Count > 0;
        var fijos = fijosConfigurados;
        if (!dto.HayCostosFijosConfigurados)
            dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        else
            fijos = FiltrarFijosSeleccionados(fijos, costosFijosIds, dto.Advertencias);
        dto.SinCostosFijos = fijos.Count == 0;

        // La cuota es por preparación base. Al escalar a más porciones se consumen
        // "factor" preparaciones equivalentes (8 porciones de una base de 4 = 2).
        var cuotaPorPreparacion = preparacionesBaseMes is > 0
            ? CalculadoraCostos.FijoProrrateado(fijos.Select(c => c.MontoMensual), preparacionesBaseMes.Value)
            : 0m;
        var fijoProrrateado = cuotaPorPreparacion * factor;
        AdvertenciaPreparacionesBaseMes(preparacionesBaseMes, fijos, dto.Advertencias);

        dto.CostoInsumos = decimal.Round(dto.Lineas.Sum(l => l.CostoInsumo), 2);
        dto.CostoMermaAplicada = decimal.Round(dto.Lineas.Sum(l => l.MermaExtra), 2);
        dto.CostoFijoProrrateado = decimal.Round(fijoProrrateado, 2);
        dto.CostoTotal = dto.CostoInsumos + dto.CostoFijoProrrateado;
        dto.CostoPorPorcion = decimal.Round(dto.CostoTotal / porcionesDeseadas, 2);

        foreach (var l in dto.Lineas)
        {
            l.CostoInsumo = decimal.Round(l.CostoInsumo, 2);
            l.MermaExtra = decimal.Round(l.MermaExtra, 2);
        }
        return ResultadoCosteo<EscaladoRecetaDto>.Ok(dto);
    }

    // Precio por unidad del insumo a partir del paquete del proveedor principal.
    // null si no se puede determinar (cantidad 0 o unidad incompatible).
    // Nota: el factor convierte cantidades (1 kg -> 1000 g), así que el precio
    // por unidad se divide por él (precio por g = precio por kg / 1000).
    private static decimal? CostoPorUnidad(PrecioProveedorPrincipal pp, string unidadInsumo)
    {
        if (pp.Cantidad <= 0 || string.IsNullOrWhiteSpace(pp.UnidadMedida)) return null;
        var factor = ConversorUnidades.Convertir(1m, pp.UnidadMedida, unidadInsumo);
        return factor is null or 0 ? null : pp.Costo / pp.Cantidad / factor;
    }

    // Respaldo: Insumo.CostoUnitario también es el total del paquete (Insumo.Cantidad).
    private static decimal CostoRespaldoPorUnidad(Insumo i) =>
        i.Cantidad.HasValue && i.Cantidad.Value > 0 ? i.CostoUnitario / i.Cantidad.Value : i.CostoUnitario;

    // Si el usuario seleccionó costos fijos específicos, el prorrateo usa solo esos.
    private static List<CostoFijo> FiltrarFijosSeleccionados(
        List<CostoFijo> fijos, List<int>? ids, List<string> advertencias)
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

    // El divisor es único por negocio. Si falta, los fijos no se pueden repartir;
    // si es muy bajo, la cuota por preparación puede quedar sobrestimada.
    private static void AdvertenciaPreparacionesBaseMes(
        int? preparacionesBaseMes, List<CostoFijo> fijos, List<string> advertencias)
    {
        if (fijos.Count == 0) return;

        if (preparacionesBaseMes is null or <= 0)
            advertencias.Add("No has configurado las preparaciones base al mes; el costo fijo prorrateado es 0. Defínelas en 'Costos fijos'.");
        else if (preparacionesBaseMes < 10)
            advertencias.Add("Las preparaciones base al mes son menores a 10; la cuota prorrateada puede quedar muy alta. Revisa el valor en 'Costos fijos'.");
    }
}