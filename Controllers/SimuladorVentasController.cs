using System.Security.Claims;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

// POST /api/simulador-ventas/calcular
// Simulación mensual sin persistencia: recibe cantidades y devuelve los
// totales del escenario. No crea ni modifica registros de ventas.
[ApiController]
[Route("api/simulador-ventas")]
[Authorize]
public sealed class SimuladorVentasController : ControllerBase
{
    private readonly ISimuladorVentasService _servicio;

    public SimuladorVentasController(ISimuladorVentasService servicio) => _servicio = servicio;

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular([FromBody] SimularVentasDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.CalcularAsync(cuentaId, dto);
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
