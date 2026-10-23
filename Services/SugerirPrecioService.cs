using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface ISugerirPrecioService
{
    // costosFijosIds: null = todos los fijos activos; lista vacía = ninguno;
    // lista con IDs = solo esos costos fijos.
    Task<ResultadoCosteo<PrecioSugeridoDto>> CalcularAsync(
        int cuentaId, int recetaId, decimal margenDeseado, List<int>? costosFijosIds = null);
}

public sealed class SugerirPrecioService : ISugerirPrecioService
{
    private readonly ICostoRecetaService _costoRecetaService;

    public SugerirPrecioService(ICostoRecetaService costoRecetaService)
    {
        _costoRecetaService = costoRecetaService;
    }

    public async Task<ResultadoCosteo<PrecioSugeridoDto>> CalcularAsync(
        int cuentaId, int recetaId, decimal margenDeseado, List<int>? costosFijosIds = null)
    {
        var costo = await _costoRecetaService.CalcularAsync(cuentaId, recetaId, costosFijosIds);
        if (costo.NoEncontrado)
            return ResultadoCosteo<PrecioSugeridoDto>.NoHallado(costo.Mensaje);
        if (!costo.Exito || costo.Datos is null)
            return ResultadoCosteo<PrecioSugeridoDto>.Error(costo.Mensaje);

        var datosCosto = costo.Datos;
        var dto = new PrecioSugeridoDto
        {
            RecetaId = datosCosto.RecetaId,
            Receta = datosCosto.Receta,
            CostoDirectoPorPorcion = datosCosto.PorcionesBase > 0
                ? decimal.Round(datosCosto.CostoInsumos / datosCosto.PorcionesBase, 2)
                : 0,
            CostoPorPorcion = datosCosto.CostoPorPorcion,
            MargenDeseado = margenDeseado,
            Advertencias = new List<string>(datosCosto.Advertencias)
        };

        if (margenDeseado <= 0)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("Un margen menor o igual a 0% no genera ganancia.");
        }
        else if (margenDeseado >= 100)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("Un margen de 100% o más no permite calcular un precio válido.");
        }
        else if (datosCosto.PorcionesBase <= 0)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("No se puede sugerir un precio: la receta no tiene un costo por porción válido.");
        }
        else
        {
            dto.MargenValido = true;
            dto.PrecioSugerido = decimal.Round(datosCosto.CostoPorPorcion / (1 - (margenDeseado / 100m)), 2);
        }

        return ResultadoCosteo<PrecioSugeridoDto>.Ok(dto);
    }
}