using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class PerfilEmpreServiceTests
{
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IPerfilEmprendimientoRepository> profiles = new();
    private readonly Mock<IEmailService> mail = new();
    private readonly Cuenta account;
    private readonly PerfilEditGrant grant = new() { CuentaId = 7, FechaExpiracion = DateTime.UtcNow.AddMinutes(15) };
    private readonly PerfilEmpreService service;

    public PerfilEmpreServiceTests()
    {
        account = new Cuenta { Id = 7, Correo = "ana@example.com", Activo = true, EmailVerificado = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4),
            UsuarioEmprendedor = new UsuarioEmprendedor { Id = 3, Nombres = "Ana", Apellidos = "Perez", TipoFormalizacion = "no_formalizado",
                TipoDocumento = "CC", NumeroDocumento = "1234567", NombreEmprendimiento = "Cocina",
                Negocio = new Negocio { Id = 2, TelefonoContacto = "3001234567", CorreoContacto = "ana@example.com",
                    TipoEmprendimiento = new TipoEmprendimiento { Id = 1, Nombre = "Restaurante" } } } };
        users.Setup(x => x.ObtenerCuentaPorIdAsync(7)).ReturnsAsync(account);
        users.Setup(x => x.ObtenerOCrearTipoEmprendimientoAsync("Restaurante")).ReturnsAsync(new TipoEmprendimiento { Id = 1, Nombre = "Restaurante" });
        profiles.Setup(x => x.ObtenerPerfilCompletoAsync(7)).ReturnsAsync(account);
        profiles.Setup(x => x.ObtenerPermisoEdicionAsync(TokenGenerator.CalcularHash("edit"))).ReturnsAsync(grant);
        profiles.Setup(x => x.ExisteRolAsync(1)).ReturnsAsync(true);
        service = new(users.Object, profiles.Object, mail.Object, NullLogger<PerfilEmpreService>.Instance);
    }

    [Theory]
    [InlineData("Chef")]
    [InlineData("Cajero")]
    public async Task Empleado_lee_y_edita_solo_sus_datos_personales(string rol)
    {
        var empleado = new Empleado { Rol = new Role { Nombre = rol }, Negocio = account.UsuarioEmprendedor!.Negocio! };
        var cuentaEmpleado = new Cuenta
        {
            Id = 8, TipoCuenta = "empleado", Correo = "empleado@example.com",
            NombreCompleto = "Nombre anterior", Telefono = "3005550100", Empleado = empleado
        };
        profiles.Setup(x => x.ObtenerPerfilCompletoAsync(8)).ReturnsAsync(cuentaEmpleado);

        var perfil = Assert.IsType<PerfilUsuarioDto>(await service.ObtenerPerfilUsuarioAsync(8));
        Assert.Equal("Nombre anterior", perfil.NombreCompleto);
        Assert.Equal("3005550100", perfil.TelefonoContacto);
        Assert.Equal("empleado@example.com", perfil.Correo);
        Assert.Empty(perfil.NumeroDocumento);

        var resultado = await service.ActualizarPerfilUsuarioAsync(8, new()
        {
            NombreCompleto = " Nombre nuevo ", TelefonoContacto = " 3005550101 ",
            Correo = "ajeno@example.com", NumeroDocumento = "999"
        });

        Assert.True(resultado.Exito);
        Assert.Equal("Nombre nuevo", cuentaEmpleado.NombreCompleto);
        Assert.Equal("3005550101", cuentaEmpleado.Telefono);
        Assert.Equal("empleado@example.com", cuentaEmpleado.Correo);
        Assert.Equal(rol, empleado.Rol.Nombre);
        Assert.Equal("3001234567", empleado.Negocio.TelefonoContacto);
        profiles.Verify(x => x.GuardarCambiosUsuarioAsync(), Times.Once);
    }

    [Theory]
    [InlineData("  ", "3005550100")]
    [InlineData("Nombre válido", "123")]
    public async Task Empleado_no_guarda_un_nombre_o_telefono_invalido(string nombre, string telefono)
    {
        var cuentaEmpleado = new Cuenta { Id = 8, TipoCuenta = "empleado", NombreCompleto = "Original" };
        profiles.Setup(x => x.ObtenerPerfilCompletoAsync(8)).ReturnsAsync(cuentaEmpleado);

        var resultado = await service.ActualizarPerfilUsuarioAsync(8, new()
        {
            NombreCompleto = nombre, TelefonoContacto = telefono
        });

        Assert.False(resultado.Exito);
        Assert.Equal("Original", cuentaEmpleado.NombreCompleto);
        profiles.Verify(x => x.GuardarCambiosUsuarioAsync(), Times.Never);
    }

    private static PerfilEmprendedorDto Valid() => new() { CorreoCuenta = "ana@example.com", NombreEmprendimiento = " Cocina ", CategoriaNegocio = "Restaurante",
        TelefonoContacto = "3001234567", CorreoContacto = " CONTACTO@example.com ", CorreoEmpresa = "empresa@example.com", TipoDocumento = "cc", NumeroDocumento = "1234567" };

    [Fact]
    public async Task Confirmar_identidad_revoca_permiso_anterior_y_guarda_solo_hash()
    {
        var result = await service.ConfirmarIdentidadAsync(7, new() { Password = "Password123!" });
        Assert.NotNull(result);
        Assert.InRange(result.ExpiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
        profiles.Verify(x => x.RevocarPermisosEdicionAsync(7), Times.Once);
        profiles.Verify(x => x.RegistrarPermisoEdicionAsync(It.Is<PerfilEditGrant>(g => g.CuentaId == 7 && !g.Usado && g.TokenHash == TokenGenerator.CalcularHash(result.EditToken))), Times.Once);
        await service.RevocarPermisoAsync(7);
        profiles.Verify(x => x.RevocarPermisosEdicionAsync(7), Times.Exactly(2));
    }

    [Theory]
    [InlineData("ausente")][InlineData("inactivo")][InlineData("sin-verificar")][InlineData("password")]
    public async Task Identidad_invalida_no_emite_permiso(string failure)
    {
        if (failure == "ausente") users.Setup(x => x.ObtenerCuentaPorIdAsync(7)).ReturnsAsync((Cuenta?)null);
        if (failure == "inactivo") account.Activo = false;
        if (failure == "sin-verificar") account.EmailVerificado = false;
        Assert.Null(await service.ConfirmarIdentidadAsync(7, new() { Password = failure == "password" ? "wrong" : "Password123!" }));
        profiles.Verify(x => x.RegistrarPermisoEdicionAsync(It.IsAny<PerfilEditGrant>()), Times.Never);
    }

    [Theory]
    [InlineData("vacio")][InlineData("desconocido")][InlineData("ajeno")][InlineData("usado")][InlineData("expirado")]
    public async Task Edicion_requiere_permiso_vigente_y_propio(string failure)
    {
        if (failure == "ajeno") grant.CuentaId = 99;
        if (failure == "usado") grant.Usado = true;
        if (failure == "expirado") grant.FechaExpiracion = DateTime.UtcNow.AddSeconds(-1);
        var token = failure == "vacio" ? " " : failure == "desconocido" ? "unknown" : "edit";
        Assert.Null(await service.ObtenerPerfilAsync(7, token));
        var result = await service.ActualizarPerfilAsync(7, token, Valid());
        Assert.False(result.Exito);
        Assert.True(result.RequiereReautenticacion);
        profiles.Verify(x => x.GuardarEmpleadosAsync(It.IsAny<PerfilEditGrant>(), It.IsAny<IEnumerable<Empleado>>(), It.IsAny<IEnumerable<Empleado>>()), Times.Never);
    }

    [Fact]
    public async Task Perfil_mapea_negocio_empleados_y_roles()
    {
        account.UsuarioEmprendedor!.Negocio!.Empleados.Add(new Empleado { Id = 9, CorreoInvitado = "chef@example.com", RolId = 1, Rol = new Role { Nombre = "Chef" }, Activo = true });
        var profile = await service.ObtenerPerfilAsync(7, "edit");
        Assert.Equal("Cocina", profile!.NombreEmprendimiento);
        Assert.Equal("Restaurante", profile.CategoriaNegocio);
        Assert.Equal("Chef", Assert.Single(profile.Empleados).NombreRol);
        Assert.Equal("Ana", (await service.ObtenerPerfilUsuarioAsync(7))!.Nombres);
        profiles.Setup(x => x.ObtenerRolesAsync()).ReturnsAsync(new List<Role> { new() { Id = 1, Nombre = "Chef" } });
        Assert.Equal("Chef", Assert.Single(await service.ObtenerRolesAsync()).Nombre);
        profiles.Setup(x => x.ObtenerPerfilCompletoAsync(7)).ReturnsAsync((Cuenta?)null);
        Assert.Null(await service.ObtenerPerfilAsync(7, "edit"));
        Assert.Null(await service.ObtenerPerfilUsuarioAsync(7));
        Assert.False((await service.ActualizarPerfilAsync(7, "edit", Valid())).Exito);
        Assert.False((await service.ActualizarPerfilUsuarioAsync(7, new())).Exito);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Actualiza_perfil_y_solo_solicita_confirmacion_si_cambia_correo(bool changeEmail)
    {
        var dto = Valid();
        if (changeEmail) dto.CorreoCuenta = " NEW@example.com ";
        dto.Empleados.Add(new() { CorreoInvitado = " CHEF@example.com ", RolId = 1 });
        var result = await service.ActualizarPerfilAsync(7, "edit", dto);
        Assert.True(result.Exito);
        Assert.Equal(changeEmail, result.RequiereConfirmacionCorreo);
        Assert.Equal(!changeEmail, account.EmailVerificado);
        Assert.Equal("Cocina", account.UsuarioEmprendedor!.NombreEmprendimiento);
        Assert.Equal("contacto@example.com", account.UsuarioEmprendedor.Negocio!.CorreoContacto);
        profiles.Verify(x => x.GuardarEmpleadosAsync(grant, It.Is<IEnumerable<Empleado>>(e => e.Single().CorreoInvitado == "chef@example.com" && e.Single().EstadoInvitacion == "pendiente"), It.Is<IEnumerable<Empleado>>(e => !e.Any())), Times.Once);
        mail.Verify(x => x.EnviarCorreoVerificacionAsync("new@example.com", It.IsAny<string>()), changeEmail ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("correo")][InlineData("nit")][InlineData("tipo")][InlineData("documento-vacio")][InlineData("documento-repetido")]
    [InlineData("telefono-corto")][InlineData("telefono-largo")][InlineData("empleado-ajeno")][InlineData("empleado-repetido")]
    [InlineData("correo-repetido")][InlineData("rol")][InlineData("correo-ocupado")]
    public async Task Validaciones_impiden_persistir_perfil_invalido(string failure)
    {
        var dto = Valid();
        switch (failure)
        {
            case "correo": dto.CorreoCuenta = "other@example.com"; profiles.Setup(x => x.ExisteCorreoAsync(dto.CorreoCuenta, 7)).ReturnsAsync(true); break;
            case "nit": dto.Formalizado = true; dto.Nit = "123"; profiles.Setup(x => x.ExisteNitAsync("123", 7)).ReturnsAsync(true); break;
            case "tipo": dto.TipoDocumento = "XX"; break;
            case "documento-vacio": dto.NumeroDocumento = " "; break;
            case "documento-repetido": profiles.Setup(x => x.ExisteDocumentoAsync("1234567", 7)).ReturnsAsync(true); break;
            case "telefono-corto": dto.TelefonoContacto = "123"; break;
            case "telefono-largo": dto.TelefonoContacto = new string('1', 21); break;
            case "empleado-ajeno": dto.Empleados.Add(new() { Id = 99 }); break;
            case "empleado-repetido": dto.Empleados.AddRange(new[] { new EmpleadoDto { Id = 9 }, new EmpleadoDto { Id = 9 } }); break;
            case "correo-repetido": dto.Empleados.AddRange(new[] { new EmpleadoDto { CorreoInvitado = "a@b.co" }, new EmpleadoDto { CorreoInvitado = " A@b.co " } }); break;
            case "rol": dto.Empleados.Add(new() { CorreoInvitado = "a@b.co", RolId = 99 }); break;
            case "correo-ocupado": dto.Empleados.Add(new() { CorreoInvitado = "a@b.co", RolId = 1 }); profiles.Setup(x => x.ExisteEmpleadoCorreoAsync(2, "a@b.co", null)).ReturnsAsync(true); break;
        }
        var result = await service.ActualizarPerfilAsync(7, "edit", dto);
        Assert.False(result.Exito);
        Assert.NotEmpty(result.Mensaje);
        profiles.Verify(x => x.GuardarEmpleadosAsync(It.IsAny<PerfilEditGrant>(), It.IsAny<IEnumerable<Empleado>>(), It.IsAny<IEnumerable<Empleado>>()), Times.Never);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Actualiza_empleado_o_lo_elimina_al_deformalizar(bool deformalize)
    {
        var employee = new Empleado { Id = 9, CorreoInvitado = "old@example.com", RolId = 1 };
        account.UsuarioEmprendedor!.Negocio!.Empleados.Add(employee);
        account.UsuarioEmprendedor.TipoFormalizacion = "formalizado";
        var dto = Valid(); dto.Formalizado = !deformalize; dto.Nit = "900123"; dto.RazonSocial = " Cocina SAS "; dto.TieneSitioFisico = true; dto.Direccion = " Calle 1 ";
        dto.Empleados.Add(new() { Id = 9, CorreoInvitado = "new@example.com", RolId = 1 });
        Assert.True((await service.ActualizarPerfilAsync(7, "edit", dto)).Exito);
        Assert.Equal("Calle 1", account.UsuarioEmprendedor.Negocio.Direccion);
        if (deformalize) profiles.Verify(x => x.GuardarEmpleadosAsync(grant, It.IsAny<IEnumerable<Empleado>>(), It.Is<IEnumerable<Empleado>>(e => e.Single() == employee)), Times.Once);
        else { Assert.Equal("new@example.com", employee.CorreoInvitado); Assert.Null(account.UsuarioEmprendedor.NumeroDocumento); }
    }

    [Theory]
    [InlineData("ok")][InlineData("formalizado")][InlineData("tipo")][InlineData("documento")][InlineData("telefono")][InlineData("foto")][InlineData("persistencia")]
    public async Task Perfil_personal_normaliza_y_valida_datos(string scenario)
    {
        var dto = new PerfilUsuarioDto { Nombres = " Ana Maria ", Apellidos = " Perez ", TipoDocumento = "cc", NumeroDocumento = " 1234567 ", TelefonoContacto = "3001234567" };
        if (scenario == "formalizado") account.UsuarioEmprendedor!.TipoFormalizacion = "formalizado";
        if (scenario == "tipo") dto.TipoDocumento = "XX";
        if (scenario == "documento") dto.NumeroDocumento = " ";
        if (scenario == "telefono") dto.TelefonoContacto = "123";
        if (scenario == "foto") dto.FotoPerfilUrl = "javascript:alert(1)";
        if (scenario == "persistencia") profiles.Setup(x => x.GuardarCambiosUsuarioAsync()).ThrowsAsync(new DbUpdateException("storage unavailable"));
        var result = await service.ActualizarPerfilUsuarioAsync(7, dto);
        var valid = scenario is "ok" or "formalizado";
        Assert.Equal(valid, result.Exito);
        if (valid) { Assert.Equal("Ana Maria", account.UsuarioEmprendedor!.Nombres); Assert.Equal(scenario == "formalizado" ? null : "CC", account.UsuarioEmprendedor.TipoDocumento); }
        else Assert.NotEmpty(result.Mensaje);
    }
}
