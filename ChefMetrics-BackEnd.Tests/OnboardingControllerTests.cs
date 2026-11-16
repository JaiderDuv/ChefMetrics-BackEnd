using ChefMetrics_BackEnd.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class OnboardingControllerTests
{
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Guarda_datos_personales_negocio_y_finaliza_onboarding(bool formalized)
    {
        using var database = new TestDatabase();
        var c = CrudControllerTests.WithUser(new OnboardingController(database.Context, NullLogger<OnboardingController>.Instance));
        var user = database.Account.UsuarioEmprendedor!;
        user.TipoFormalizacion = formalized ? "formalizado" : "no_formalizado";
        user.OnboardingCompletado = false;
        Assert.IsType<OkObjectResult>(await c.DatosPersonales());
        Assert.IsType<OkObjectResult>(await c.DatosNegocio());
        Assert.IsType<OkObjectResult>(await c.GuardarDatosPersonales(new() { Nombres = " Ana Maria ", Apellidos = " Perez ", TipoDocumento = " ce ", NumeroDocumento = " 456789 ", Telefono = " 3009999999 " }));
        Assert.Equal("Ana Maria", user.Nombres);
        Assert.Equal(formalized ? null : "CE", user.TipoDocumento);
        Assert.Equal("3009999999", database.Business.TelefonoContacto);
        var dto = new OnboardingController.OnboardingNegocioDto { NombreEmpresa = " Cocina Nueva ", Formalizado = formalized,
            Nit = "900123456", RazonSocial = " Cocina SAS ", Rues = "12345", TieneSitioFisico = true, Direccion = " Calle 1 ",
            CorreoEmpresa = " NEGOCIO@example.com ", TelefonoEmpresa = "3001234567", LogoBase64 = "data:image/png;base64,AQID" };
        Assert.IsType<OkObjectResult>(await c.GuardarDatosNegocio(dto));
        Assert.Equal("Cocina Nueva", user.NombreEmprendimiento);
        Assert.Equal(formalized ? "900123456" : null, user.Nit);
        Assert.Equal("negocio@example.com", database.Business.CorreoContacto);
        Assert.Equal("Calle 1", database.Business.Direccion);
        Assert.Equal(dto.LogoBase64, database.Business.LogoUrl);
        Assert.IsType<OkObjectResult>(await c.GuardarFotoPerfil(new() { FotoPerfilUrl = " data:image/png;base64,AQID " }));
        Assert.Equal("data:image/png;base64,AQID", user.FotoPerfilUrl);
        Assert.IsType<OkObjectResult>(await c.GuardarFotoPerfil(new()));
        Assert.Null(user.FotoPerfilUrl);
        Assert.IsType<OkObjectResult>(await c.Completar());
        Assert.True(user.OnboardingCompletado);
        database.Context.ChangeTracker.Clear();
        Assert.Equal("Cocina Nueva", (await database.Context.UsuarioEmprendedors.SingleAsync()).NombreEmprendimiento);
    }

    [Theory]
    [InlineData(null)][InlineData("not-a-number")][InlineData("99")]
    public async Task No_expone_perfiles_ante_identidad_ausente_o_desconocida(string? account)
    {
        using var database = new TestDatabase();
        var c = CrudControllerTests.WithUser(new OnboardingController(database.Context, NullLogger<OnboardingController>.Instance), account);
        var results = new[] { await c.DatosPersonales(), await c.DatosNegocio(), await c.GuardarDatosPersonales(new()),
            await c.GuardarDatosNegocio(new()), await c.GuardarFotoPerfil(new()), await c.Completar() };
        foreach (var result in results)
        {
            var status = result is ObjectResult obj ? obj.StatusCode : ((StatusCodeResult)result).StatusCode;
            Assert.Equal(account == "99" ? 404 : 401, status);
        }
        Assert.Equal("Ana", database.Account.UsuarioEmprendedor!.Nombres);
    }

    [Theory]
    [InlineData("tipo")][InlineData("documento")][InlineData("nit")][InlineData("direccion")][InlineData("logo")][InlineData("foto")]
    public async Task Rechaza_datos_incompletos_y_fotos_invalidas(string failure)
    {
        using var database = new TestDatabase();
        var c = CrudControllerTests.WithUser(new OnboardingController(database.Context, NullLogger<OnboardingController>.Instance));
        IActionResult result;
        if (failure is "tipo" or "documento") result = await c.GuardarDatosPersonales(new() { TipoDocumento = failure == "tipo" ? "XX" : "CC" });
        else if (failure == "foto") result = await c.GuardarFotoPerfil(new() { FotoPerfilUrl = "https://example.com/photo.png" });
        else result = await c.GuardarDatosNegocio(new() { NombreEmpresa = "Cocina", CorreoEmpresa = "a@b.co", TelefonoEmpresa = "3001234567",
            Formalizado = failure == "nit", TieneSitioFisico = failure == "direccion", LogoBase64 = failure == "logo" ? "invalid" : null });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Crea_negocio_faltante_con_categoria_configurada()
    {
        using var database = new TestDatabase();
        database.Context.Insumos.Remove(database.Ingredient);
        database.Context.Negocios.Remove(database.Business);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        var c = CrudControllerTests.WithUser(new OnboardingController(database.Context, NullLogger<OnboardingController>.Instance));
        Assert.IsType<OkObjectResult>(await c.GuardarDatosNegocio(new() { NombreEmpresa = "Nueva", CorreoEmpresa = "nuevo@example.com", TelefonoEmpresa = "3001234567" }));
        var business = await database.Context.Negocios.SingleAsync();
        Assert.Equal(1, business.TipoEmprendimientoId);
        Assert.Equal("nuevo@example.com", business.CorreoContacto);
        Assert.Null(business.Direccion);
    }
}
