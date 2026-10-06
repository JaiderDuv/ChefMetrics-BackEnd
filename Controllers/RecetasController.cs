using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/recetas")]
[Authorize]
public sealed class RecetasController : ControllerBase
{
    private readonly IRecetaService _recetaService;

    public RecetasController(IRecetaService recetaService) => _recetaService = recetaService;

    // GET /api/recetas?busqueda=
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var lista = await _recetaService.ListarAsync(cuentaId, busqueda);
        return lista is null
            ? NotFound(new { mensaje = "Aún no tienes un negocio registrado para administrar recetas." })
            : Ok(lista);
    }

    // GET /api/recetas/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var r = await _recetaService.ObtenerPorIdAsync(cuentaId, id);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(r.Receta);
    }

    // POST /api/recetas
    // POST /api/recetas
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarRecetaDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var r = await _recetaService.CrearAsync(cuentaId, dto);
            if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
            if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });

            return CreatedAtAction(nameof(ObtenerPorId), new { id = r.Receta!.Id }, r.Receta);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = ex.InnerException?.Message ?? ex.Message });
        }
    }

    // PUT /api/recetas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar([FromRoute] int id, [FromBody] GuardarRecetaDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _recetaService.ActualizarAsync(cuentaId, id, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });

        return Ok(r.Receta);
    }

    // DELETE /api/recetas/{id} — definitiva; bloqueada si está en un producto del menú.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var r = await _recetaService.EliminarAsync(cuentaId, id);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (r.BloqueadaPorMenu)
            return Conflict(new { mensaje = r.Mensaje, numeroProductos = r.NumeroProductos });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });

        return Ok(new { mensaje = r.Mensaje });
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(subject, out cuentaId);
    }

}