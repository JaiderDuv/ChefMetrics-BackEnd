using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/perfil")]
[Authorize]
public sealed class PerfilEmprendimientoController : ControllerBase
{
    private readonly IPerfilEmpreService _perfilService;

    public PerfilEmprendimientoController(IPerfilEmpreService perfilService)
    {
        _perfilService = perfilService;
    }

    [HttpPost("confirmar-identidad")]
    public async Task<IActionResult> ConfirmarIdentidad(
        [FromBody] ConfirmarEdicionPerfilDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var permiso = await _perfilService.ConfirmarIdentidadAsync(cuentaId, dto);
        return permiso is null
            ? Unauthorized(new { mensaje = "La contraseña actual no es válida." })
            : Ok(permiso);
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerPerfil(
        [FromHeader(Name = "X-Profile-Edit-Token")] string? editToken)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var perfil = await _perfilService.ObtenerPerfilAsync(cuentaId, editToken ?? string.Empty);
        return perfil is null
            ? Unauthorized(new { mensaje = "La autorización de edición es inválida o ha expirado." })
            : Ok(perfil);
    }

    [HttpPut]
    public async Task<IActionResult> ActualizarPerfil(
        [FromHeader(Name = "X-Profile-Edit-Token")] string? editToken,
        [FromBody] PerfilEmprendedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _perfilService.ActualizarPerfilAsync(
            cuentaId,
            editToken ?? string.Empty,
            dto);

        if (resultado.Exito)
        {
            return Ok(resultado);
        }

        return resultado.RequiereReautenticacion
            ? Unauthorized(resultado)
            : BadRequest(resultado);
    }

    // Perfil de usuario: libre, sin contraseña ni token de edición. Solo JWT.
    // Cada GET autocompleta el formulario con lo guardado en BD.
    [HttpGet("usuario")]
    public async Task<IActionResult> ObtenerPerfilUsuario()
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var perfil = await _perfilService.ObtenerPerfilUsuarioAsync(cuentaId);
        return perfil is null
            ? NotFound(new { mensaje = "No se encontró el perfil del usuario." })
            : Ok(perfil);
    }

    [HttpPut("usuario")]
    public async Task<IActionResult> ActualizarPerfilUsuario(
        [FromBody] PerfilUsuarioDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _perfilService.ActualizarPerfilUsuarioAsync(cuentaId, dto);
        return resultado.Exito
            ? Ok(resultado)
            : BadRequest(resultado);
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(subject, out cuentaId);
    }
}
