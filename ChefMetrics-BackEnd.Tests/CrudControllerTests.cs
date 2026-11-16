using System.Security.Claims;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

// Invocación directa: comprueba la traducción a HTTP y el contexto de cuenta.
// Los filtros de autorización y el model binding requieren pruebas HTTP aparte.
public sealed class CrudControllerTests
{
    internal static T WithUser<T>(T controller, string? id = "7", string claim = ClaimTypes.NameIdentifier) where T : ControllerBase
    {
        controller.ControllerContext = new() { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(id is null ? Array.Empty<Claim>() : new[] { new Claim(claim, id) }, "test")) } };
        return controller;
    }
    private static void Status(IActionResult result, int expected)
    {
        var actual = result is ObjectResult obj ? obj.StatusCode : ((StatusCodeResult)result).StatusCode;
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("ok", 200)][InlineData("missing", 404)][InlineData("invalid", 400)][InlineData("anonymous", 401)]
    public async Task Insumos_traduce_resultados_y_pasa_cuenta_autenticada(string scenario, int expected)
    {
        var repo = new Mock<IInsumoService>();
        var success = scenario == "ok"; var missing = scenario == "missing";
        var dto = new InsumoDto { Id = 10, Nombre = "Harina" };
        repo.SetReturnsDefault(Task.FromResult(new ResultadoInsumoDto { Exito = success, NoEncontrado = missing, Insumo = dto, Mensaje = "resultado" }));
        repo.SetReturnsDefault(Task.FromResult(new ResultadoEliminarInsumoDto { Exito = success, NoEncontrado = missing }));
        repo.SetReturnsDefault(Task.FromResult(new ResultadoHistorialDto { NoEncontrado = missing }));
        repo.SetReturnsDefault(Task.FromResult(new ResultadoHistorialGeneralDto { NoEncontrado = missing }));
        repo.SetReturnsDefault(Task.FromResult<ListaPaginadaDto<InsumoDto>?>(missing ? null : new()));
        var controller = WithUser(new InsumosController(repo.Object), scenario == "anonymous" ? null : "7");
        Status(await controller.Crear(new()), success ? 201 : expected);
        Status(await controller.ActualizarParcial(10, new()), expected);
        Status(await controller.Restaurar(10), expected);
        Status(await controller.Eliminar(10), expected);
        var readStatus = scenario == "invalid" ? 200 : expected;
        Status(await controller.Listar(new()), readStatus);
        Status(await controller.ObtenerPorId(10), readStatus);
        Status(await controller.ObtenerHistorialPrecios(10), readStatus);
        Status(await controller.ObtenerHistorialGeneral(null, null), readStatus);
        if (success) repo.Verify(x => x.ObtenerPorIdAsync(7, 10), Times.Once);
        if (scenario == "anonymous") Assert.Empty(repo.Invocations);
    }

    [Fact]
    public async Task Insumos_modelo_invalido_y_conflictos_tienen_respuesta_especifica()
    {
        var service = new Mock<IInsumoService>();
        var c = WithUser(new InsumosController(service.Object), "7", "sub");
        c.ModelState.AddModelError("nombre", "obligatorio");
        Status(await c.Crear(new()), 400); Status(await c.ActualizarParcial(10, new()), 400);
        Assert.Empty(service.Invocations);
        c.ModelState.Clear();
        service.Setup(x => x.RestaurarAsync(7, 10)).ReturnsAsync(new ResultadoInsumoDto { Conflicto = true });
        service.Setup(x => x.EliminarAsync(7, 10)).ReturnsAsync(new ResultadoEliminarInsumoDto { BloqueadoPorRecetas = true, NumeroRecetas = 2 });
        Status(await c.Restaurar(10), 409); Status(await c.Eliminar(10), 409);
    }

    [Theory]
    [InlineData("ok", 200)][InlineData("missing", 404)][InlineData("invalid", 400)][InlineData("anonymous", 401)]
    public async Task Recetas_traduce_resultados_y_ubicacion_del_recurso(string scenario, int expected)
    {
        var service = new Mock<IRecetaService>();
        var success = scenario == "ok"; var missing = scenario == "missing";
        service.SetReturnsDefault(Task.FromResult(new ResultadoRecetaDto { Exito = success, NoEncontrado = missing, Receta = new() { Id = 20 } }));
        service.SetReturnsDefault(Task.FromResult(new ResultadoEliminarRecetaDto { Exito = success, NoEncontrado = missing }));
        service.SetReturnsDefault(Task.FromResult<List<RecetaDto>?>(missing ? null : new()));
        var c = WithUser(new RecetasController(service.Object), scenario == "anonymous" ? null : "7");
        var created = await c.Crear(new());
        Status(created, success ? 201 : expected);
        if (success) { var location = Assert.IsType<CreatedAtActionResult>(created); Assert.Equal("ObtenerPorId", location.ActionName); Assert.Equal(20, location.RouteValues!["id"]); }
        Status(await c.Actualizar(20, new()), expected);
        Status(await c.SubirImagen(20, null!), expected);
        Status(await c.Eliminar(20), expected);
        var read = scenario == "invalid" ? 200 : expected;
        Status(await c.ObtenerPorId(20), read); Status(await c.Listar(null), read); Status(await c.QuitarImagen(20), read);
        if (success) service.Verify(x => x.ObtenerPorIdAsync(7, 20), Times.Once);
        if (scenario == "anonymous") Assert.Empty(service.Invocations);
    }

    [Fact]
    public async Task Recetas_rechaza_modelo_invalido_y_bloquea_borrado_usado_en_menu()
    {
        var service = new Mock<IRecetaService>(); var c = WithUser(new RecetasController(service.Object));
        c.ModelState.AddModelError("nombre", "obligatorio");
        Status(await c.Crear(new()), 400); Status(await c.Actualizar(20, new()), 400); Assert.Empty(service.Invocations);
        c.ModelState.Clear();
        service.Setup(x => x.EliminarAsync(7, 20)).ReturnsAsync(new ResultadoEliminarRecetaDto { BloqueadaPorMenu = true, NumeroProductos = 2 });
        Status(await c.Eliminar(20), 409);
    }

    [Theory]
    [InlineData("ok", 200)][InlineData("missing", 404)][InlineData("invalid", 400)][InlineData("anonymous", 401)]
    public async Task Proveedores_traduce_resultados_en_todas_las_operaciones(string scenario, int expected)
    {
        var service = new Mock<IProveedorService>(); var missing = scenario == "missing";
        service.SetReturnsDefault(Task.FromResult(new ResultadoProveedorDto { Exito = scenario == "ok", NoEncontrado = missing, Datos = "proveedores" }));
        service.SetReturnsDefault(Task.FromResult(new ResultadoChequeoEliminarDto { Exito = true, NoEncontrado = missing, Datos = new() { ProveedorId = 3 } }));
        var c = WithUser(new ProveedoresController(service.Object), scenario == "anonymous" ? null : "7");
        var results = new[] { await c.ListarDelNegocio(null), await c.ListarDelNegocio("Mol", 8), await c.Crear(new()),
            await c.ListarPorInsumo(10), await c.ListarDisponibles(10), await c.Asociar(10, new()), await c.CrearYAsociar(10, new()),
            await c.Desasociar(10, 3), await c.MarcarPrincipal(10, 3), await c.ActualizarCosto(10, 3, new()), await c.Actualizar(3, new()), await c.Eliminar(3) };
        Assert.All(results, r => Status(r, expected));
        Status(await c.ChequearEliminar(3), scenario == "invalid" ? 200 : expected);
        if (scenario == "ok") service.Verify(x => x.BuscarProveedoresAsync(7, "Mol", 8), Times.Once);
        if (scenario == "anonymous") Assert.Empty(service.Invocations);
    }

    [Fact]
    public async Task Proveedores_modelo_invalido_no_llega_a_servicio()
    {
        var service = new Mock<IProveedorService>(); var c = WithUser(new ProveedoresController(service.Object));
        c.ModelState.AddModelError("costo", "fuera de rango");
        Assert.All(new[] { await c.Crear(new()), await c.Asociar(10, new()), await c.CrearYAsociar(10, new()), await c.ActualizarCosto(10, 3, new()), await c.Actualizar(3, new()) }, r => Status(r, 400));
        Assert.Empty(service.Invocations);
    }

    [Theory]
    [InlineData("ok", 200)][InlineData("missing", 404)][InlineData("invalid", 400)][InlineData("anonymous", 401)]
    public async Task Productos_traduce_crud_costos_y_precios(string scenario, int expected)
    {
        var service = new Mock<IProductoMenuService>(); var costs = new Mock<ICostoProductoService>(); var prices = new Mock<ISugerirPrecioProductoService>();
        var success = scenario == "ok"; var missing = scenario == "missing";
        service.SetReturnsDefault(Task.FromResult(new ResultadoProductoMenuDto { Exito = success, NoEncontrado = missing, Producto = new() { Id = 30 } }));
        service.SetReturnsDefault(Task.FromResult(new ResultadoCosteo<bool> { Exito = success, NoEncontrado = missing }));
        service.SetReturnsDefault(Task.FromResult<List<ProductoMenuDto>?>(missing ? null : new()));
        costs.SetReturnsDefault(Task.FromResult(new ResultadoCosteo<CostoProductoDto> { Exito = success, NoEncontrado = missing }));
        prices.SetReturnsDefault(Task.FromResult(new ResultadoCosteo<PrecioSugeridoProductoDto> { Exito = success, NoEncontrado = missing }));
        var c = WithUser(new ProductosMenuController(service.Object, costs.Object, prices.Object), scenario == "anonymous" ? null : "7");
        Status(await c.Crear(new()), success ? 201 : expected);
        Assert.All(new[] { await c.Actualizar(30, new()), await c.ActualizarParcial(30, new()), await c.SubirImagen(30, null!), await c.QuitarImagen(30),
            await c.ObtenerCosto(30, null), await c.CostoPreview(new()), await c.PrecioSugerido(30, new()), await c.PrecioPreview(new()) }, r => Status(r, expected));
        var read = scenario == "invalid" ? 200 : expected;
        Status(await c.ObtenerPorId(30), read); Status(await c.Listar(null), read); Status(await c.Eliminar(30), read);
        if (success) costs.Verify(x => x.CalcularAsync(7, 30, null, null), Times.Once);
        if (scenario == "anonymous") { Assert.Empty(service.Invocations); Assert.Empty(costs.Invocations); Assert.Empty(prices.Invocations); }
    }

    [Fact]
    public async Task Productos_no_guarda_modelos_invalidos()
    {
        var service = new Mock<IProductoMenuService>(); var costs = new Mock<ICostoProductoService>(); var prices = new Mock<ISugerirPrecioProductoService>();
        var c = WithUser(new ProductosMenuController(service.Object, costs.Object, prices.Object)); c.ModelState.AddModelError("nombre", "obligatorio");
        Assert.All(new[] { await c.Crear(new()), await c.Actualizar(30, new()), await c.ActualizarParcial(30, new()), await c.CostoPreview(new()), await c.PrecioPreview(new()) }, r => Status(r, 400));
        Assert.Empty(service.Invocations); Assert.Empty(costs.Invocations); Assert.Empty(prices.Invocations);
    }
}
