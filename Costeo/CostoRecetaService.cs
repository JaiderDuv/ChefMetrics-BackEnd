using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Costeo;

public interface ICostoRecetaService
{
    // HU8.2 inyecta esta interfaz y usa Datos.CostoPorPorcion.
    // costosFijosIds: si se indica, el prorrateo usa solo esos costos fijos; null = todos.
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
            if (principales.TryGetValue(i.Id, out var cp)) { costo = cp; fuente = "proveedor_principal"; }
            else
            {
                costo = i.CostoUnitario; fuente = "costo_unitario_respaldo";
                dto.Advertencias.Add($"'{i.Nombre}' no tiene proveedor principal; se usó su costo unitario.");
            }
            if (costo <= 0) dto.Advertencias.Add($"'{i.Nombre}' tiene costo 0; el resultado puede estar subestimado.");

            dto.Lineas.Add(CalculadoraCostos.Linea(i.Id, i.Nombre, ri.Cantidad, ri.UnidadMedida,
                i.UnidadMedida, i.PorcentajeMerma, costo, fuente));
        }

        // 2) Costos fijos prorrateados (solo los seleccionados por el usuario, si se indican)
        var fijos = await _repo.ListarCostosFijosAsync(negocioId.Value, soloActivos: true);
        dto.SinCostosFijos = fijos.Count == 0;
        if (dto.SinCostosFijos) dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        else fijos = FiltrarFijosSeleccionados(fijos, costosFijosIds, dto.Advertencias);
        var fijoProrrateado = CalculadoraCostos.FijoProrrateado(fijos.Select(c => (c.MontoMensual, c.ValorDivisor)));

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
            if (principales.TryGetValue(i.Id, out var cp)) { costo = cp; fuente = "proveedor_principal"; }
            else
            {
                costo = i.CostoUnitario; fuente = "costo_unitario_respaldo";
                dto.Advertencias.Add($"'{i.Nombre}' no tiene proveedor principal; se usó su costo unitario.");
            }
            if (costo <= 0) dto.Advertencias.Add($"'{i.Nombre}' tiene costo 0; el resultado puede estar subestimado.");

            dto.Lineas.Add(CalculadoraCostos.Linea(i.Id, i.Nombre, ri.Cantidad * factor, ri.UnidadMedida,
                i.UnidadMedida, i.PorcentajeMerma, costo, fuente));
        }

        var fijos = await _repo.ListarCostosFijosAsync(negocioId.Value, soloActivos: true);
        dto.SinCostosFijos = fijos.Count == 0;
        if (dto.SinCostosFijos) dto.Advertencias.Add("No hay costos fijos configurados; el costo fijo prorrateado es 0.");
        else fijos = FiltrarFijosSeleccionados(fijos, costosFijosIds, dto.Advertencias);
        var fijoProrrateado = CalculadoraCostos.FijoProrrateado(fijos.Select(c => (c.MontoMensual, c.ValorDivisor)));

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
}