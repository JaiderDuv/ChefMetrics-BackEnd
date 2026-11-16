using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CostoProductoLoteTests
{
    private readonly Mock<IProductoMenuRepository> products = new();
    private readonly Mock<ICosteoRepository> costs = new();
    private readonly Mock<IInsumoRepository> ingredients = new();
    private readonly Receta recipe;
    private readonly Insumo ingredient = new() { Id = 10, Nombre = "Harina", UnidadMedida = "g", Cantidad = 100, CostoUnitario = 100, Activo = true };
    private readonly Insumo box = new() { Id = 11, Nombre = "Caja", UnidadMedida = "unidad", Cantidad = 1, CostoUnitario = 5, Activo = true };
    private readonly CostoProductoService service;
    public CostoProductoLoteTests()
    {
        recipe = new() { Id = 20, Nombre = "Pan", PorcionesBase = 2, RecetaInsumos = { new() { InsumoId = 10, Insumo = ingredient, Cantidad = 20, UnidadMedida = "g" } } };
        ingredients.Setup(x => x.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        products.Setup(x => x.ObtenerRecetasConInsumosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Receta> { recipe });
        products.Setup(x => x.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo> { box });
        costs.Setup(x => x.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, PrecioProveedorPrincipal>());
        costs.Setup(x => x.ObtenerConfiguracionCostosAsync(2)).ReturnsAsync((new List<CostoFijo> { new() { Id = 1, MontoMensual = 1000 }, new() { Id = 2, MontoMensual = 1000 } }, (int?)100));
        service = new(products.Object, costs.Object, ingredients.Object);
    }
    private static ProductoMenu Product(int id, decimal portions) => new() { Id = id, Nombre = "Combo", PrecioVenta = 100,
        ProductoMenuReceta = { new() { RecetaId = 20, Cantidad = portions } },
        ProductoMenuInsumos = { new() { InsumoId = 11, Cantidad = 1, UnidadMedida = "unidad" } } };

    [Theory]
    [InlineData("todos", 20)][InlineData("uno", 10)][InlineData("ninguno", 0)][InlineData("ajeno", 0)]
    public async Task Lote_costea_porciones_y_empaques_cargando_cada_conjunto_una_vez(string selection, decimal fixedAmount)
    {
        List<int>? ids = selection switch { "todos" => null, "uno" => new() { 1 }, "ninguno" => new(), _ => new() { 99 } };
        var result = await service.CalcularLoteAsync(2, new[] { Product(30, 2), Product(31, 1), new ProductoMenu { Id = 32 } }, ids);
        Assert.Equal(2, result.Count); Assert.False(result.ContainsKey(32));
        Assert.Equal(25, result[30].CostoDirecto); Assert.Equal(fixedAmount, result[30].CostoFijoProrrateado);
        Assert.Equal(25 + fixedAmount, result[30].CostoTotal); Assert.Equal(75, result[30].MargenDirecto);
        Assert.Equal(75 - fixedAmount, result[30].MargenReal);
        Assert.Equal(15, result[31].CostoDirecto); Assert.Equal(fixedAmount / 2, result[31].CostoFijoProrrateado);
        products.Verify(x => x.ObtenerRecetasConInsumosAsync(2, It.Is<IEnumerable<int>>(v => v.SequenceEqual(new[] { 20 }))), Times.Once);
        products.Verify(x => x.ObtenerInsumosActivosAsync(2, It.Is<IEnumerable<int>>(v => v.SequenceEqual(new[] { 11 }))), Times.Once);
        costs.Verify(x => x.ObtenerConfiguracionCostosAsync(2), Times.Once);
        costs.Verify(x => x.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task Lote_vacio_no_consulta_repositorios()
    {
        Assert.Empty(await service.CalcularLoteAsync(2, Array.Empty<ProductoMenu>()));
        Assert.Empty(products.Invocations); Assert.Empty(costs.Invocations);
    }

    [Theory]
    [InlineData("receta-ausente")][InlineData("porciones")][InlineData("unidad-receta")][InlineData("insumo-ausente")][InlineData("unidad-empaque")][InlineData("divisor")]
    public async Task Datos_incompletos_generan_advertencias_sin_inventar_costos(string scenario)
    {
        var p = Product(30, 2);
        if (scenario == "receta-ausente") products.Setup(x => x.ObtenerRecetasConInsumosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Receta>());
        if (scenario == "porciones") recipe.PorcionesBase = 0;
        if (scenario == "unidad-receta") recipe.RecetaInsumos.Single().UnidadMedida = "l";
        if (scenario == "insumo-ausente") products.Setup(x => x.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo>());
        if (scenario == "unidad-empaque") p.ProductoMenuInsumos.Single().UnidadMedida = "l";
        if (scenario == "divisor") costs.Setup(x => x.ObtenerConfiguracionCostosAsync(2)).ReturnsAsync((new List<CostoFijo> { new() { MontoMensual = 1000 } }, (int?)null));
        var result = (await service.CalcularLoteAsync(2, new[] { p }))[30];
        Assert.NotEmpty(result.Advertencias);
        if (scenario is "receta-ausente" or "porciones" or "unidad-receta") Assert.Equal(0, result.CostoRecetas);
        if (scenario is "insumo-ausente" or "unidad-empaque") Assert.Equal(0, result.CostoInsumosDirectos);
        if (scenario is "divisor" or "receta-ausente" or "porciones") Assert.Equal(0, result.CostoFijoProrrateado);
    }

    [Theory]
    [InlineData("vacio")][InlineData("muchas")][InlineData("repetida")][InlineData("id")][InlineData("cantidad")][InlineData("decimales")]
    [InlineData("insumo-repetido")][InlineData("insumo-id")][InlineData("insumo-cantidad")][InlineData("insumo-unidad")]
    public async Task Preview_valida_composicion_antes_de_consultar_costos(string scenario)
    {
        var dto = new PreviewCostoProductoDto { Recetas = new() { new() { RecetaId = 20, Cantidad = 1 } } };
        switch (scenario) {
            case "vacio": dto.Recetas.Clear(); break;
            case "muchas": dto.Recetas = Enumerable.Range(1, 51).Select(id => new RecetaEnProductoDto { RecetaId = id, Cantidad = 1 }).ToList(); break;
            case "repetida": dto.Recetas.Add(new() { RecetaId = 20, Cantidad = 1 }); break;
            case "id": dto.Recetas[0].RecetaId = 0; break;
            case "cantidad": dto.Recetas[0].Cantidad = 0; break;
            case "decimales": dto.Recetas[0].Cantidad = 1.0001m; break;
            default: dto.InsumosDirectos.Add(new() { InsumoId = scenario == "insumo-id" ? 0 : 11, Cantidad = scenario == "insumo-cantidad" ? 0 : 1, UnidadMedida = scenario == "insumo-unidad" ? "XX" : "unidad" });
                if (scenario == "insumo-repetido") dto.InsumosDirectos.Add(new() { InsumoId = 11 }); break;
        }
        var result = await service.CalcularPreviewAsync(7, dto);
        Assert.False(result.Exito); Assert.NotEmpty(result.Mensaje); Assert.Empty(products.Invocations); Assert.Empty(costs.Invocations);
    }
}
