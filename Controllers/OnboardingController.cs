using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Utilities;
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

    public sealed class OnboardingPersonalDto
    {
        [Required, StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [StringLength(10)]
        public string? TipoDocumento { get; set; }

        [StringLength(20)]
        public string? NumeroDocumento { get; set; }

        [Required, StringLength(20, MinimumLength = 7)]
        public string Telefono { get; set; } = string.Empty;
    }

    public sealed class OnboardingNegocioDto
    {
        [Required, StringLength(150)]
        public string NombreEmpresa { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Nit { get; set; }

        [StringLength(150)]
        public string? RazonSocial { get; set; }

        [StringLength(50)]
        public string? Rues { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string CorreoEmpresa { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 7)]
        public string TelefonoEmpresa { get; set; } = string.Empty;

        public bool Formalizado { get; set; }
        public bool TieneSitioFisico { get; set; }
        public string? Direccion { get; set; }
        public string? LogoBase64 { get; set; }
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
            FotoPerfilUrl = usuario.FotoPerfilUrl
        });
    }

    // Guarda la foto de perfil del emprendedor. Se acepta una data URL de imagen
    // o un valor vacío para quitarla.
    [HttpPost("foto-perfil")]
    public async Task<IActionResult> GuardarFotoPerfil([FromBody] OnboardingFotoPerfilDto dto)
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
            return NotFound(new { mensaje = "No se encontró el perfil." });
        }

        if (!FotoPerfilHelper.TryNormalizar(dto.FotoPerfilUrl, out var foto, out var error))
        {
            return BadRequest(new { mensaje = error });
        }

        usuario.FotoPerfilUrl = foto;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            exito = true,
            fotoPerfilUrl = foto,
            mensaje = foto is null ? "Foto de perfil eliminada." : "Foto de perfil guardada."
        });
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
            negocio?.Direccion,
            negocio?.LogoUrl
        });
    }

    // Guarda los datos personales que se confirman en la primera pestaña.
    [HttpPost("datos-personales")]
    public async Task<IActionResult> GuardarDatosPersonales([FromBody] OnboardingPersonalDto dto)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var usuario = await _context.UsuarioEmprendedors
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);

        if (usuario is null)
        {
            return NotFound(new { mensaje = "No se encontró el perfil." });
        }

        var formalizado = string.Equals(usuario.TipoFormalizacion, "formalizado",
            StringComparison.OrdinalIgnoreCase);

        if (formalizado)
        {
            // Los negocios formalizados se identifican por NIT, no por documento personal.
            usuario.TipoDocumento = null;
            usuario.NumeroDocumento = null;
        }
        else
        {
            var tipo = dto.TipoDocumento?.Trim().ToUpperInvariant();
            var numero = dto.NumeroDocumento?.Trim();
            if (tipo is not ("CC" or "CE" or "TI" or "PA"))
            {
                return BadRequest(new { mensaje = "El tipo de documento no es válido. Valores permitidos: CC, CE, TI, PA." });
            }
            if (string.IsNullOrWhiteSpace(numero))
            {
                return BadRequest(new { mensaje = "El número de documento es obligatorio." });
            }

            usuario.TipoDocumento = tipo;
            usuario.NumeroDocumento = numero;
        }

        usuario.Nombres = dto.Nombres.Trim();
        usuario.Apellidos = dto.Apellidos.Trim();

        if (usuario.Negocio is not null)
        {
            usuario.Negocio.TelefonoContacto = dto.Telefono.Trim();
            usuario.Negocio.FechaActualizacion = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { mensaje = "El documento de identidad ya está registrado por otro usuario." });
        }

        return Ok(new { exito = true, mensaje = "Datos personales guardados." });
    }

    // Guarda los datos del negocio que se confirman en la segunda pestaña.
    [HttpPost("datos-negocio")]
    public async Task<IActionResult> GuardarDatosNegocio([FromBody] OnboardingNegocioDto dto)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var cuentaId))
        {
            return Unauthorized();
        }

        var usuario = await _context.UsuarioEmprendedors
            .Include(u => u.Negocio)
            .FirstOrDefaultAsync(u => u.CuentaId == cuentaId);

        if (usuario is null)
        {
            return NotFound(new { mensaje = "No se encontró el perfil." });
        }

        var nit = dto.Formalizado ? dto.Nit?.Trim() : null;
        var razonSocial = dto.Formalizado ? dto.RazonSocial?.Trim() : null;
        var rues = dto.Formalizado ? dto.Rues?.Trim() : null;

        if (dto.Formalizado &&
            (string.IsNullOrWhiteSpace(nit) || string.IsNullOrWhiteSpace(razonSocial) ||
             string.IsNullOrWhiteSpace(rues)))
        {
            return BadRequest(new { mensaje = "Para un negocio formalizado el NIT, la razón social y el RUES son obligatorios." });
        }

        if (dto.TieneSitioFisico && string.IsNullOrWhiteSpace(dto.Direccion))
        {
            return BadRequest(new { mensaje = "La dirección es obligatoria si el negocio tiene sitio físico." });
        }

        var correo = dto.CorreoEmpresa.Trim().ToLowerInvariant();
        var telefono = dto.TelefonoEmpresa.Trim();

        var negocio = usuario.Negocio;
        if (negocio is null)
        {
            var tipo = await _context.TipoEmprendimientos
                .FirstOrDefaultAsync(t => t.Nombre == "Otros")
                ?? await _context.TipoEmprendimientos.FirstOrDefaultAsync();

            if (tipo is null)
            {
                return BadRequest(new { mensaje = "No hay tipos de emprendimiento configurados." });
            }

            negocio = new Negocio
            {
                UsuarioId = usuario.Id,
                TipoEmprendimientoId = tipo.Id
            };
            _context.Negocios.Add(negocio);
        }

        usuario.TipoFormalizacion = dto.Formalizado ? "formalizado" : "no_formalizado";
        usuario.NombreEmprendimiento = dto.NombreEmpresa.Trim();
        usuario.Nit = nit;
        usuario.RazonSocial = razonSocial;

        if (dto.Formalizado)
        {
            usuario.TipoDocumento = null;
            usuario.NumeroDocumento = null;
        }

        negocio.TelefonoContacto = telefono;
        negocio.CorreoContacto = correo;
        negocio.CorreoEmpresa = correo;
        negocio.Rues = rues;
        negocio.TieneSitioFisico = dto.TieneSitioFisico;
        negocio.Direccion = dto.TieneSitioFisico ? dto.Direccion?.Trim() : null;
        negocio.FechaActualizacion = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.LogoBase64))
        {
            if (!FotoPerfilHelper.TryNormalizar(dto.LogoBase64, out var logo, out var error))
            {
                return BadRequest(new { mensaje = error });
            }
            negocio.LogoUrl = logo;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { mensaje = "No se pudo guardar la información. Verifica que el NIT no esté registrado por otro negocio." });
        }

        return Ok(new { exito = true, mensaje = "Información del negocio guardada." });
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
