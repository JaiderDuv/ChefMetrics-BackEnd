using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

// CRUD /api/productos-menu + costeo y precio sugerido del menú.
[ApiController]
[Route("api/productos-menu")]
[Authorize]
public sealed class ProductosMenuController : ControllerBase
{
    private readonly IProductoMenuService _servicio;
    private readonly ICostoProductoService _costos;
    private readonly ISugerirPrecioProductoService _precios;

    public ProductosMenuController(
        IProductoMenuService servicio,
        ICostoProductoService costos,
        ISugerirPrecioProductoService precios)
    {
        _servicio = servicio;
        _costos = costos;
        _precios = precios;
    }

    // GET /api/productos-menu?busqueda=&incluirInactivos=false
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool incluirInactivos = false)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var lista = await _servicio.ListarAsync(cuentaId, busqueda, incluirInactivos);
        return lista is null
            ? NotFound(new { mensaje = "Aún no tienes un negocio registrado para administrar el menú." })
            : Ok(lista);
    }

    // GET /api/productos-menu/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.ObtenerAsync(cuentaId, id);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(r.Producto);
    }

    // POST /api/productos-menu -> 201 + Location al nuevo detalle.
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProductoMenuDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.CrearAsync(cuentaId, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return CreatedAtAction(nameof(ObtenerPorId), new { id = r.Producto!.Id }, r.Producto);
    }

    // PUT /api/productos-menu/{id} — reemplazo total estandarizado.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar([FromRoute] int id, [FromBody] ActualizarProductoMenuDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.ActualizarAsync(cuentaId, id, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Producto);
    }

    // PATCH /api/productos-menu/{id} — solo campos editados (evita pérdida de datos).
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> ActualizarParcial([FromRoute] int id, [FromBody] PatchProductoMenuDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var r = await _servicio.ActualizarParcialAsync(cuentaId, id, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Producto);
    }

    // DELETE /api/productos-menu/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar([FromRoute] int id)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _servicio.EliminarAsync(cuentaId, id);
        return r.NoEncontrado ? NotFound(new { mensaje = r.Mensaje }) : Ok(new { mensaje = r.Mensaje });
    }

    // GET /api/productos-menu/{id}/costo?costosFijosIds=
    // Sin parámetro = todos los fijos activos; vacío = ninguno; IDs = selección.
    [HttpGet("{id:int}/costo")]
    public async Task<IActionResult> ObtenerCosto([FromRoute] int id, [FromQuery] string[]? costosFijosIds)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var ids = Request.LeerCostosFijosIds(costosFijosIds);
        var r = await _costos.CalcularAsync(cuentaId, id, null, ids);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    // POST /api/productos-menu/costo-preview — costo antes de guardar (soporta empaques).
    [HttpPost("costo-preview")]
    public async Task<IActionResult> CostoPreview([FromBody] PreviewCostoProductoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = await _costos.CalcularPreviewAsync(cuentaId, dto);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    // POST /api/productos-menu/{id}/precio-sugerido { margenDeseado, costosFijosIds }
    // costosFijosIds vacío = precio con costo directo; null = incluye todos los fijos.
    [HttpPost("{id:int}/precio-sugerido")]
    public async Task<IActionResult> PrecioSugerido([FromRoute] int id, [FromBody] SugerirPrecioProductoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        var r = await _precios.CalcularAsync(cuentaId, id, dto.MargenDeseado, dto.CostosFijosIds);
        if (r.NoEncontrado) return NotFound(new { mensaje = r.Mensaje });
        if (!r.Exito) return BadRequest(new { mensaje = r.Mensaje });
        return Ok(r.Datos);
    }

    // POST /api/productos-menu/precio-sugerido-preview — sugerencia antes de guardar.
    [HttpPost("precio-sugerido-preview")]
    public async Task<IActionResult> PrecioPreview([FromBody] PreviewPrecioProductoDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = await _precios.CalcularPreviewAsync(cuentaId, dto);
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
