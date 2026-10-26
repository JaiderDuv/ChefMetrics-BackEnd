using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class SimuladorVentasServiceTests
{
    private const int CuentaId = 7;

    [Fact]
    public async Task Calcula_totales_y_punto_de_equilibrio_con_el_mix_ponderado()
    {
        var (servicio, _, _) = CrearServicio(
            new[]
            {
                Producto(1, "Desayuno", 10m, 6m),
                Producto(2, "Pastel", 20m, 10m)
            },
            new[] { Fijo(1_000m) });

        var resultado = await servicio.CalcularAsync(CuentaId, new SimularVentasDto
        {
            Productos =
            {
                new() { ProductoId = 1, Cantidad = 10 },
                new() { ProductoId = 2, Cantidad = 5 }
            }
        });

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<SimulacionVentasDto>(resultado.Datos);
        Assert.Equal(200m, dto.VentasTotales);
        Assert.Equal(110m, dto.CostoDirectoTotal);
        Assert.Equal(90m, dto.MargenContribucion);
        Assert.Equal(45m, dto.MargenContribucionPorcentaje!.Value);
        Assert.Equal(-910m, dto.Utilidad);
        Assert.Equal(2_222.22m, dto.PuntoEquilibrioMensual!.Value);
        Assert.Equal(2_022.22m, dto.FaltanteParaEquilibrio!.Value);
        Assert.Null(dto.ExcedenteSobreEquilibrio);
        Assert.Equal(9m, dto.CoberturaCostosFijosPorcentaje!.Value);
        Assert.Equal(2, dto.ProductosConsiderados);

        var desayuno = dto.Productos.Single(p => p.ProductoId == 1);
        Assert.Equal(50m, desayuno.ParticipacionVentasPorcentaje!.Value);
        Assert.Equal(44.44m, desayuno.ParticipacionMargenPorcentaje!.Value);

        var pastel = dto.Productos.Single(p => p.ProductoId == 2);
        Assert.Equal(50m, pastel.ParticipacionVentasPorcentaje!.Value);
        Assert.Equal(55.56m, pastel.ParticipacionMargenPorcentaje!.Value);
    }

    [Fact]
    public async Task Suma_cantidades_repetidas_y_advierte()
    {
        var (servicio, _, _) = CrearServicio(
            new[] { Producto(1, "Desayuno", 10m, 6m) },
            new[] { Fijo(1_000m) });

        var resultado = await servicio.CalcularAsync(CuentaId, new SimularVentasDto
        {
            Productos =
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 1, Cantidad = 3 }
            }
        });

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<SimulacionVentasDto>(resultado.Datos);
        Assert.Single(dto.Productos);
        Assert.Equal(5, dto.Productos[0].Cantidad);
        Assert.Equal(50m, dto.VentasTotales);
        Assert.Contains(dto.Advertencias, a => a.Contains("repetido"));
    }

    [Fact]
    public async Task Omite_productos_sin_costo_directo_y_advierte()
    {
        var (servicio, _, _) = CrearServicio(
            new[]
            {
                Producto(1, "Desayuno", 10m, 6m),
                Producto(2, "Pastel", 20m, 0m)
            },
            new[] { Fijo(1_000m) });

        var resultado = await servicio.CalcularAsync(CuentaId, new SimularVentasDto
        {
            Productos =
            {
                new() { ProductoId = 1, Cantidad = 1 },
                new() { ProductoId = 2, Cantidad = 1 }
            }
        });

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<SimulacionVentasDto>(resultado.Datos);
        Assert.Single(dto.Productos);
        Assert.Equal(1, dto.ProductosConsiderados);
        Assert.Contains(dto.Advertencias, a => a.Contains("costo directo"));
    }

    [Fact]
    public async Task No_calcula_punto_de_equilibrio_si_el_margen_es_cero_o_negativo()
    {
        var (servicio, _, _) = CrearServicio(
            new[] { Producto(1, "Desayuno", 10m, 10m) },
            new[] { Fijo(1_000m) });

        var resultado = await servicio.CalcularAsync(CuentaId, new SimularVentasDto
        {
            Productos = { new() { ProductoId = 1, Cantidad = 1 } }
        });

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<SimulacionVentasDto>(resultado.Datos);
        Assert.Equal(0m, dto.MargenContribucion);
        Assert.Equal(-1_000m, dto.Utilidad);
        Assert.Null(dto.PuntoEquilibrioMensual);
        Assert.Null(dto.FaltanteParaEquilibrio);
        Assert.Null(dto.CoberturaCostosFijosPorcentaje);
        Assert.Contains(dto.Advertencias, a => a.Contains("cero o negativo"));
    }

    [Fact]
    public async Task Sin_costos_fijos_el_punto_de_equilibrio_es_cero()
    {
        var (servicio, _, _) = CrearServicio(
            new[] { Producto(1, "Desayuno", 10m, 6m) },
            Array.Empty<CostoFijoDto>());

        var resultado = await servicio.CalcularAsync(CuentaId, new SimularVentasDto
        {
            Productos = { new() { ProductoId = 1, Cantidad = 2 } }
        });

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<SimulacionVentasDto>(resultado.Datos);
        Assert.Equal(0m, dto.PuntoEquilibrioMensual!.Value);
        Assert.Null(dto.CoberturaCostosFijosPorcentaje);
        Assert.Contains(dto.Advertencias, a => a.Contains("No hay costos fijos"));
    }

    private static (ISimuladorVentasService Servicio, Mock<IProductoMenuService> Productos, Mock<ICostosFijosService> Fijos)
        CrearServicio(IEnumerable<ProductoMenuDto> productos, IEnumerable<CostoFijoDto> fijos)
    {
        var productosMock = new Mock<IProductoMenuService>(MockBehavior.Strict);
        productosMock
            .Setup(s => s.ListarAsync(CuentaId, It.IsAny<string?>(), false))
            .ReturnsAsync(productos.ToList());

        var fijosMock = new Mock<ICostosFijosService>(MockBehavior.Strict);
        fijosMock
            .Setup(s => s.ListarAsync(CuentaId, false))
            .ReturnsAsync(fijos.ToList());

        var servicio = new SimuladorVentasService(productosMock.Object, fijosMock.Object);
        return (servicio, productosMock, fijosMock);
    }

    private static ProductoMenuDto Producto(int id, string nombre, decimal? precio, decimal costoDirecto) =>
        new()
        {
            Id = id,
            Nombre = nombre,
            PrecioVenta = precio,
            CostoDirecto = costoDirecto,
            Activo = true
        };

    private static CostoFijoDto Fijo(decimal monto) =>
        new() { Id = 1, Nombre = "Arriendo", Categoria = "Arriendo", MontoMensual = monto, Activo = true };
}
