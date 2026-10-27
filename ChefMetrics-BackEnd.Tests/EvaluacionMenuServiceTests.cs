using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class EvaluacionMenuServiceTests
{
    private const int CuentaId = 7;
    private const int NegocioId = 3;

    [Fact]
    public async Task Marca_en_riesgo_los_productos_con_margen_directo_bajo_el_umbral()
    {
        var productos = new Mock<IProductoMenuService>(MockBehavior.Strict);
        productos.Setup(p => p.ListarAsync(CuentaId, null, false))
            .ReturnsAsync(new List<ProductoMenuDto>
            {
                new() { Id = 1, Nombre = "Pastel", CostoDirecto = 60m, CostoTotal = 70m, PrecioVenta = 100m, MargenDirecto = 40m },
                new() { Id = 2, Nombre = "Pan", CostoDirecto = 90m, CostoTotal = 100m, PrecioVenta = 100m, MargenDirecto = 10m },
                new() { Id = 3, Nombre = "Sin precio", CostoDirecto = 20m, CostoTotal = 25m }
            });

        var umbral = new Mock<IUmbralMargenRepository>(MockBehavior.Strict);
        umbral.Setup(u => u.ObtenerAsync(NegocioId)).ReturnsAsync(20m);

        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);

        var servicio = new EvaluacionMenuService(umbral.Object, insumos.Object, productos.Object);

        var resultado = await servicio.EvaluarAsync(CuentaId);

        Assert.True(resultado.Exito);
        var dto = Assert.IsType<EvaluacionMenuDto>(resultado.Datos);
        Assert.Equal(20m, dto.UmbralMargenMinimo);
        Assert.Equal(3, dto.Productos.Count);
        Assert.False(dto.Productos.Single(p => p.ProductoId == 1).EnRiesgo);
        Assert.True(dto.Productos.Single(p => p.ProductoId == 2).EnRiesgo);
        var sinPrecio = dto.Productos.Single(p => p.ProductoId == 3);
        Assert.True(sinPrecio.MargenNoCalculable);
        Assert.Contains("precio", sinPrecio.MotivoNoCalculable);
    }

    [Fact]
    public async Task Usa_umbral_por_defecto_cuando_no_hay_configuracion()
    {
        var productos = new Mock<IProductoMenuService>(MockBehavior.Strict);
        productos.Setup(p => p.ListarAsync(CuentaId, null, false))
            .ReturnsAsync(new List<ProductoMenuDto>());

        var umbral = new Mock<IUmbralMargenRepository>(MockBehavior.Strict);
        umbral.Setup(u => u.ObtenerAsync(NegocioId)).ReturnsAsync((decimal?)null);

        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);

        var servicio = new EvaluacionMenuService(umbral.Object, insumos.Object, productos.Object);

        var resultado = await servicio.EvaluarAsync(CuentaId);

        Assert.True(resultado.Exito);
        Assert.Equal(20m, Assert.IsType<EvaluacionMenuDto>(resultado.Datos).UmbralMargenMinimo);
    }
}
