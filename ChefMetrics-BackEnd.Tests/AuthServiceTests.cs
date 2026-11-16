using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace ChefMetrics_BackEnd.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> repository = new();
    private readonly Mock<IEmailService> email = new();
    private readonly MemoryCache cache = new(new MemoryCacheOptions());
    private readonly AuthService service;
    private readonly Cuenta account;

    public AuthServiceTests()
    {
        account = new Cuenta { Id = 7, Correo = "ana@example.com", Activo = true, EmailVerificado = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4),
            UsuarioEmprendedor = new UsuarioEmprendedor { Id = 3, Nombres = "Ana", Apellidos = "Perez", NombreEmprendimiento = "Cocina", OnboardingCompletado = true, Negocio = new Negocio { Id = 2 } } };
        repository.Setup(r => r.ObtenerCuentaPorEmailAsync(account.Correo)).ReturnsAsync(account);
        service = new AuthService(repository.Object, email.Object, Options.Create(new JwtSettings
        { Key = new string('k', 64), Issuer = "tests", Audience = "tests", AccessTokenMinutes = 15, RefreshTokenDays = 7 }), cache);
    }

    [Fact]
    public async Task Login_normaliza_correo_emite_claims_y_guarda_solo_hash_de_refresh()
    {
        RefreshToken? saved = null;
        repository.Setup(r => r.RegistrarRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(value => saved = value).ReturnsAsync((RefreshToken value) => value);
        var result = await service.IniciarSesionAsync(new LoginDto { CorreoElectronico = " ANA@EXAMPLE.COM ", Password = "Password123!" });
        Assert.True(result.Exito);
        Assert.Equal("emprendedor", result.TipoCuenta);
        Assert.Equal(2, result.NegocioId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("tests", jwt.Issuer);
        Assert.Contains(jwt.Claims, claim => claim.Value == "Emprendedor");
        Assert.Equal(TokenGenerator.CalcularHash(result.RefreshToken!), saved!.TokenHash);
        Assert.NotEqual(result.RefreshToken, saved.TokenHash);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("ana@example.com", "")]
    [InlineData("missing@example.com", "Password123!")]
    [InlineData("ana@example.com", "incorrecta")]
    public async Task Credenciales_invalidas_no_generan_sesion(string address, string password)
    {
        Assert.False((await service.IniciarSesionAsync(new LoginDto { CorreoElectronico = address, Password = password })).Exito);
        repository.Verify(r => r.RegistrarRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Theory]
    [InlineData("inactiva")]
    [InlineData("sin_verificar")]
    [InlineData("empleado_inactivo")]
    [InlineData("invitacion_pendiente")]
    public async Task Restringe_cuentas_no_habilitadas(string state)
    {
        account.Activo = state != "inactiva";
        account.EmailVerificado = state != "sin_verificar";
        if (state.StartsWith("empleado") || state == "invitacion_pendiente")
            account.Empleado = new Empleado { Activo = state != "empleado_inactivo", EstadoInvitacion = "pendiente" };
        Assert.False((await service.IniciarSesionAsync(new LoginDto { CorreoElectronico = account.Correo, Password = "Password123!" })).Exito);
        repository.Verify(r => r.RegistrarRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Tres_fallos_bloquean_el_siguiente_intento()
    {
        var dto = new LoginDto { CorreoElectronico = account.Correo, Password = "incorrecta" };
        for (var attempt = 0; attempt < 3; attempt++) Assert.False((await service.IniciarSesionAsync(dto)).Exito);
        dto.Password = "Password123!";
        Assert.Contains("máximo", (await service.IniciarSesionAsync(dto)).Mensaje);
        repository.Verify(r => r.ObtenerCuentaPorEmailAsync(account.Correo), Times.Exactly(3));
    }

    [Fact]
    public async Task Login_de_empleado_incluye_rol_negocio_y_version()
    {
        account.UsuarioEmprendedor = null;
        account.Empleado = new Empleado { Id = 9, Activo = true, EstadoInvitacion = "aceptada", NegocioId = 2, SessionVersion = 4, Rol = new Role { Nombre = "Chef" }, Negocio = new Negocio { Usuario = new UsuarioEmprendedor { NombreEmprendimiento = "Cocina" } } };
        var result = await service.IniciarSesionAsync(new LoginDto { CorreoElectronico = account.Correo, Password = "Password123!" });
        Assert.True(result.Exito);
        Assert.Equal("Chef", result.Rol);
        Assert.Equal("empleado", result.TipoCuenta);
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken).Claims, c => c.Type == "versionEmpleado" && c.Value == "4");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_solo_emite_sesion_si_la_rotacion_es_atomica(bool rotated)
    {
        var previous = new RefreshToken { Cuenta = account, CuentaId = account.Id, FechaExpiracion = DateTime.UtcNow.AddDays(1) };
        repository.Setup(r => r.ObtenerRefreshTokenPorHashAsync(It.IsAny<string>())).ReturnsAsync(previous);
        repository.Setup(r => r.RotarRefreshTokenAsync(previous, It.IsAny<RefreshToken>())).ReturnsAsync(rotated);
        var result = await service.RenovarSesionAsync(new RefreshTokenDto { Token = "refresh" });
        Assert.Equal(rotated, result is not null);
        repository.Verify(r => r.RotarRefreshTokenAsync(previous, It.Is<RefreshToken>(t => t.CuentaId == account.Id && !t.Revocado)), Times.Once);
    }

    [Theory]
    [InlineData("vacio")]
    [InlineData("ausente")]
    [InlineData("expirado")]
    [InlineData("revocado")]
    [InlineData("inactiva")]
    public async Task Refresh_invalido_no_se_rota(string state)
    {
        var token = new RefreshToken { Cuenta = account, Revocado = state == "revocado", FechaExpiracion = DateTime.UtcNow.AddDays(state == "expirado" ? -1 : 1) };
        account.Activo = state != "inactiva";
        repository.Setup(r => r.ObtenerRefreshTokenPorHashAsync(It.IsAny<string>())).ReturnsAsync(state == "ausente" ? null : token);
        Assert.Null(await service.RenovarSesionAsync(new RefreshTokenDto { Token = state == "vacio" ? "" : "refresh" }));
        repository.Verify(r => r.RotarRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_revoca_el_hash_del_token()
    {
        Assert.False(await service.CerrarSesionAsync(new RefreshTokenDto()));
        repository.Setup(r => r.RevocarRefreshTokenAsync(TokenGenerator.CalcularHash("refresh"))).ReturnsAsync(true);
        Assert.True(await service.CerrarSesionAsync(new RefreshTokenDto { Token = "refresh" }));
    }

    [Fact]
    public async Task Recuperacion_guarda_token_con_proposito_y_envia_al_destinatario()
    {
        Assert.False(await service.SolicitarRecuperacionPasswordAsync(new RecuperarPasswordDto { CorreoElectronico = "missing@example.com" }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SolicitarRecuperacionPasswordAsync(new RecuperarPasswordDto()));
        Assert.True(await service.SolicitarRecuperacionPasswordAsync(new RecuperarPasswordDto { CorreoElectronico = account.Correo }));
        repository.Verify(r => r.RegistrarPasswordResetTokenAsync(It.Is<PasswordResetToken>(t => t.Tipo == TokenPurposes.PasswordReset && t.CuentaId == 7 && !t.Usado)), Times.Once);
        email.Verify(e => e.EnviarCorreoRecuperacionAsync(account.Correo, "Ana Perez", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Restablecimiento_valida_propietario_y_revoca_sesiones()
    {
        var dto = new RestablecerPasswordDto { Token = "reset", CorreoElectronico = account.Correo, NuevaPassword = "NewPassword123!", ConfirmarPassword = "otra" };
        await Assert.ThrowsAsync<ArgumentException>(() => service.RestablecerPasswordAsync(dto));
        dto.ConfirmarPassword = dto.NuevaPassword;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RestablecerPasswordAsync(dto));
        var token = new PasswordResetToken { Cuenta = account, FechaExpiracion = DateTime.UtcNow.AddMinutes(5) };
        repository.Setup(r => r.ObtenerTokenPorHashAsync(It.IsAny<string>(), TokenPurposes.PasswordReset)).ReturnsAsync(token);
        dto.CorreoElectronico = "other@example.com";
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RestablecerPasswordAsync(dto));
        dto.CorreoElectronico = account.Correo;
        Assert.True(await service.RestablecerPasswordAsync(dto));
        repository.Verify(r => r.RestablecerPasswordYRevocarSesionesAsync(account, token, It.Is<string>(hash => hash != dto.NuevaPassword)), Times.Once);
    }

    public void Dispose() => cache.Dispose();
}
