using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

// GET /api/recetas/{id}/costo
// Sin costosFijosIds en la query, el binding entrega una lista vacía:
// el cálculo usa el costo directo (ningún fijo). Para incluir fijos,
// enviar sus IDs como parámetros repetidos (?costosFijosIds=1&costosFijosIds=2).
[ApiController]
[Route("api/recetas/{id:int}/costo")]
[Authorize]
public sealed class CostoRecetaController : ControllerBase
{
    private readonly ICostoRecetaService _servicio;
    public CostoRecetaController(ICostoRecetaService servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<IActionResult> Obtener([FromRoute] int id, [FromQuery] List<int>? costosFijosIds)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.CalcularAsync(cuentaId, id, costosFijosIds);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(r.Datos);
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(s, out cuentaId);
    }
}

[ApiController]
[Route("api/recetas/{id:int}/escalado")]
[Authorize]
public sealed class EscaladoRecetaController : ControllerBase
{
    // Igual que el costo: sin costosFijosIds se usa el costo directo.
    // Para incluir fijos, enviar sus IDs como parámetros repetidos.
    private readonly ICostoRecetaService _servicio;
    public EscaladoRecetaController(ICostoRecetaService servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<IActionResult> Obtener([FromRoute] int id, [FromQuery] int porciones, [FromQuery] List<int>? costosFijosIds)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (porciones <= 0) return BadRequest(new { mensaje = "Las porciones deseadas deben ser mayores a 0." });

        var r = await _servicio.CalcularEscaladoAsync(cuentaId, id, porciones, costosFijosIds);
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

// CRUD /api/costos-fijos
[ApiController]
[Route("api/costos-fijos")]
[Authorize]
public sealed class CostosFijosController : ControllerBase
{
    private readonly ICostosFijosService _servicio;
    private readonly IPuntoEquilibrioService _equilibrio;

    public CostosFijosController(ICostosFijosService servicio, IPuntoEquilibrioService equilibrio)
    {
        _servicio = servicio;
        _equilibrio = equilibrio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivos = false)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var lista = await _servicio.ListarAsync(cuentaId, incluirInactivos);
        return lista is null
            ? NotFound(new { mensaje = "Aún no tienes un negocio registrado para administrar costos fijos." })
            : Ok(lista);
    }

    // GET /api/costos-fijos/punto-equilibrio
    // Debe declararse antes de {id:int} por claridad; el constraint int evita ambigüedad.
    [HttpGet("punto-equilibrio")]
    public async Task<IActionResult> PuntoEquilibrio()
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _equilibrio.CalcularAsync(cuentaId);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.ObtenerAsync(cuentaId, id);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(r.Datos);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCostoFijoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.CrearAsync(cuentaId, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return CreatedAtAction(nameof(ObtenerPorId), new { id = r.Datos!.Id }, r.Datos);
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Actualizar([FromRoute] int id, [FromBody] ActualizarCostoFijoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.ActualizarAsync(cuentaId, id, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.EliminarAsync(cuentaId, id);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(new { mensaje = r.Mensaje });
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(s, out cuentaId);
    }
}