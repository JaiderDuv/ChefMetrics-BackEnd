using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class ProductoMenuServiceTests
{
    private readonly Mock<IProductoMenuRepository> repository = new();
    private readonly Mock<IInsumoRepository> insumos = new();
    private readonly Mock<ICostoProductoService> costs = new();
    private readonly ProductoMenuService service;
    private readonly ProductoMenu product = new() { Id = 30, NegocioId = 2, Nombre = "Pan", PrecioVenta = 200, Activo = true };
    private readonly CostoProductoDto cost = new() { ProductoId = 30, Producto = "Pan", PrecioVenta = 200, CostoTotal = 100, CostoDirecto = 80 };

    public ProductoMenuServiceTests()
    {
        service = new ProductoMenuService(repository.Object, insumos.Object, costs.Object, Mock.Of<IWebHostEnvironment>(), NullLogger<ProductoMenuService>.Instance);
        insumos.Setup(r => r.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        repository.Setup(r => r.ObtenerPorIdAsync(2, 30)).ReturnsAsync(product);
        repository.Setup(r => r.ObtenerParaEditarAsync(2, 30)).ReturnsAsync(product);
        repository.Setup(r => r.ListarAsync(2, null, false)).ReturnsAsync(new List<ProductoMenu> { product });
        repository.Setup(r => r.ObtenerRecetasConInsumosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Receta> { new() { Id = 20 } });
        repository.Setup(r => r.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo> { new() { Id = 10 } });
        repository.Setup(r => r.CrearAsync(It.IsAny<ProductoMenu>())).ReturnsAsync(product);
        costs.Setup(c => c.CalcularAsync(7, 30, null, null)).ReturnsAsync(ResultadoCosteo<CostoProductoDto>.Ok(cost));
        costs.Setup(c => c.CalcularLoteAsync(2, It.IsAny<IReadOnlyCollection<ProductoMenu>>(), null)).ReturnsAsync(new Dictionary<int, CostoProductoDto> { [30] = cost });
    }

    private static CrearProductoMenuDto Valid() => new() { Nombre = "Pan", PrecioVenta = 200, Recetas = { new() { RecetaId = 20, Cantidad = 1 } }, InsumosDirectos = { new() { InsumoId = 10, Cantidad = 1, UnidadMedida = "unidad" } } };

    [Fact]
    public async Task Listado_y_detalle_reflejan_costos_y_margenes()
    {
        var row = Assert.Single((await service.ListarAsync(7, null, false))!);
        Assert.Equal(50, row.MargenReal);
        Assert.Equal(60, row.MargenDirecto);
        Assert.True((await service.ObtenerAsync(7, 30)).Exito);
        costs.Verify(c => c.CalcularLoteAsync(2, It.IsAny<IReadOnlyCollection<ProductoMenu>>(), null), Times.Once);
    }

    [Fact]
    public async Task Crear_actualizar_y_patch_conservan_las_relaciones()
    {
        var dto = Valid();
        Assert.True((await service.CrearAsync(7, dto)).Exito);
        repository.Verify(r => r.CrearAsync(It.Is<ProductoMenu>(p => p.NegocioId == 2 && p.ProductoMenuReceta.Count == 1 && p.ProductoMenuInsumos.Count == 1)), Times.Once);
        Assert.True((await service.ActualizarAsync(7, 30, new ActualizarProductoMenuDto { Nombre = "Pan integral", PrecioVenta = 300, Activo = true, Recetas = dto.Recetas, InsumosDirectos = dto.InsumosDirectos })).Exito);
        Assert.Equal("Pan integral", product.Nombre);
        Assert.True((await service.ActualizarParcialAsync(7, 30, new PatchProductoMenuDto { Nombre = "Pan grande", PrecioVenta = 400, Activo = false, Recetas = dto.Recetas, InsumosDirectos = dto.InsumosDirectos })).Exito);
        Assert.Equal(400, product.PrecioVenta);
        Assert.False(product.Activo);
        Assert.True((await service.ActualizarParcialAsync(7, 30, new PatchProductoMenuDto { QuitarPrecioVenta = true })).Exito);
        Assert.Null(product.PrecioVenta);
        Assert.True((await service.EliminarAsync(7, 30)).Exito);
        repository.Verify(r => r.Eliminar(product), Times.Once);
    }

    [Theory]
    [InlineData("nombre")]
    [InlineData("nombre_largo")]
    [InlineData("recetas_vacias")]
    [InlineData("repetida")]
    [InlineData("receta_id")]
    [InlineData("cantidad")]
    [InlineData("precision")]
    [InlineData("cantidad_tope")]
    [InlineData("precio")]
    [InlineData("precio_tope")]
    [InlineData("precio_precision")]
    [InlineData("directo_id")]
    [InlineData("directo_repetido")]
    [InlineData("directo_cantidad")]
    [InlineData("directo_unidad")]
    [InlineData("directo_ajeno")]
    public async Task Producto_invalido_no_se_persiste(string scenario)
    {
        var dto = Valid();
        switch (scenario)
        {
            case "nombre": dto.Nombre = "a"; break;
            case "nombre_largo": dto.Nombre = new string('a', 151); break;
            case "recetas_vacias": dto.Recetas.Clear(); break;
            case "repetida": dto.Recetas.Add(dto.Recetas[0]); break;
            case "receta_id": dto.Recetas[0].RecetaId = 0; break;
            case "cantidad": dto.Recetas[0].Cantidad = 0; break;
            case "precision": dto.Recetas[0].Cantidad = 0.0001m; break;
            case "cantidad_tope": dto.Recetas[0].Cantidad = 10001; break;
            case "precio": dto.PrecioVenta = 0; break;
            case "precio_tope": dto.PrecioVenta = 1000000; break;
            case "precio_precision": dto.PrecioVenta = 1.001m; break;
            case "directo_id": dto.InsumosDirectos[0].InsumoId = 0; break;
            case "directo_repetido": dto.InsumosDirectos.Add(dto.InsumosDirectos[0]); break;
            case "directo_cantidad": dto.InsumosDirectos[0].Cantidad = 0; break;
            case "directo_unidad": dto.InsumosDirectos[0].UnidadMedida = "invalida"; break;
            case "directo_ajeno": repository.Setup(r => r.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo>()); break;
        }
        Assert.False((await service.CrearAsync(7, dto)).Exito);
        repository.Verify(r => r.CrearAsync(It.IsAny<ProductoMenu>()), Times.Never);
    }

    [Fact]
    public async Task Cuenta_sin_negocio_y_patch_vacio_no_guardan()
    {
        Assert.Null(await service.ListarAsync(99, null, false));
        Assert.True((await service.ObtenerAsync(99, 30)).NoEncontrado);
        Assert.True((await service.CrearAsync(99, Valid())).NoEncontrado);
        Assert.True((await service.ActualizarAsync(99, 30, new ActualizarProductoMenuDto())).NoEncontrado);
        Assert.True((await service.ActualizarParcialAsync(99, 30, new PatchProductoMenuDto())).NoEncontrado);
        Assert.True((await service.EliminarAsync(99, 30)).NoEncontrado);
        Assert.False((await service.ActualizarParcialAsync(7, 30, new PatchProductoMenuDto())).Exito);
        repository.Verify(r => r.GuardarCambiosAsync(), Times.Never);
    }
}
