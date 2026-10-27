using System.Security.Claims;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public sealed class EvaluacionMenuController : ControllerBase
{
    private readonly IEvaluacionMenuService _servicio;
    public EvaluacionMenuController(IEvaluacionMenuService servicio) => _servicio = servicio;

    // GET /api/menu/umbral
    [HttpGet("umbral")]
    public async Task<IActionResult> ObtenerUmbral()
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.ObtenerUmbralAsync(cuentaId);
        return Responder(r);
    }

    // PUT /api/menu/umbral { umbralMargenMinimo }
    [HttpPut("umbral")]
    public async Task<IActionResult> ActualizarUmbral([FromBody] ActualizarUmbralDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.ActualizarUmbralAsync(cuentaId, dto);
        return Responder(r);
    }

    // GET /api/menu/evaluacion
    [HttpGet("evaluacion")]
    public async Task<IActionResult> Evaluar()
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.EvaluarAsync(cuentaId);
        return Responder(r);
    }

    private IActionResult Responder(ResultadoEvaluacionDto r)
    {
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