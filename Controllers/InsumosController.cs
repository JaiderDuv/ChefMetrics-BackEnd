using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api/insumos")]
[Authorize]
public sealed class InsumosController : ControllerBase
{
    private readonly IInsumoService _insumoService;

    public InsumosController(IInsumoService insumoService)
    {
        _insumoService = insumoService;
    }

    // POST /api/insumos — crea el insumo asociado al negocio del usuario autenticado.
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearInsumoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultado = await _insumoService.CrearAsync(cuentaId, dto);
        if (resultado.NoEncontrado)
        {
            return NotFound(new { mensaje = resultado.Mensaje });
        }
        if (!resultado.Exito)
        {
            return BadRequest(new { mensaje = resultado.Mensaje });
        }

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = resultado.Insumo!.Id },
            resultado.Insumo);
    }

    // GET /api/insumos?busqueda=&ordenarPor=nombre&direccion=asc&incluirInactivos=false&pagina=1&tamanoPagina=50
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltrosInsumoDto filtros)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var lista = await _insumoService.ListarAsync(cuentaId, filtros);
        return lista is null
            ? NotFound(new { mensaje = "Aún no tienes un negocio registrado para administrar insumos." })
            : Ok(lista);
    }

    // GET /api/insumos/historial-precios?nombre=&proveedor= — global del negocio,
    // del más reciente al más viejo.
    [HttpGet("historial-precios")]
    public async Task<IActionResult> ObtenerHistorialGeneral(
        [FromQuery] string? nombre,
        [FromQuery] string? proveedor)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _insumoService.ObtenerHistorialGeneralAsync(cuentaId, nombre, proveedor);
        return resultado.NoEncontrado
            ? NotFound(new { mensaje = resultado.Mensaje })
            : Ok(resultado.Historial);
    }

    // GET /api/insumos/{id} — detalle de un insumo específico.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _insumoService.ObtenerPorIdAsync(cuentaId, id);
        return resultado.NoEncontrado
            ? NotFound(new { mensaje = resultado.Mensaje })
            : Ok(resultado.Insumo);
    }

    // GET /api/insumos/{id}/historial-precios — del más reciente al más viejo.
    [HttpGet("{id:int}/historial-precios")]
    public async Task<IActionResult> ObtenerHistorialPrecios([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _insumoService.ObtenerHistorialPreciosAsync(cuentaId, id);
        return resultado.NoEncontrado
            ? NotFound(new { mensaje = resultado.Mensaje })
            : Ok(resultado.Historial);
    }

    // PATCH /api/insumos/{id} — edición parcial de un insumo específico.
    // Para cambiar el precio es obligatorio enviar el proveedor en la misma solicitud.
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> ActualizarParcial(
        [FromRoute] int id,
        [FromBody] ActualizarInsumoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultado = await _insumoService.ActualizarParcialAsync(cuentaId, id, dto);
        if (resultado.NoEncontrado)
        {
            return NotFound(new { mensaje = resultado.Mensaje });
        }
        if (!resultado.Exito)
        {
            return BadRequest(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Insumo);
    }

    // POST /api/insumos/{id}/restaurar — revive un insumo eliminado a partir
    // de su historial archivado, con sus últimos datos guardados.
    [HttpPost("{id:int}/restaurar")]
    public async Task<IActionResult> Restaurar([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _insumoService.RestaurarAsync(cuentaId, id);
        if (resultado.NoEncontrado)
        {
            return NotFound(new { mensaje = resultado.Mensaje });
        }
        if (resultado.Conflicto)
        {
            return Conflict(new { mensaje = resultado.Mensaje });
        }
        if (!resultado.Exito)
        {
            return BadRequest(new { mensaje = resultado.Mensaje });
        }

        return Ok(resultado.Insumo);
    }

    // DELETE /api/insumos/{id} — bloqueado si el insumo está asociado a una receta.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId))
        {
            return Unauthorized();
        }

        var resultado = await _insumoService.EliminarAsync(cuentaId, id);
        if (resultado.NoEncontrado)
        {
            return NotFound(new { mensaje = resultado.Mensaje });
        }
        if (resultado.BloqueadoPorRecetas)
        {
            return Conflict(new
            {
                mensaje = resultado.Mensaje,
                numeroRecetas = resultado.NumeroRecetas
            });
        }
        if (!resultado.Exito)
        {
            return BadRequest(new { mensaje = resultado.Mensaje });
        }

        return Ok(new { mensaje = resultado.Mensaje });
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(subject, out cuentaId);
    }
}
