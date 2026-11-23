using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class EmpleadosControllerTests
{
    private const string InitialPassword = "Password123!";
    private const string EmployeeEmail = "chef@example.com";
    private readonly Mock<IEmailService> _mail = new();

    private EmpleadosController Controller(TestDatabase database, string? owner = "7") =>
        CrudControllerTests.WithUser(new EmpleadosController(
            database.Context, _mail.Object, NullLogger<EmpleadosController>.Instance), owner);

    private static async Task<Role> AddRole(TestDatabase database, string name = "Chef")
    {
        var role = new Role { Nombre = name };
        database.Context.Roles.Add(role);
        await database.Context.SaveChangesAsync();
        return role;
    }

    private static async Task<Empleado> AddEmployee(TestDatabase database, bool withAccount = true)
    {
        var role = await AddRole(database);
        var employee = new Empleado
        {
            NegocioId = database.Business.Id, RolId = role.Id, CorreoInvitado = EmployeeEmail,
            EstadoInvitacion = "pendiente", Activo = true,
            InvitadoPor = database.Business.UsuarioId,
            Cuenta = withAccount ? new Cuenta
            {
                Correo = EmployeeEmail, PasswordHash = database.Account.PasswordHash, TipoCuenta = "empleado"
            } : null
        };
        database.Context.Empleados.Add(employee);
        await database.Context.SaveChangesAsync();
        return employee;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invitacion_guarda_el_hash_o_revierte_si_falla_el_correo(bool mailFails)
    {
        using var database = new TestDatabase(relational: true);
        var role = await AddRole(database);
        string? token = null;
        _mail.Setup(x => x.EnviarInvitacionEmpleadoAsync(EmployeeEmail, "Cocina", It.IsAny<string>()))
            .Callback<string, string, string>((_, _, value) => token = value)
            .Returns(() => mailFails ? Task.FromException(new HttpRequestException()) : Task.CompletedTask);

        var result = await Controller(database).Invitar(new()
        {
            Correo = " CHEF@EXAMPLE.COM ", RolId = role.Id, Password = InitialPassword
        });
        database.Context.ChangeTracker.Clear();

        if (mailFails)
        {
            Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
            Assert.Empty(await database.Context.Empleados.ToListAsync());
            Assert.False(await database.Context.Cuenta.AnyAsync(c => c.Correo == EmployeeEmail));
            return;
        }

        Assert.IsType<OkObjectResult>(result);
        var employee = await database.Context.Empleados.Include(e => e.Cuenta).SingleAsync();
        Assert.Null(employee.CredencialCifrada);
        Assert.NotNull(token);
        Assert.Equal(TokenGenerator.CalcularHash(token), employee.TokenInvitacion);
        Assert.False(employee.Cuenta!.EmailVerificado);
        Assert.True(BCrypt.Net.BCrypt.Verify(InitialPassword, employee.Cuenta.PasswordHash));
    }

    [Theory]
    [InlineData("vigente")]
    [InlineData("sin credencial")]
    [InlineData("expirada")]
    public async Task Aceptacion_exige_token_vigente_y_lo_invalida_tras_usarlo(string scenario)
    {
        using var database = new TestDatabase();
        var employee = await AddEmployee(database);
        const string token = "invitacion-prueba";
        employee.TokenInvitacion = TokenGenerator.CalcularHash(token);
        employee.TokenInvitacionExpira = scenario == "expirada" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1);
        employee.CredencialCifrada = scenario == "sin credencial" ? null : "cifrada";
        await database.Context.SaveChangesAsync();

        var controller = Controller(database, null);
        var result = await controller.Aceptar(new() { Token = token });
        if (scenario == "expirada")
        {
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("pendiente", employee.EstadoInvitacion);
            Assert.False(employee.Cuenta!.EmailVerificado);
            return;
        }

        var response = Assert.IsType<AceptarInvitacionRespuestaDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(EmployeeEmail, response.Correo);
        Assert.DoesNotContain("Password", System.Text.Json.JsonSerializer.Serialize(response));
        Assert.True(employee.Cuenta!.EmailVerificado);
        Assert.Null(employee.TokenInvitacion);
        Assert.Null(employee.CredencialCifrada);
        Assert.IsType<BadRequestObjectResult>(await controller.Aceptar(new() { Token = token }));
    }

    [Theory]
    [InlineData("rol")]
    [InlineData("correo")]
    [InlineData("contraseña")]
    [InlineData("sin cambios")]
    [InlineData("crear cuenta")]
    public async Task Editar_empleado_aplica_solo_los_cambios_pedidos(string scenario)
    {
        using var database = new TestDatabase(relational: true);
        var employee = await AddEmployee(database, withAccount: scenario != "crear cuenta");
        var nextRole = await AddRole(database, "Cajero");
        const string newPassword = "Nueva123!";
        string? token = null;
        _mail.Setup(x => x.EnviarInvitacionEmpleadoAsync(It.IsAny<string>(), "Cocina", It.IsAny<string>()))
            .Callback<string, string, string>((_, _, value) => token = value)
            .Returns(Task.CompletedTask);
        if (scenario == "contraseña")
        {
            employee.EstadoInvitacion = "aceptada";
            employee.CredencialCifrada = "anterior";
            await database.Context.SaveChangesAsync();
        }

        var result = await Controller(database).Actualizar(employee.Id, new()
        {
            Correo = scenario == "correo" ? "nuevo@example.com" : EmployeeEmail,
            RolId = scenario == "rol" ? nextRole.Id : employee.RolId,
            NuevaPassword = scenario is "crear cuenta" or "contraseña" ? newPassword : null
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scenario == "sin cambios" ? 0 : 1, employee.SessionVersion);
        if (scenario == "rol") Assert.Equal(nextRole.Id, employee.RolId);
        if (scenario is "correo" or "crear cuenta")
        {
            Assert.NotNull(token);
            Assert.Equal(TokenGenerator.CalcularHash(token), employee.TokenInvitacion);
            Assert.False(employee.Cuenta!.Activo);
        }
        else Assert.Null(token);
        if (scenario == "correo") Assert.Equal("nuevo@example.com", employee.Cuenta!.Correo);
        if (scenario is "crear cuenta" or "contraseña")
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, employee.Cuenta!.PasswordHash));
        if (scenario == "contraseña") Assert.Null(employee.CredencialCifrada);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Desactivar_invalida_sesion_y_eliminar_borra_la_cuenta_propia(bool withAccount)
    {
        using var database = new TestDatabase(relational: true);
        var employee = await AddEmployee(database, withAccount);
        var accountId = employee.CuentaId;
        if (accountId.HasValue)
        {
            database.Context.RefreshTokens.Add(new RefreshToken
            {
                CuentaId = accountId.Value, TokenHash = "sesion", FechaExpiracion = DateTime.UtcNow.AddDays(1)
            });
            await database.Context.SaveChangesAsync();
        }

        var controller = Controller(database);
        Assert.IsType<OkObjectResult>(await controller.ActualizarEstado(employee.Id, new() { Activo = false }));
        Assert.False(employee.Activo);
        Assert.Equal(1, employee.SessionVersion);
        database.Context.ChangeTracker.Clear();
        if (accountId.HasValue)
        {
            Assert.True((await database.Context.RefreshTokens.AsNoTracking().SingleAsync()).Revocado);
            Assert.False((await database.Context.Cuenta.FindAsync(accountId.Value))!.Activo);
        }

        // Simula la vida útil de un contexto distinto para la siguiente petición.
        database.Context.ChangeTracker.Clear();
        Assert.IsType<NoContentResult>(await controller.Eliminar(employee.Id));
        Assert.Empty(await database.Context.Empleados.ToListAsync());
        Assert.Empty(await database.Context.RefreshTokens.ToListAsync());
        Assert.Equal(database.Account.Id, (await database.Context.Cuenta.SingleAsync()).Id);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("999", 404)]
    [InlineData("7", 404)]
    public async Task Operaciones_de_empleado_rechazan_cuentas_o_ids_ajenos(string? account, int status)
    {
        using var database = new TestDatabase();
        var controller = Controller(database, account);
        var results = new[]
        {
            await controller.Actualizar(999, new()),
            await controller.ActualizarEstado(999, new()),
            await controller.Eliminar(999)
        };
        Assert.All(results, result => Assert.Equal(status, Assert.IsAssignableFrom<Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult>(result).StatusCode));
        _mail.VerifyNoOtherCalls();
    }
}
