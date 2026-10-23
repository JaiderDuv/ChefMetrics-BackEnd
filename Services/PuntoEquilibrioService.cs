using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface IPuntoEquilibrioService
{
    Task<ResultadoCosteo<PuntoEquilibrioDto>> CalcularAsync(int cuentaId);
}

// Punto de equilibrio = costos fijos mensuales ÷ margen de contribución.
// Margen de contribución = precio de venta − costo directo (sin fijos).
// Se calcula por producto (si fuera el único que se vende) y un global
// con el margen promedio, que supone un mix uniforme de productos.
public sealed class PuntoEquilibrioService : IPuntoEquilibrioService
{
    private readonly IProductoMenuService _productos;
    private readonly ICostosFijosService _costosFijos;

    public PuntoEquilibrioService(IProductoMenuService productos, ICostosFijosService costosFijos)
    {
        _productos = productos;
        _costosFijos = costosFijos;
    }

    public async Task<ResultadoCosteo<PuntoEquilibrioDto>> CalcularAsync(int cuentaId)
    {
        var fijos = await _costosFijos.ListarAsync(cuentaId, incluirInactivos: false);
        if (fijos is null)
            return ResultadoCosteo<PuntoEquilibrioDto>.NoHallado("Aún no tienes un negocio registrado.");

        var productos = await _productos.ListarAsync(cuentaId, null, incluirInactivos: false);
        if (productos is null)
            return ResultadoCosteo<PuntoEquilibrioDto>.NoHallado("Aún no tienes un negocio registrado.");

        var dto = new PuntoEquilibrioDto
        {
            CostosFijosMensuales = decimal.Round(fijos.Where(f => f.Activo).Sum(f => f.MontoMensual), 2)
        };

        if (dto.CostosFijosMensuales <= 0)
            dto.Advertencias.Add("No hay costos fijos activos: no hay un punto de equilibrio que cubrir.");

        var conPrecio = productos
            .Where(p => p.PrecioVenta.HasValue && p.PrecioVenta.Value > 0)
            .ToList();

        if (conPrecio.Count == 0)
        {
            dto.Advertencias.Add("No hay productos activos con precio de venta; no se puede calcular el punto de equilibrio.");
            return ResultadoCosteo<PuntoEquilibrioDto>.Ok(dto);
        }

        var ratios = new List<decimal>();
        var sinCosto = 0;

        foreach (var p in conPrecio)
        {
            var precio = p.PrecioVenta!.Value;
            var directo = p.CostoDirecto;
            var margen = precio - directo;

            var item = new PuntoEquilibrioProductoDto
            {
                ProductoId = p.Id,
                Producto = p.Nombre,
                PrecioVenta = decimal.Round(precio, 2),
                CostoDirecto = decimal.Round(directo, 2),
                MargenContribucion = decimal.Round(margen, 2),
                MargenContribucionPorcentaje = decimal.Round(margen / precio * 100m, 2)
            };

            if (directo <= 0)
            {
                // Sin costo calculado el margen está sobreestimado: no se usa
                // para el promedio ni para el punto de equilibrio global.
                sinCosto++;
            }
            else if (margen > 0 && dto.CostosFijosMensuales > 0)
            {
                // Unidades a vender si este producto fuera el único del menú.
                item.UnidadesEquilibrio = Math.Ceiling(dto.CostosFijosMensuales / margen);
                item.VentasEquilibrio = decimal.Round(dto.CostosFijosMensuales / (margen / precio), 2);
            }

            if (directo > 0 && margen > 0) ratios.Add(margen / precio);
            dto.Productos.Add(item);
        }

        dto.ProductosConsiderados = conPrecio.Count;
        if (sinCosto > 0)
            dto.Advertencias.Add($"{sinCosto} producto(s) no tienen costo directo calculado; revisa sus recetas y empaques antes de confiar en su margen.");

        if (ratios.Count == 0)
        {
            dto.Advertencias.Add("Ningún producto tiene margen de contribución positivo; revisa precios y costos.");
            return ResultadoCosteo<PuntoEquilibrioDto>.Ok(dto);
        }

        var ratioPromedio = ratios.Average();
        dto.MargenContribucionPromedioPorcentaje = decimal.Round(ratioPromedio * 100m, 2);
        if (dto.CostosFijosMensuales > 0)
            dto.VentasEquilibrioMensuales = decimal.Round(dto.CostosFijosMensuales / ratioPromedio, 2);

        dto.Advertencias.Add("El punto de equilibrio global supone que todos los productos participan por igual en las ventas. Las unidades por producto aplican solo si vendieras ese producto.");
        return ResultadoCosteo<PuntoEquilibrioDto>.Ok(dto);
    }
}
