using System.Security.Claims;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

// POST /api/recetas/{id}/precio-sugerido
[ApiController]
[Route("api/recetas/{id:int}/precio-sugerido")]
[Authorize]
public sealed class SugerirPrecioController : ControllerBase
{
    private readonly ISugerirPrecioService _servicio;
    public SugerirPrecioController(ISugerirPrecioService servicio) => _servicio = servicio;

    [HttpPost]
    public async Task<IActionResult> Calcular([FromRoute] int id, [FromBody] SugerirPrecioDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var r = await _servicio.CalcularAsync(cuentaId, id, dto.MargenDeseado, dto.CostosFijosIds);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(s, out cuentaId);
    }
}