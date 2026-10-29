using System.Text.Json;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class AuthControllerSeguridadTests
{
    [Fact]
    public async Task Recuperacion_responde_igual_exista_o_no_el_correo()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(s => s.SolicitarRecuperacionPasswordAsync(It.IsAny<RecuperarPasswordDto>()))
            .ReturnsAsync(false);

        var controller = new AuthController(
            auth.Object,
            NullLogger<AuthController>.Instance);

        var resultado = await controller.RecuperarPassword(new RecuperarPasswordDto
        {
            CorreoElectronico = "nadie@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        Assert.True(json.RootElement.GetProperty("exito").GetBoolean());
    }

    [Fact]
    public async Task Recuperacion_no_expone_el_mensaje_interno_de_una_excepcion()
    {
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(s => s.SolicitarRecuperacionPasswordAsync(It.IsAny<RecuperarPasswordDto>()))
            .ThrowsAsync(new Exception("detalle SQL secreto"));

        var controller = new AuthController(
            auth.Object,
            NullLogger<AuthController>.Instance);

        var resultado = await controller.RecuperarPassword(new RecuperarPasswordDto
        {
            CorreoElectronico = "nadie@example.com"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
        var cuerpo = JsonSerializer.Serialize(badRequest.Value);
        Assert.DoesNotContain("detalle SQL secreto", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No se pudo procesar", cuerpo, StringComparison.OrdinalIgnoreCase);
    }
}
