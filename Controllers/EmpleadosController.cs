using System.Security.Claims;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/empleados")]
public sealed class EmpleadosController : ControllerBase
{
    private readonly ChefMetricsContext _db;
    private readonly IEmailService _email;
    private readonly ICredencialProtector _protector;
    private readonly ILogger<EmpleadosController> _logger;

    public EmpleadosController(
        ChefMetricsContext db,
        IEmailService email,
        ICredencialProtector protector,
        ILogger<EmpleadosController> logger)
    {
        _db = db;
        _email = email;
        _protector = protector;
        _logger = logger;
    }

    [HttpPost("invitaciones")]
    [Authorize(Policy = "SoloEmprendedor")]
    public async Task<IActionResult> Invitar([FromBody] InvitarEmpleadoDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!TryGetCuentaId(out var cuentaId)) return Unauthorized();

        var usuario = await _db.UsuarioEmprendedors
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);
        if (usuario?.Negocio is null)
            return NotFound(new { mensaje = "No se encontró el negocio de esta cuenta." });

        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await _db.Cuenta.AnyAsync(c => c.Correo.ToLower() == correo))
            return Conflict(new { mensaje = "Ya existe una cuenta con ese correo." });
        if (await _db.Empleados.AnyAsync(e => e.NegocioId == usuario.Negocio.Id && e.CorreoInvitado.ToLower() == correo))
            return Conflict(new { mensaje = "Ese correo ya tiene una invitación en este negocio." });

        var rol = await _db.Roles.FindAsync(dto.RolId);
        if (rol is null ||
            (!string.Equals(rol.Nombre, "Chef", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(rol.Nombre, "Cajero", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { mensaje = "Selecciona el rol Chef o Cajero." });

        var token = TokenGenerator.GenerarTokenVerificacion();
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var cuenta = new Cuenta
        {
            Correo = correo,
            PasswordHash = PasswordHasher.HashPassword(dto.Password),
            TipoCuenta = "empleado",
            EmailVerificado = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _db.Cuenta.Add(cuenta);
        await _db.SaveChangesAsync();

        var empleado = new Empleado
        {
            CuentaId = cuenta.Id,
            NegocioId = usuario.Negocio.Id,
            RolId = rol.Id,
            CorreoInvitado = correo,
            EstadoInvitacion = "pendiente",
            TokenInvitacion = TokenGenerator.CalcularHash(token),
            TokenInvitacionExpira = DateTime.UtcNow.AddDays(7),
            CredencialCifrada = _protector.Proteger(dto.Password),
            Activo = true,
            Rol = rol,
            InvitadoPor = usuario.Id,
            FechaInvitacion = DateTime.UtcNow
        };
        _db.Empleados.Add(empleado);
        await _db.SaveChangesAsync();

        try
        {
            await _email.EnviarInvitacionEmpleadoAsync(correo, usuario.NombreEmprendimiento ?? "tu negocio", token);
            await transaction.CommitAsync();
            return Ok(CrearRespuestaEmpleado(empleado, "Invitación enviada correctamente."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo enviar una invitación de empleado.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { mensaje = "No se pudo enviar la invitación. Revisa la configuración de correo e intenta de nuevo." });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "SoloEmprendedor")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarEmpleadoDto dto)
    {
        if (!TryGetCuentaId(out var cuentaId)) return Unauthorized();

        var usuario = await _db.UsuarioEmprendedors
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);
        if (usuario?.Negocio is null)
            return NotFound(new { mensaje = "No se encontró el negocio de esta cuenta." });

        var empleado = await _db.Empleados
            .Include(e => e.Cuenta)
            .Include(e => e.Rol)
            .FirstOrDefaultAsync(e => e.Id == id && e.NegocioId == usuario.Negocio.Id);
        if (empleado is null)
            return NotFound(new { mensaje = "No se encontró el empleado." });

        var correo = dto.Correo.Trim().ToLowerInvariant();
        var rol = await _db.Roles.FindAsync(dto.RolId);
        if (rol is null ||
            (!string.Equals(rol.Nombre, "Chef", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(rol.Nombre, "Cajero", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { mensaje = "Selecciona el rol Chef o Cajero." });

        var correoCambiado = !string.Equals(empleado.CorreoInvitado, correo, StringComparison.OrdinalIgnoreCase);
        var rolCambiado = empleado.RolId != rol.Id;
        var passwordNueva = !string.IsNullOrWhiteSpace(dto.NuevaPassword);
        var cuenta = empleado.Cuenta;
        var requiereCuentaNueva = cuenta is null;

        if (correoCambiado || requiereCuentaNueva)
        {
            if (await _db.Cuenta.AnyAsync(c => c.Correo.ToLower() == correo && c.Id != empleado.CuentaId))
                return Conflict(new { mensaje = "Ya existe una cuenta con ese correo." });

            if (await _db.Empleados.AnyAsync(e => e.Id != empleado.Id && e.NegocioId == empleado.NegocioId &&
                e.CorreoInvitado.ToLower() == correo))
                return Conflict(new { mensaje = "Ese correo ya pertenece a otro empleado de este negocio." });
        }

        if (requiereCuentaNueva && !passwordNueva)
            return BadRequest(new { mensaje = "Este empleado aún no tiene cuenta. Define una contraseña para enviarle una invitación nueva." });

        var cambios = correoCambiado || rolCambiado || passwordNueva || requiereCuentaNueva;
        if (!cambios)
        {
            return Ok(CrearRespuestaEmpleado(empleado, "No hay cambios para guardar."));
        }

        var reenviarInvitacion = correoCambiado || requiereCuentaNueva;
        var token = reenviarInvitacion ? TokenGenerator.GenerarTokenVerificacion() : null;
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (requiereCuentaNueva)
            {
                cuenta = new Cuenta
                {
                    Correo = correo,
                    PasswordHash = PasswordHasher.HashPassword(dto.NuevaPassword!),
                    TipoCuenta = "empleado",
                    EmailVerificado = false,
                    Activo = false,
                    FechaCreacion = DateTime.UtcNow
                };
                empleado.Cuenta = cuenta;
                empleado.CredencialCifrada = _protector.Proteger(dto.NuevaPassword!);
            }
            else if (correoCambiado)
            {
                cuenta!.Correo = correo;
                cuenta.EmailVerificado = false;
                cuenta.Activo = false;
            }

            if (passwordNueva && !requiereCuentaNueva)
            {
                cuenta!.PasswordHash = PasswordHasher.HashPassword(dto.NuevaPassword!);
                if (reenviarInvitacion ||
                    string.Equals(empleado.EstadoInvitacion, "pendiente", StringComparison.OrdinalIgnoreCase))
                    empleado.CredencialCifrada = _protector.Proteger(dto.NuevaPassword!);
                else
                    empleado.CredencialCifrada = null;
            }

            empleado.CorreoInvitado = correo;
            empleado.RolId = rol.Id;
            empleado.Rol = rol;
            empleado.SessionVersion++;

            if (reenviarInvitacion)
            {
                empleado.EstadoInvitacion = "pendiente";
                empleado.TokenInvitacion = TokenGenerator.CalcularHash(token!);
                empleado.TokenInvitacionExpira = DateTime.UtcNow.AddDays(7);
                empleado.FechaInvitacion = DateTime.UtcNow;
                empleado.FechaAceptacion = null;
                cuenta!.EmailVerificado = false;
                cuenta.Activo = false;
            }

            await _db.SaveChangesAsync();
            if (cuenta is not null)
            {
                await _db.RefreshTokens
                    .Where(t => t.CuentaId == cuenta.Id && !t.Revocado)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Revocado, true));
            }

            if (reenviarInvitacion)
            {
                await _email.EnviarInvitacionEmpleadoAsync(
                    correo,
                    usuario.NombreEmprendimiento ?? "tu negocio",
                    token!);
            }

            await transaction.CommitAsync();
            return Ok(CrearRespuestaEmpleado(
                empleado,
                reenviarInvitacion
                    ? "Datos actualizados. Se envió una nueva invitación y la cuenta quedará habilitada cuando el empleado la acepte."
                    : passwordNueva
                        ? "Datos actualizados y sesión revocada. Comunica de forma segura la nueva contraseña al empleado."
                        : "Datos actualizados. El empleado deberá iniciar sesión nuevamente."));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudieron actualizar los datos del empleado {EmpleadoId}.", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { mensaje = reenviarInvitacion
                    ? "No se pudo enviar la invitación. No se guardaron los cambios; revisa la configuración del correo e intenta de nuevo."
                    : "No se pudieron guardar los cambios del empleado." });
        }
    }

    [HttpPost("aceptar-invitacion")]
    [AllowAnonymous]
    public async Task<IActionResult> Aceptar([FromBody] AceptarInvitacionDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var hash = TokenGenerator.CalcularHash(dto.Token);
        var empleado = await _db.Empleados
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.TokenInvitacion == hash);
        if (empleado is null || empleado.Cuenta is null ||
            !string.Equals(empleado.EstadoInvitacion, "pendiente", StringComparison.OrdinalIgnoreCase) ||
            empleado.TokenInvitacionExpira <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "La invitación no es válida o ya expiró." });

        string password = string.Empty;
        if (!string.IsNullOrWhiteSpace(empleado.CredencialCifrada))
        {
            try
            {
                password = _protector.Desproteger(empleado.CredencialCifrada);
            }
            catch
            {
                return BadRequest(new { mensaje = "No se pudieron recuperar los datos de acceso. Solicita una nueva invitación." });
            }
        }

        empleado.EstadoInvitacion = "aceptada";
        empleado.FechaAceptacion = DateTime.UtcNow;
        empleado.TokenInvitacion = null;
        empleado.TokenInvitacionExpira = null;
        empleado.CredencialCifrada = null;
        empleado.Cuenta.EmailVerificado = true;
        empleado.Cuenta.Activo = empleado.Activo;
        await _db.SaveChangesAsync();

        return Ok(new AceptarInvitacionRespuestaDto
        {
            Correo = empleado.CorreoInvitado,
            Password = password,
            Mensaje = string.IsNullOrWhiteSpace(password)
                ? "Invitación aceptada. El correo fue actualizado; inicia sesión con tu contraseña actual."
                : "Invitación aceptada. Ya puedes iniciar sesión con estos datos."
        });
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "SoloEmprendedor")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoEmpleadoDto dto)
    {
        if (!TryGetCuentaId(out var cuentaId)) return Unauthorized();
        var negocioId = await _db.Negocios
            .Where(n => n.Usuario.CuentaId == cuentaId)
            .Select(n => (int?)n.Id)
            .FirstOrDefaultAsync();
        if (!negocioId.HasValue) return NotFound(new { mensaje = "No se encontró el negocio." });

        var empleado = await _db.Empleados.Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Id == id && e.NegocioId == negocioId.Value);
        if (empleado is null) return NotFound(new { mensaje = "No se encontró el empleado." });

        empleado.Activo = dto.Activo;
        empleado.SessionVersion++;
        if (empleado.Cuenta is not null)
        {
            empleado.Cuenta.Activo = dto.Activo &&
                string.Equals(empleado.EstadoInvitacion, "aceptada", StringComparison.OrdinalIgnoreCase);
            if (!dto.Activo)
                await _db.RefreshTokens.Where(t => t.CuentaId == empleado.CuentaId && !t.Revocado)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Revocado, true));
        }
        await _db.SaveChangesAsync();
        return Ok(new { activo = empleado.Activo });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "SoloEmprendedor")]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (!TryGetCuentaId(out var cuentaId)) return Unauthorized();
        var negocioId = await _db.Negocios
            .Where(n => n.Usuario.CuentaId == cuentaId)
            .Select(n => (int?)n.Id)
            .FirstOrDefaultAsync();
        if (!negocioId.HasValue) return NotFound(new { mensaje = "No se encontró el negocio." });

        var empleado = await _db.Empleados
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Id == id && e.NegocioId == negocioId.Value);
        if (empleado is null) return NotFound(new { mensaje = "No se encontró el empleado." });

        var cuenta = empleado.Cuenta;
        if (cuenta is not null &&
            (!string.Equals(cuenta.TipoCuenta, "empleado", StringComparison.OrdinalIgnoreCase) ||
             await _db.UsuarioEmprendedors.AnyAsync(u => u.CuentaId == cuenta.Id)))
        {
            return Conflict(new { mensaje = "La cuenta vinculada no es una cuenta de empleado eliminable." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (cuenta is not null)
            {
                await _db.RefreshTokens
                    .Where(t => t.CuentaId == cuenta.Id)
                    .ExecuteDeleteAsync();
                await _db.PasswordResetTokens
                    .Where(t => t.CuentaId == cuenta.Id)
                    .ExecuteDeleteAsync();
                await _db.PerfilEditGrants
                    .Where(g => g.CuentaId == cuenta.Id)
                    .ExecuteDeleteAsync();
            }

            _db.Empleados.Remove(empleado);
            await _db.SaveChangesAsync();

            if (cuenta is not null)
            {
                _db.Cuenta.Remove(cuenta);
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo eliminar el empleado {EmpleadoId} del negocio {NegocioId}.", id, negocioId.Value);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "No se pudo eliminar el empleado y sus datos asociados." });
        }
    }

    private static object CrearRespuestaEmpleado(Empleado empleado, string mensaje) => new
    {
        id = empleado.Id,
        correoInvitado = empleado.CorreoInvitado,
        rolId = empleado.RolId,
        nombreRol = empleado.Rol?.Nombre,
        estadoInvitacion = empleado.EstadoInvitacion,
        activo = empleado.Activo,
        mensaje
    };

    private bool TryGetCuentaId(out int cuentaId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out cuentaId);
    }
}
