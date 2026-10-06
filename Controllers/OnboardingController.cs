using System.Security.Claims;
using ChefMetrics_BackEnd.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly ChefMetricsContext _context;

    public OnboardingController(ChefMetricsContext context)
    {
        _context = context;
    }

    public class OnboardingFotoPerfilDto
{
    public string? FotoPerfilUrl { get; set; }
}

// Devuelve los datos del negocio ya registrados, para autocompletar
    // la pestaña "Información del Negocio" del onboarding sin re-tipearlos.
    [HttpGet("datos-personales")]
    public async Task<IActionResult> DatosPersonales()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var cuenta = await _context.Cuenta
            .AsNoTracking()
            .Include(c => c.UsuarioEmprendedor)
            .ThenInclude(u => u!.Negocio)
            .FirstOrDefaultAsync(c => c.Id == cuentaId);

        if (cuenta is null || cuenta.UsuarioEmprendedor is null)
        {
            return NotFound(new { mensaje = "No se encontró el perfil." });
        }

        var usuario = cuenta.UsuarioEmprendedor;
        return Ok(new
        {
            usuario.Nombres,
            usuario.Apellidos,
            usuario.TipoDocumento,
            usuario.NumeroDocumento,
            CorreoElectronico = cuenta.Correo,
            usuario.Negocio?.TelefonoContacto,
            // La foto no se persiste (la BD no tiene la columna):
            // solo se usa como vista previa en el cliente.
            FotoPerfilUrl = (string?)null
        });
    }

    // Recibe la foto pero no la persiste (sin columna en BD): la vista previa
    // vive en el cliente. Se responde OK para no romper el flujo de onboarding.
    [HttpPost("foto-perfil")]
    public async Task<IActionResult> GuardarFotoPerfil([FromBody] OnboardingFotoPerfilDto dto)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var existe = await _context.UsuarioEmprendedors
            .AsNoTracking()
            .AnyAsync(u => u.CuentaId == cuentaId);

        if (!existe)
        {
            return NotFound(new { mensaje = "No se encontró el perfil." });
        }

        return Ok(new { exito = true, mensaje = "Vista previa actualizada (la foto no se guarda en el servidor)." });
    }

    [HttpGet("datos-negocio")]
    public async Task<IActionResult> DatosNegocio()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var usuario = await _context.UsuarioEmprendedors
            .AsNoTracking()
            .Include(u => u.Negocio)
            .ThenInclude(n => n!.TipoEmprendimiento)
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);

        if (usuario is null) return NotFound();

        var negocio = usuario.Negocio;
        return Ok(new
        {
            usuario.TipoFormalizacion,
            usuario.Nit,
            usuario.RazonSocial,
            usuario.NombreEmprendimiento,
            CategoriaNegocio = negocio?.TipoEmprendimiento?.Nombre,
            negocio?.TelefonoContacto,
            negocio?.CorreoContacto,
            negocio?.Rues,
            negocio?.CorreoEmpresa,
            negocio?.TieneSitioFisico,
            negocio?.Direccion
        });
    }

    // Marca el onboarding como completado para la cuenta autenticada.
    [HttpPost("completar")]
    public async Task<IActionResult> Completar()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var usuario = await _context.UsuarioEmprendedors
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);

        if (usuario is null)
        {
            return NotFound(new { mensaje = "No se encontró el perfil de emprendimiento." });
        }

        usuario.OnboardingCompletado = true;
        await _context.SaveChangesAsync();

        return Ok(new { exito = true, mensaje = "Onboarding completado." });
    }
}
