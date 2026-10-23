using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface ISugerirPrecioProductoService
{
    Task<ResultadoCosteo<PrecioSugeridoProductoDto>> CalcularAsync(int cuentaId, int productoId, decimal margenDeseado);
    Task<ResultadoCosteo<PrecioSugeridoProductoDto>> CalcularPreviewAsync(int cuentaId, PreviewPrecioProductoDto dto);
}

public sealed class SugerirPrecioProductoService : ISugerirPrecioProductoService
{
    private readonly ICostoProductoService _costos;

    public SugerirPrecioProductoService(ICostoProductoService costos) => _costos = costos;

    public async Task<ResultadoCosteo<PrecioSugeridoProductoDto>> CalcularAsync(int cuentaId, int productoId, decimal margenDeseado)
    {
        var costo = await _costos.CalcularAsync(cuentaId, productoId);
        if (costo.NoEncontrado) return ResultadoCosteo<PrecioSugeridoProductoDto>.NoHallado(costo.Mensaje);
        if (!costo.Exito || costo.Datos is null) return ResultadoCosteo<PrecioSugeridoProductoDto>.Error(costo.Mensaje);
        return Armar(costo.Datos.ProductoId, costo.Datos.Producto, costo.Datos.CostoTotal, margenDeseado, costo.Datos.Advertencias);
    }

    public async Task<ResultadoCosteo<PrecioSugeridoProductoDto>> CalcularPreviewAsync(int cuentaId, PreviewPrecioProductoDto dto)
    {
        var costo = await _costos.CalcularPreviewAsync(cuentaId, new PreviewCostoProductoDto
        {
            Recetas = dto.Recetas,
            InsumosDirectos = dto.InsumosDirectos,
            CostosFijosIds = dto.CostosFijosIds
        });
        if (costo.NoEncontrado) return ResultadoCosteo<PrecioSugeridoProductoDto>.NoHallado(costo.Mensaje);
        if (!costo.Exito || costo.Datos is null) return ResultadoCosteo<PrecioSugeridoProductoDto>.Error(costo.Mensaje);
        return Armar(null, "(vista previa)", costo.Datos.CostoTotal, dto.MargenDeseado, costo.Datos.Advertencias);
    }

    // Margen sobre el precio de venta: precio = costo ÷ (1 − margen/100).
    // Misma definición que la sugerencia de receta y el margen real del producto.
    private static ResultadoCosteo<PrecioSugeridoProductoDto> Armar(
        int? productoId, string nombre, decimal costoTotal, decimal margen, List<string> advertencias)
    {
        var dto = new PrecioSugeridoProductoDto
        {
            ProductoId = productoId,
            Producto = nombre,
            CostoTotal = costoTotal,
            MargenDeseado = margen,
            Advertencias = new List<string>(advertencias)
        };

        if (margen <= 0)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("Un margen menor o igual a 0% no genera ganancia.");
        }
        else if (margen >= 100)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("Un margen de 100% o más no permite calcular un precio válido.");
        }
        else if (costoTotal <= 0)
        {
            dto.MargenValido = false;
            dto.Advertencias.Add("No se puede sugerir un precio: el costo total no es válido.");
        }
        else
        {
            dto.MargenValido = true;
            dto.PrecioSugerido = decimal.Round(costoTotal / (1 - (margen / 100m)), 2);
        }

        return ResultadoCosteo<PrecioSugeridoProductoDto>.Ok(dto);
    }
}
