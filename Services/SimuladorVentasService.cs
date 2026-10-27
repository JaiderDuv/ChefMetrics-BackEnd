using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Services;

public interface ISimuladorVentasService
{
    Task<ResultadoCosteo<SimulacionVentasDto>> CalcularAsync(int cuentaId, SimularVentasDto dto);
}

// Simulador mensual sin persistencia. Usa los precios y costos directos
// actuales del menú, y los costos fijos activos del negocio.
//
// Con cantidades reales el margen global deja de ser un promedio simple:
// margen ponderado = Σ(cantidad × margen unitario) ÷ Σ(cantidad × precio).
// El punto de equilibrio se calcula con ese margen ponderado.
public sealed class SimuladorVentasService : ISimuladorVentasService
{
    private const int MaxCantidadPorProducto = 1_000_000;
    private const int MaxProductos = 200;

    private readonly IProductoMenuService _productos;
    private readonly ICostosFijosService _costosFijos;

    public SimuladorVentasService(
        IProductoMenuService productos,
        ICostosFijosService costosFijos)
    {
        _productos = productos;
        _costosFijos = costosFijos;
    }

    public async Task<ResultadoCosteo<SimulacionVentasDto>> CalcularAsync(
        int cuentaId, SimularVentasDto dto)
    {
        var items = dto.Productos;
        if (items is null || items.Count == 0)
            return ResultadoCosteo<SimulacionVentasDto>.Error(
                "Agrega al menos un producto a la simulación.");

        if (items.Count > MaxProductos)
            return ResultadoCosteo<SimulacionVentasDto>.Error(
                $"Máximo {MaxProductos} productos por simulación.");

        var productos = await _productos.ListarAsync(cuentaId, null, incluirInactivos: false);
        if (productos is null)
            return ResultadoCosteo<SimulacionVentasDto>.NoHallado(
                "Aún no tienes un negocio registrado para simular ventas.");

        var fijos = await _costosFijos.ListarAsync(cuentaId, incluirInactivos: false);
        if (fijos is null)
            return ResultadoCosteo<SimulacionVentasDto>.NoHallado(
                "Aún no tienes un negocio registrado para simular ventas.");

        var advertencias = new List<string>();
        var grupos = items
            .GroupBy(i => i.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Cantidad = g.Sum(i => (long)i.Cantidad),
                Repetido = g.Count() > 1
            })
            .ToList();

        var mapaProductos = productos.ToDictionary(p => p.Id);
        var lineas = new List<SimulacionVentaProductoDto>();

        foreach (var grupo in grupos)
        {
            if (grupo.Cantidad is <= 0 or > MaxCantidadPorProducto)
                return ResultadoCosteo<SimulacionVentasDto>.Error(
                    "La cantidad de cada producto debe estar entre 1 y 1.000.000.");

            if (!mapaProductos.TryGetValue(grupo.ProductoId, out var producto))
            {
                advertencias.Add(
                    $"El producto #{grupo.ProductoId} no existe o está inactivo; no se incluyó.");
                continue;
            }

            if (producto.PrecioVenta is not decimal precio || precio <= 0)
            {
                advertencias.Add(
                    $"'{producto.Nombre}' no tiene precio de venta; no se incluyó en la simulación.");
                continue;
            }

            if (producto.CostoDirecto <= 0)
            {
                advertencias.Add(
                    $"'{producto.Nombre}' no tiene costo directo calculado; no se incluyó en la simulación.");
                continue;
            }

            if (grupo.Repetido)
                advertencias.Add(
                    $"'{producto.Nombre}' estaba repetido; se sumaron sus cantidades.");

            var cantidad = (int)grupo.Cantidad;
            var ventas = decimal.Round(precio * cantidad, 2);
            var costo = decimal.Round(producto.CostoDirecto * cantidad, 2);
            var margen = decimal.Round(ventas - costo, 2);

            lineas.Add(new SimulacionVentaProductoDto
            {
                ProductoId = producto.Id,
                Producto = producto.Nombre,
                PrecioVenta = decimal.Round(precio, 2),
                CostoDirecto = decimal.Round(producto.CostoDirecto, 2),
                Cantidad = cantidad,
                Ventas = ventas,
                CostoDirectoTotal = costo,
                MargenContribucion = margen,
                MargenContribucionPorcentaje = ventas > 0
                    ? decimal.Round(margen / ventas * 100m, 2)
                    : 0m
            });
        }

        if (lineas.Count == 0)
            return ResultadoCosteo<SimulacionVentasDto>.Error(
                "Ninguno de los productos seleccionados tiene precio y costo directo válidos. " +
                "Revisa el menú antes de simular.");

        var ventasTotales = decimal.Round(lineas.Sum(l => l.Ventas), 2);
        var costoDirectoTotal = decimal.Round(lineas.Sum(l => l.CostoDirectoTotal), 2);
        var margenTotal = decimal.Round(ventasTotales - costoDirectoTotal, 2);
        var costosFijos = decimal.Round(fijos.Where(f => f.Activo).Sum(f => f.MontoMensual), 2);

        var resultado = new SimulacionVentasDto
        {
            CostosFijosMensuales = costosFijos,
            VentasTotales = ventasTotales,
            CostoDirectoTotal = costoDirectoTotal,
            MargenContribucion = margenTotal,
            MargenContribucionPorcentaje = ventasTotales > 0
                ? decimal.Round(margenTotal / ventasTotales * 100m, 2)
                : null,
            Utilidad = decimal.Round(margenTotal - costosFijos, 2),
            ProductosConsiderados = lineas.Count,
            Productos = lineas,
            Advertencias = advertencias
        };

        if (costosFijos <= 0)
        {
            resultado.PuntoEquilibrioMensual = 0m;
            resultado.Advertencias.Add(
                "No hay costos fijos activos; el punto de equilibrio es $0 y la utilidad " +
                "equivale al margen de contribución.");
        }
        else if (margenTotal <= 0)
        {
            resultado.Advertencias.Add(
                "El margen de contribución es cero o negativo; con este mix no se puede " +
                "calcular un punto de equilibrio.");
        }
        else
        {
            var ratio = margenTotal / ventasTotales;
            var punto = decimal.Round(costosFijos / ratio, 2);
            resultado.PuntoEquilibrioMensual = punto;

            if (ventasTotales < punto)
                resultado.FaltanteParaEquilibrio = decimal.Round(punto - ventasTotales, 2);
            else
                resultado.ExcedenteSobreEquilibrio = decimal.Round(ventasTotales - punto, 2);

            resultado.CoberturaCostosFijosPorcentaje =
                decimal.Round(margenTotal / costosFijos * 100m, 2);
        }

        if (margenTotal > 0)
        {
            foreach (var linea in resultado.Productos)
            {
                linea.ParticipacionVentasPorcentaje = ventasTotales > 0
                    ? decimal.Round(linea.Ventas / ventasTotales * 100m, 2)
                    : null;
                linea.ParticipacionMargenPorcentaje =
                    decimal.Round(linea.MargenContribucion / margenTotal * 100m, 2);
            }
        }

        foreach (var linea in resultado.Productos.Where(l => l.MargenContribucion <= 0))
            resultado.Advertencias.Add(
                $"'{linea.Producto}' tiene margen de contribución cero o negativo; " +
                "revisa su precio o su costo directo.");

        return ResultadoCosteo<SimulacionVentasDto>.Ok(resultado);
    }
}
