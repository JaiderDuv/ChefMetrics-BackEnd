using System.Security.Claims;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedorService;

    public ProveedoresController(IProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    [HttpGet("negocios/mio/proveedores")]
    public async Task<IActionResult> ListarDelNegocio()
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.ListarProveedoresDelNegocioAsync(cuentaId);
        return Responder(resultado);
    }

    [HttpPost("negocios/mio/proveedores")]
    public async Task<IActionResult> Crear([FromBody] CrearProveedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _proveedorService.CrearProveedorAsync(cuentaId, dto);
        return Responder(resultado);
    }

    [HttpGet("insumos/{insumoId:int}/proveedores")]
    public async Task<IActionResult> ListarPorInsumo(int insumoId)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.ListarPorInsumoAsync(cuentaId, insumoId);
        return Responder(resultado);
    }

    [HttpGet("insumos/{insumoId:int}/proveedores-disponibles")]
    public async Task<IActionResult> ListarDisponibles(int insumoId)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.ListarDisponiblesParaInsumoAsync(cuentaId, insumoId);
        return Responder(resultado);
    }

    [HttpPost("insumos/{insumoId:int}/proveedores")]
    public async Task<IActionResult> Asociar(int insumoId, [FromBody] AsociarProveedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _proveedorService.AsociarAsync(cuentaId, insumoId, dto);
        return Responder(resultado);
    }

    [HttpPost("insumos/{insumoId:int}/proveedores/nuevo")]
    public async Task<IActionResult> CrearYAsociar(int insumoId, [FromBody] CrearYAsociarProveedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _proveedorService.CrearYAsociarAsync(cuentaId, insumoId, dto);
        return Responder(resultado);
    }

    [HttpDelete("insumos/{insumoId:int}/proveedores/{proveedorId:int}")]
    public async Task<IActionResult> Desasociar(int insumoId, int proveedorId)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.DesasociarAsync(cuentaId, insumoId, proveedorId);
        return Responder(resultado);
    }

    [HttpPatch("insumos/{insumoId:int}/proveedores/{proveedorId:int}/principal")]
    public async Task<IActionResult> MarcarPrincipal(int insumoId, int proveedorId)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.MarcarPrincipalAsync(cuentaId, insumoId, proveedorId);
        return Responder(resultado);
    }

    [HttpPatch("insumos/{insumoId:int}/proveedores/{proveedorId:int}/costo")]
    public async Task<IActionResult> ActualizarCosto(int insumoId, int proveedorId, [FromBody] ActualizarCostoProveedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _proveedorService.ActualizarCostoAsync(cuentaId, insumoId, proveedorId, dto.Costo);
        return Responder(resultado);
    }

    private IActionResult Responder(ResultadoProveedorDto resultado)
    {
        if (resultado.NoEncontrado) return NotFound(new { mensaje = resultado.Mensaje });
        if (!resultado.Exito) return BadRequest(new { mensaje = resultado.Mensaje });
        return Ok(resultado.Datos);
    }

    private bool TryObtenerCuentaId(out int cuentaId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(subject, out cuentaId);
    }

    [HttpPatch("proveedores/{proveedorId:int}")]
    public async Task<IActionResult> Actualizar(int proveedorId, [FromBody] ActualizarProveedorDto dto)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var resultado = await _proveedorService.ActualizarProveedorAsync(cuentaId, proveedorId, dto);
        return Responder(resultado);
    }

    [HttpDelete("proveedores/{proveedorId:int}")]
    public async Task<IActionResult> Eliminar(int proveedorId)
    {
        if (!TryObtenerCuentaId(out var cuentaId)) return Unauthorized();

        var resultado = await _proveedorService.EliminarProveedorAsync(cuentaId, proveedorId);
        return Responder(resultado);
    }
}