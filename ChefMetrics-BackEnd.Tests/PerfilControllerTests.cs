using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class PerfilControllerTests
{
    [Fact]
    public async Task Perfil_pasa_identidad_y_token_y_devuelve_datos_del_servicio()
    {
        var service = new Mock<IPerfilEmpreService>(); var profile = new PerfilEmprendedorDto { CorreoCuenta = "ana@example.com" };
        var permission = new PermisoEdicionPerfilDto { EditToken = "edit" }; var personal = new PerfilUsuarioDto { Nombres = "Ana" };
        service.Setup(x => x.ConfirmarIdentidadAsync(7, It.IsAny<ConfirmarEdicionPerfilDto>())).ReturnsAsync(permission);
        service.Setup(x => x.ObtenerPerfilAsync(7, "edit")).ReturnsAsync(profile);
        service.Setup(x => x.ObtenerPerfilUsuarioAsync(7)).ReturnsAsync(personal);
        service.SetReturnsDefault(Task.FromResult(new ActualizarPerfilResultadoDto { Exito = true }));
        service.Setup(x => x.ObtenerRolesAsync()).ReturnsAsync(new List<RolDto> { new() { Id = 1, Nombre = "Chef" } });
        var c = CrudControllerTests.WithUser(new PerfilEmprendimientoController(service.Object));
        Assert.Same(permission, Assert.IsType<OkObjectResult>(await c.ConfirmarIdentidad(new())).Value);
        Assert.Same(profile, Assert.IsType<OkObjectResult>(await c.ObtenerPerfil("edit")).Value);
        Assert.Same(personal, Assert.IsType<OkObjectResult>(await c.ObtenerPerfilUsuario()).Value);
        Assert.IsType<OkObjectResult>(await c.ObtenerRoles());
        Assert.IsType<OkObjectResult>(await c.ActualizarPerfil("edit", profile));
        Assert.IsType<OkObjectResult>(await c.ActualizarPerfilUsuario(personal));
        Assert.IsType<OkObjectResult>(await c.RevocarPermiso());
        service.Verify(x => x.ActualizarPerfilAsync(7, "edit", profile), Times.Once);
        service.Verify(x => x.RevocarPermisoAsync(7), Times.Once);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Permisos_invalidos_exigen_autenticacion_y_datos_invalidos_devuelven_400(bool reauthenticate)
    {
        var service = new Mock<IPerfilEmpreService>();
        service.SetReturnsDefault(Task.FromResult(new ActualizarPerfilResultadoDto { Exito = false, RequiereReautenticacion = reauthenticate }));
        var c = CrudControllerTests.WithUser(new PerfilEmprendimientoController(service.Object));
        Assert.IsType<UnauthorizedObjectResult>(await c.ConfirmarIdentidad(new()));
        Assert.IsType<UnauthorizedObjectResult>(await c.ObtenerPerfil(null));
        Assert.IsType<NotFoundObjectResult>(await c.ObtenerPerfilUsuario());
        var result = Assert.IsAssignableFrom<ObjectResult>(await c.ActualizarPerfil(null, new()));
        Assert.Equal(reauthenticate ? 401 : 400, result.StatusCode);
        Assert.IsType<BadRequestObjectResult>(await c.ActualizarPerfilUsuario(new()));
        service.Verify(x => x.ObtenerPerfilAsync(7, ""), Times.Once);
    }

    [Fact]
    public async Task Sin_identidad_no_invoca_servicio_de_perfil()
    {
        var service = new Mock<IPerfilEmpreService>(); var c = CrudControllerTests.WithUser(new PerfilEmprendimientoController(service.Object), null);
        Assert.All(new[] { await c.ConfirmarIdentidad(new()), await c.ObtenerPerfil(null), await c.ObtenerRoles(), await c.RevocarPermiso(),
            await c.ActualizarPerfil(null, new()), await c.ObtenerPerfilUsuario(), await c.ActualizarPerfilUsuario(new()) }, r => Assert.IsType<UnauthorizedResult>(r));
        Assert.Empty(service.Invocations);
    }
}
