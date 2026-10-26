using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [AllowAnonymous]
        [HttpPost("registro-formalizado")]
        public async Task<IActionResult> RegistrarFormalizado([FromBody] EmprendedorFormalizado dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.NombreEmprendimiento?.Length > 100)
            {
                return BadRequest();
            }

            try
            {
                var resultado = await _usuarioService.RegistrarEmprendedorFormalizadoAsync(dto);
                return Ok(new { mensaje = "Registro exitoso", exito = resultado });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("registro-no-formalizado")]
        public async Task<IActionResult> RegistrarNoFormalizado([FromBody] Emprendedor dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.NombreEmprendimiento?.Length > 100)
            {
                return BadRequest();
            }

            try
            {
                var resultado = await _usuarioService.RegistrarEmprendedorAsync(dto);
                return Ok(new { mensaje = "Registro exitoso", exito = resultado });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("validar-token")]
        public async Task<IActionResult> ValidarToken([FromBody] TokenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var valido = await _usuarioService.ValidarTokenAsync(dto.Token);

            if (!valido)
            {
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "El enlace de verificación ha expirado o ya fue utilizado. Por favor, solicita uno nuevo o inicia sesión si ya confirmaste tu cuenta."
                });
            }

            return Ok(new
            {
                exito = true,
                mensaje = "¡Tu correo electrónico ha sido verificado con éxito!"
            });
        }

        [AllowAnonymous]
        [HttpPost("reenviar-verificacion")]
        public async Task<IActionResult> ReenviarVerificacion([FromBody] ReenviarCorreoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _usuarioService.ReenviarCorreoVerificacionAsync(dto.Correo);

                return Ok(new
                {
                    exito = true,
                    mensaje = "Si el correo está registrado y pendiente de verificación, recibirás un nuevo enlace en breve."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    exito = false,
                    mensaje = $"Error al procesar el reenvío: {ex.Message}"
                });
            }
        }
    }
}