using System.Text.Json;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CuentaControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registro_traduce_validacion_resultado_y_conflicto(bool formalizado)
    {
        var service = new Mock<IUsuarioService>(MockBehavior.Strict);
        var controller = new UsuariosController(service.Object);
        var individual = new Emprendedor();
        var business = new EmprendedorFormalizado();
        Task<IActionResult> Register() => formalizado
            ? controller.RegistrarFormalizado(business)
            : controller.RegistrarNoFormalizado(individual);

        controller.ModelState.AddModelError("correo", "Correo inválido");
        Assert.IsType<BadRequestObjectResult>(await Register());
        controller.ModelState.Clear();
        individual.NombreEmprendimiento = business.NombreEmprendimiento = new string('A', 101);
        Assert.IsType<BadRequestResult>(await Register());
        Assert.Empty(service.Invocations);

        individual.NombreEmprendimiento = business.NombreEmprendimiento = "Cocina";
        service.SetupSequence(s => s.RegistrarEmprendedorAsync(individual))
            .ReturnsAsync(true).ThrowsAsync(new InvalidOperationException("Correo registrado"));
        service.SetupSequence(s => s.RegistrarEmprendedorFormalizadoAsync(business))
            .ReturnsAsync(true).ThrowsAsync(new InvalidOperationException("Correo registrado"));
        Assert.Contains("Registro exitoso", JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(await Register()).Value));
        Assert.Contains("Correo registrado", JsonSerializer.Serialize(Assert.IsType<BadRequestObjectResult>(await Register()).Value));
    }

    [Fact]
    public async Task Activar_cuenta_distingue_token_expirado_y_reenvio_fallido()
    {
        var service = new Mock<IUsuarioService>(MockBehavior.Strict);
        var controller = new UsuariosController(service.Object);
        var token = new TokenDto { Token = "verificacion" };
        var resend = new ReenviarCorreoDto { Correo = "ana@example.com" };
        controller.ModelState.AddModelError("correo", "Inválido");
        Assert.IsType<BadRequestObjectResult>(await controller.ValidarToken(token));
        Assert.IsType<BadRequestObjectResult>(await controller.ReenviarVerificacion(resend));
        Assert.Empty(service.Invocations);
        controller.ModelState.Clear();

        service.SetupSequence(s => s.ValidarTokenAsync(token.Token)).ReturnsAsync(false).ReturnsAsync(true);
        service.SetupSequence(s => s.ReenviarCorreoVerificacionAsync(resend.Correo))
            .ReturnsAsync(true).ThrowsAsync(new InvalidOperationException("detalle interno"));
        Assert.IsType<BadRequestObjectResult>(await controller.ValidarToken(token));
        Assert.IsType<OkObjectResult>(await controller.ValidarToken(token));
        Assert.IsType<OkObjectResult>(await controller.ReenviarVerificacion(resend));
        var failure = Assert.IsType<ObjectResult>(await controller.ReenviarVerificacion(resend));
        Assert.Equal(500, failure.StatusCode);
    }

    [Fact]
    public async Task Restablecer_contrasena_informa_token_invalido_y_oculta_fallos_internos()
    {
        var service = new Mock<IAuthService>(MockBehavior.Strict);
        var controller = new AuthController(service.Object, NullLogger<AuthController>.Instance);
        var dto = new RestablecerPasswordDto();
        controller.ModelState.AddModelError("token", "Obligatorio");
        Assert.IsType<BadRequestObjectResult>(await controller.RestablecerPassword(dto));
        Assert.Empty(service.Invocations);
        controller.ModelState.Clear();

        service.SetupSequence(s => s.RestablecerPasswordAsync(dto))
            .ReturnsAsync(true)
            .ThrowsAsync(new KeyNotFoundException("Token inválido"))
            .ThrowsAsync(new InvalidOperationException("detalle interno"));
        Assert.IsType<OkObjectResult>(await controller.RestablecerPassword(dto));
        Assert.IsType<NotFoundObjectResult>(await controller.RestablecerPassword(dto));
        var failure = Assert.IsType<BadRequestObjectResult>(await controller.RestablecerPassword(dto));
        Assert.DoesNotContain("detalle interno", JsonSerializer.Serialize(failure.Value));
    }
}
