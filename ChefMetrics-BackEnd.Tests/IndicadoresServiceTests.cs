using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class IndicadoresServiceTests
{
    [Theory]
    [InlineData(50, 100, 200, true)][InlineData(25, 100, 133.33, true)]
    [InlineData(0, 100, 0, false)][InlineData(100, 100, 0, false)][InlineData(50, 0, 0, false)]
    public async Task Precio_producto_aplica_margen_sobre_venta(decimal margin, decimal cost, decimal price, bool valid)
    {
        var costs = new Mock<ICostoProductoService>();
        var result = ResultadoCosteo<CostoProductoDto>.Ok(new() { ProductoId = 30, Producto = "Pan", CostoTotal = cost, CostoDirecto = cost,
            Advertencias = new() { "No seleccionaste ningún costo fijo", "Revisar insumos" } });
        costs.Setup(x => x.CalcularAsync(7, 30, null, null)).ReturnsAsync(result);
        costs.Setup(x => x.CalcularPreviewAsync(7, It.IsAny<PreviewCostoProductoDto>())).ReturnsAsync(result);
        var service = new SugerirPrecioProductoService(costs.Object);
        var saved = await service.CalcularAsync(7, 30, margin);
        var preview = await service.CalcularPreviewAsync(7, new() { MargenDeseado = margin });
        foreach (var r in new[] { saved, preview }) {
            Assert.True(r.Exito); Assert.Equal(valid, r.Datos!.MargenValido);
            if (valid) Assert.Equal(price, r.Datos.PrecioSugerido);
            Assert.DoesNotContain(r.Datos.Advertencias, a => a.StartsWith("No seleccionaste")); Assert.Contains("Revisar insumos", r.Datos.Advertencias); }
        Assert.Equal(30, saved.Datos!.ProductoId); Assert.Null(preview.Datos!.ProductoId);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Precio_propaga_fallo_de_costeo(bool missing)
    {
        var costs = new Mock<ICostoProductoService>();
        costs.SetReturnsDefault(Task.FromResult(new ResultadoCosteo<CostoProductoDto> { NoEncontrado = missing, Mensaje = "Sin datos" }));
        var service = new SugerirPrecioProductoService(costs.Object);
        foreach (var result in new[] { await service.CalcularAsync(7, 30, 50), await service.CalcularPreviewAsync(7, new()) }) {
            Assert.False(result.Exito); Assert.Equal(missing, result.NoEncontrado); Assert.Equal("Sin datos", result.Mensaje); }
    }

    [Fact]
    public async Task Equilibrio_usa_margen_positivo_y_excluye_productos_sin_costo()
    {
        var products = new Mock<IProductoMenuService>(); var fixedCosts = new Mock<ICostosFijosService>();
        fixedCosts.Setup(x => x.ListarAsync(7, false)).ReturnsAsync(new List<CostoFijoDto> { new() { MontoMensual = 1000, Activo = true }, new() { MontoMensual = 9000, Activo = false } });
        products.Setup(x => x.ListarAsync(7, null, false)).ReturnsAsync(new List<ProductoMenuDto> {
            new() { Id = 1, Nombre = "Pan", PrecioVenta = 100, CostoDirecto = 50 }, new() { Id = 2, PrecioVenta = 100, CostoDirecto = 0 },
            new() { Id = 3, PrecioVenta = 10, CostoDirecto = 20 }, new() { Id = 4, PrecioVenta = null } });
        var result = await new PuntoEquilibrioService(products.Object, fixedCosts.Object).CalcularAsync(7);
        Assert.True(result.Exito); var data = result.Datos!;
        Assert.Equal(1000, data.CostosFijosMensuales); Assert.Equal(2000, data.VentasEquilibrioMensuales);
        Assert.Equal(3, data.ProductosConsiderados); Assert.Equal(50, data.MargenContribucionPromedioPorcentaje);
        Assert.Equal(20, data.Productos.Single(x => x.ProductoId == 1).UnidadesEquilibrio);
        Assert.Null(data.Productos.Single(x => x.ProductoId == 2).UnidadesEquilibrio);
        Assert.Contains(data.Advertencias, a => a.Contains("no tienen costo"));
    }

    [Theory]
    [InlineData("sin-negocio")][InlineData("sin-productos")][InlineData("sin-precio")][InlineData("sin-margen")]
    public async Task Equilibrio_advierte_cuando_no_es_calculable(string scenario)
    {
        var products = new Mock<IProductoMenuService>(); var costs = new Mock<ICostosFijosService>();
        costs.Setup(x => x.ListarAsync(7, false)).ReturnsAsync(scenario == "sin-negocio" ? null : new List<CostoFijoDto>());
        products.Setup(x => x.ListarAsync(7, null, false)).ReturnsAsync(scenario == "sin-productos" ? null : scenario == "sin-margen" ? new List<ProductoMenuDto> { new() { PrecioVenta = 10, CostoDirecto = 20 } } : new());
        var result = await new PuntoEquilibrioService(products.Object, costs.Object).CalcularAsync(7);
        if (scenario is "sin-negocio" or "sin-productos") Assert.True(result.NoEncontrado);
        else { Assert.True(result.Exito); Assert.Null(result.Datos!.VentasEquilibrioMensuales); Assert.NotEmpty(result.Datos.Advertencias); }
    }
}
