using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CostosFijosCrudTests
{
    private readonly Mock<ICosteoRepository> repo = new();
    private readonly Mock<IInsumoRepository> ingredients = new();
    private readonly CostoFijo item = new() { Id = 10, NegocioId = 2, Nombre = "Arriendo", Categoria = "Local", MontoMensual = 1000, Activo = true };
    private readonly CostosFijosService service;
    public CostosFijosCrudTests()
    {
        ingredients.Setup(x => x.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        repo.Setup(x => x.ObtenerCostoFijoAsync(2, 10, It.IsAny<bool>())).ReturnsAsync(item);
        repo.Setup(x => x.ListarCostosFijosAsync(2, It.IsAny<bool>())).ReturnsAsync(new List<CostoFijo> { item });
        repo.Setup(x => x.CrearCostoFijoAsync(It.IsAny<CostoFijo>())).ReturnsAsync((CostoFijo c) => { c.Id = 11; return c; });
        service = new(repo.Object, ingredients.Object);
    }

    [Fact]
    public async Task Crud_normaliza_y_persiste_monto_categoria_y_estado()
    {
        var created = await service.CrearAsync(7, new() { Nombre = " Energia ", MontoMensual = 250 });
        Assert.True(created.Exito); Assert.Equal("Energia", created.Datos!.Nombre); Assert.Equal("Otros", created.Datos.Categoria);
        Assert.Equal(1000, (await service.ObtenerAsync(7, 10)).Datos!.MontoMensual);
        Assert.Single((await service.ListarAsync(7, false))!);
        Assert.Single((await service.ObtenerConfiguracionAsync(7, true)).Datos!.Costos);
        var updated = await service.ActualizarAsync(7, 10, new() { Nombre = " Alquiler ", Categoria = " Local ", MontoMensual = 1500.25m, Activo = false });
        Assert.True(updated.Exito); Assert.Equal("Alquiler", item.Nombre); Assert.Equal("Local", item.Categoria); Assert.Equal(1500.25m, item.MontoMensual); Assert.False(item.Activo);
        Assert.True((await service.EliminarAsync(7, 10)).Exito);
        repo.Verify(x => x.EliminarCostoFijo(item), Times.Once);
        repo.Verify(x => x.GuardarCambiosAsync(), Times.Exactly(2));
    }

    [Theory]
    [InlineData("nombre")][InlineData("categoria")][InlineData("monto")][InlineData("decimales")][InlineData("duplicado")]
    public async Task Crear_y_actualizar_invalidos_no_guardan(string failure)
    {
        var name = failure == "nombre" ? "x" : "Nuevo";
        var category = failure == "categoria" ? "x" : "Local";
        var amount = failure == "monto" ? 0 : failure == "decimales" ? 1.001m : 100m;
        if (failure == "duplicado") repo.Setup(x => x.ExisteNombreCostoFijoAsync(2, name, It.IsAny<int?>())).ReturnsAsync(true);
        Assert.False((await service.CrearAsync(7, new() { Nombre = name, Categoria = category, MontoMensual = amount })).Exito);
        Assert.False((await service.ActualizarAsync(7, 10, new() { Nombre = name, Categoria = category, MontoMensual = amount })).Exito);
        repo.Verify(x => x.CrearCostoFijoAsync(It.IsAny<CostoFijo>()), Times.Never);
        repo.Verify(x => x.GuardarCambiosAsync(), Times.Never);
    }

    [Fact]
    public async Task Ausencia_de_negocio_y_costo_o_patch_vacio_no_escriben()
    {
        Assert.Null(await service.ListarAsync(99, false));
        Assert.True((await service.ObtenerConfiguracionAsync(99, false)).NoEncontrado);
        Assert.True((await service.ActualizarPreparacionesBaseMesAsync(99, new())).NoEncontrado);
        Assert.True((await service.ActualizarPreparacionesBaseMesAsync(7, new() { PreparacionesBaseMes = 10 })).NoEncontrado);
        foreach (var account in new[] { 7, 99 }) {
            Assert.True((await service.ObtenerAsync(account, 99)).NoEncontrado);
            Assert.True((await service.ActualizarAsync(account, 99, new())).NoEncontrado);
            Assert.True((await service.EliminarAsync(account, 99)).NoEncontrado); }
        Assert.True((await service.CrearAsync(99, new())).NoEncontrado);
        Assert.False((await service.ActualizarAsync(7, 10, new())).Exito);
        repo.Verify(x => x.GuardarCambiosAsync(), Times.Never);
    }
}
