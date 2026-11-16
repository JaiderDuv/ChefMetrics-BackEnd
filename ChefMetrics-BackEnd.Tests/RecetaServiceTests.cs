using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class RecetaServiceTests : IDisposable
{
    private readonly Mock<IRecetaRepository> repository = new();
    private readonly Mock<IInsumoRepository> insumos = new();
    private readonly Mock<ICosteoRepository> costs = new();
    private readonly string directory = Path.Combine(Path.GetTempPath(), "ChefMetrics-RecipeTests-" + Guid.NewGuid());
    private readonly RecetaService service;
    private readonly Insumo ingredient = new() { Id = 10, Nombre = "Harina", UnidadMedida = "g", Cantidad = 100, CostoUnitario = 100, Activo = true };
    private readonly Receta recipe = new() { Id = 20, Nombre = "Pan", NegocioId = 2, PorcionesBase = 2 };

    public RecetaServiceTests()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(directory);
        service = new RecetaService(repository.Object, insumos.Object, costs.Object, environment.Object, NullLogger<RecetaService>.Instance);
        recipe.RecetaInsumos.Add(new RecetaInsumo { InsumoId = 10, Insumo = ingredient, Cantidad = 50, UnidadMedida = "g" });
        insumos.Setup(r => r.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        repository.Setup(r => r.ObtenerPorIdAsync(2, 20)).ReturnsAsync(recipe);
        repository.Setup(r => r.ObtenerParaEditarAsync(2, 20)).ReturnsAsync(recipe);
        repository.Setup(r => r.ListarAsync(2, null)).ReturnsAsync(new List<Receta> { recipe });
        repository.Setup(r => r.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo> { ingredient });
        repository.Setup(r => r.CrearAsync(It.IsAny<Receta>())).ReturnsAsync(recipe);
        costs.Setup(r => r.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, PrecioProveedorPrincipal>());
    }

    private static GuardarRecetaDto Valid() => new() { Nombre = " Pan ", Categoria = "panaderia", PorcionesBase = 2, Instrucciones = " Hornear ", Insumos = { new() { InsumoId = 10, Cantidad = 50, UnidadMedida = "g", PorcentajeMerma = 0 } } };

    [Fact]
    public async Task Crear_y_editar_conservan_ingredientes_y_costos_por_porcion()
    {
        var created = await service.CrearAsync(7, Valid());
        Assert.True(created.Exito);
        repository.Verify(r => r.CrearAsync(It.Is<Receta>(r => r.Nombre == "Pan" && r.NegocioId == 2 && r.RecetaInsumos.Count == 1)), Times.Once);
        var edited = await service.ActualizarAsync(7, 20, Valid());
        Assert.True(edited.Exito);
        Assert.Equal("Hornear", recipe.Instrucciones);
        repository.Verify(r => r.ReemplazarInsumos(recipe, It.IsAny<IEnumerable<RecetaInsumo>>()), Times.Once);
        var list = await service.ListarAsync(7, null);
        Assert.Equal(25m, Assert.Single(list!).CostoPorPorcion);
        Assert.True((await service.ObtenerPorIdAsync(7, 20)).Exito);
    }

    [Theory]
    [InlineData("nombre")]
    [InlineData("largo")]
    [InlineData("categoria")]
    [InlineData("sin_insumos")]
    [InlineData("repetido")]
    [InlineData("duplicado")]
    [InlineData("ajeno")]
    [InlineData("cantidad")]
    [InlineData("precision")]
    [InlineData("unidad")]
    [InlineData("incompatible")]
    [InlineData("merma")]
    [InlineData("precision_merma")]
    public async Task Rechaza_recetas_invalidas_antes_de_escribir(string scenario)
    {
        var dto = Valid();
        switch (scenario)
        {
            case "nombre": dto.Nombre = "a"; break;
            case "largo": dto.Nombre = new string('a', 101); break;
            case "categoria": dto.Categoria = "invalida"; break;
            case "sin_insumos": dto.Insumos.Clear(); break;
            case "repetido": dto.Insumos.Add(dto.Insumos[0]); break;
            case "duplicado": repository.Setup(r => r.ExisteNombreAsync(2, "Pan", null)).ReturnsAsync(true); break;
            case "ajeno": repository.Setup(r => r.ObtenerInsumosActivosAsync(2, It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<Insumo>()); break;
            case "cantidad": dto.Insumos[0].Cantidad = 0; break;
            case "precision": dto.Insumos[0].Cantidad = 1.0001m; break;
            case "unidad": dto.Insumos[0].UnidadMedida = "invalida"; break;
            case "incompatible": dto.Insumos[0].UnidadMedida = "ml"; break;
            case "merma": dto.Insumos[0].PorcentajeMerma = 101; break;
            case "precision_merma": dto.Insumos[0].PorcentajeMerma = 0.001m; break;
        }
        Assert.False((await service.CrearAsync(7, dto)).Exito);
        repository.Verify(r => r.CrearAsync(It.IsAny<Receta>()), Times.Never);
    }

    [Theory]
    [InlineData(99, 20)]
    [InlineData(7, 999)]
    public async Task Recurso_ajeno_o_inexistente_no_se_modifica(int account, int id)
    {
        Assert.True((await service.ObtenerPorIdAsync(account, id)).NoEncontrado);
        Assert.True((await service.ActualizarAsync(account, id, Valid())).NoEncontrado);
        Assert.True((await service.EliminarAsync(account, id)).NoEncontrado);
        Assert.True((await service.QuitarImagenAsync(account, id)).NoEncontrado);
        Assert.True((await service.GuardarImagenAsync(account, id, null!)).NoEncontrado);
        repository.Verify(r => r.GuardarCambiosAsync(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Eliminar_respeta_los_productos_asociados(int products)
    {
        repository.Setup(r => r.ContarProductosMenuAsync(20)).ReturnsAsync(products);
        var result = await service.EliminarAsync(7, 20);
        Assert.Equal(products == 0, result.Exito);
        Assert.Equal(products > 0, result.BloqueadaPorMenu);
        repository.Verify(r => r.Eliminar(recipe), products == 0 ? Times.Once : Times.Never);
    }

    [Theory]
    [InlineData("png", true)]
    [InlineData("jpg", true)]
    [InlineData("webp", true)]
    [InlineData("exe", false)]
    [InlineData("falsa", false)]
    public async Task Imagen_valida_se_guarda_y_se_elimina(string kind, bool valid)
    {
        byte[] bytes = kind switch
        {
            "png" => [0x89, 0x50, 0x4e, 0x47, 0, 0, 0, 0, 0, 0, 0, 0],
            "jpg" => [0xff, 0xd8, 0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            "webp" => [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50],
            _ => new byte[12]
        };
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "imagen", kind == "falsa" ? "fake.png" : "file." + kind)
        { Headers = new HeaderDictionary(), ContentType = kind == "jpg" ? "image/jpeg" : "image/" + (kind == "falsa" ? "png" : kind) };
        var result = await service.GuardarImagenAsync(7, 20, file);
        Assert.Equal(valid, result.Exito);
        if (valid)
        {
            var saved = Path.Combine(directory, recipe.ImagenUrl!.TrimStart('/'));
            Assert.True(File.Exists(saved));
            Assert.True((await service.QuitarImagenAsync(7, 20)).Exito);
            Assert.Null(recipe.ImagenUrl);
            Assert.False(File.Exists(saved));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
