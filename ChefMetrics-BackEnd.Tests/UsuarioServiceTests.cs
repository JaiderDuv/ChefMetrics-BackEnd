using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class UsuarioServiceTests
{
    private static UsuarioService Create(TestDatabase database, Mock<IUserRepository> users, Mock<IEmailService> mail, string environment = "Production")
    {
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(x => x.EnvironmentName).Returns(environment);
        return new(database.Context, users.Object, mail.Object, env.Object, Options.Create(new EmailSettings()));
    }
    private static Emprendedor Valid() => new() { Nombres = " Eva ", Apellidos = " Gomez ", CorreoElectronico = " EVA@example.com ", HashContrasena = "Password123!",
        TipoDocumento = " ce ", DocumentoIdentidadUe = " 7654321 ", NombreEmprendimiento = "Tienda", CategoriaNegocio = "Restaurante", TelefonoContacto = " 3009876543 ", CorreoElectronicoContacto = " CONTACTO@example.com " };

    [Theory]
    [InlineData("Development", false)][InlineData("Production", false)][InlineData("Production", true)]
    public async Task Registro_guarda_cuenta_negocio_y_exige_verificacion_en_produccion(string environment, bool formalized)
    {
        using var database = new TestDatabase(); var users = new Mock<IUserRepository>(); var mail = new Mock<IEmailService>();
        var service = Create(database, users, mail, environment); var dto = Valid();
        if (formalized)
            Assert.True(await service.RegistrarEmprendedorFormalizadoAsync(new() { Nombres = dto.Nombres, Apellidos = dto.Apellidos, CorreoElectronico = dto.CorreoElectronico, HashContrasena = dto.HashContrasena,
                Nit = "900123456", RazonSocial = "Tienda SAS", CategoriaNegocio = "Panaderia", TelefonoContacto = dto.TelefonoContacto, CorreoElectronicoContacto = dto.CorreoElectronicoContacto }));
        else Assert.True(await service.RegistrarEmprendedorAsync(dto));
        var account = await database.Context.Cuenta.Include(c => c.UsuarioEmprendedor).ThenInclude(u => u!.Negocio).SingleAsync(c => c.Correo == "eva@example.com");
        Assert.True(PasswordHasher.VerifyPassword(dto.HashContrasena, account.PasswordHash));
        Assert.Equal(environment == "Development", account.EmailVerificado);
        Assert.Equal("Eva", account.UsuarioEmprendedor!.Nombres);
        Assert.False(account.UsuarioEmprendedor.OnboardingCompletado);
        Assert.Equal(formalized ? null : "CE", account.UsuarioEmprendedor.TipoDocumento);
        Assert.Equal("contacto@example.com", account.UsuarioEmprendedor.Negocio!.CorreoContacto);
        users.Verify(x => x.RegistrarPasswordResetTokenAsync(It.Is<PasswordResetToken>(t => t.Tipo == TokenPurposes.EmailVerification && t.CuentaId == account.Id && !t.Usado)), environment == "Development" ? Times.Never() : Times.Once());
        mail.Verify(x => x.EnviarCorreoVerificacionAsync("eva@example.com", "Eva Gomez", It.IsAny<string>()), environment == "Development" ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData("correo")][InlineData("tipo")][InlineData("documento")][InlineData("telefono")][InlineData("nit")]
    public async Task Registro_invalido_no_crea_cuentas(string failure)
    {
        using var database = new TestDatabase(); var users = new Mock<IUserRepository>(); var mail = new Mock<IEmailService>(); var dto = Valid();
        if (failure == "correo") users.Setup(x => x.ExisteCorreoAsync("eva@example.com")).ReturnsAsync(true);
        if (failure == "tipo") dto.TipoDocumento = "XX";
        if (failure == "documento") dto.DocumentoIdentidadUe = " ";
        if (failure == "telefono") dto.TelefonoContacto = "123";
        var service = Create(database, users, mail);
        if (failure == "nit") await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarEmprendedorFormalizadoAsync(new() { CorreoElectronico = "eva@example.com" }));
        else await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarEmprendedorAsync(dto));
        Assert.Single(await database.Context.Cuenta.ToListAsync());
        Assert.Empty(mail.Invocations);
    }

    [Fact]
    public async Task Reenvio_y_recuperacion_emiten_tokens_con_propositos_distintos()
    {
        using var database = new TestDatabase(); var users = new Mock<IUserRepository>(); var mail = new Mock<IEmailService>();
        database.Account.EmailVerificado = false;
        users.Setup(x => x.ObtenerCuentaPorEmailAsync("ana@example.com")).ReturnsAsync(database.Account);
        var service = Create(database, users, mail);
        Assert.True(await service.ReenviarCorreoVerificacionAsync(" ANA@example.com "));
        Assert.True(await service.SolicitarRecuperacionPasswordAsync(" ANA@example.com "));
        users.Verify(x => x.RegistrarPasswordResetTokenAsync(It.Is<PasswordResetToken>(t => t.CuentaId == 7 && t.Tipo == TokenPurposes.EmailVerification)), Times.Once);
        users.Verify(x => x.RegistrarPasswordResetTokenAsync(It.Is<PasswordResetToken>(t => t.CuentaId == 7 && t.Tipo == TokenPurposes.PasswordReset)), Times.Once);
        mail.Verify(x => x.EnviarCorreoRecuperacionAsync("ana@example.com", "Ana Perez", It.IsAny<string>()), Times.Once);
        database.Account.EmailVerificado = true;
        Assert.False(await service.ReenviarCorreoVerificacionAsync("ana@example.com"));
        foreach (var email in new[] { " ", "missing@example.com" }) {
            Assert.False(await service.ReenviarCorreoVerificacionAsync(email)); Assert.False(await service.SolicitarRecuperacionPasswordAsync(email)); }
    }

    [Theory]
    [InlineData("valid", true)][InlineData("missing", false)][InlineData("used", false)][InlineData("expired", false)][InlineData("blank", false)]
    public async Task Verificacion_solo_consume_tokens_validos(string scenario, bool expected)
    {
        using var database = new TestDatabase(); var users = new Mock<IUserRepository>(); var mail = new Mock<IEmailService>();
        var token = new PasswordResetToken { Cuenta = database.Account, Usado = scenario == "used", FechaExpiracion = DateTime.UtcNow.AddMinutes(scenario == "expired" ? -1 : 15) };
        users.Setup(x => x.ObtenerTokenPorHashAsync(TokenGenerator.CalcularHash("token"), TokenPurposes.EmailVerification)).ReturnsAsync(scenario == "missing" ? null : token);
        var service = Create(database, users, mail);
        Assert.Equal(expected, await service.ValidarTokenAsync(scenario == "blank" ? " " : "token"));
        users.Verify(x => x.ConfirmarEmailPorTokenAsync(database.Account, token), expected ? Times.Once() : Times.Never());
    }
}
