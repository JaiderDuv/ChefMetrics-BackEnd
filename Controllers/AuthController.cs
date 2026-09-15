using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Inicia sesión de usuario con correo electrónico y contraseña.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var resultado = await _authService.IniciarSesionAsync(dto);
            if (!resultado.Exito)
            {
                return Unauthorized(resultado);
            }

            return Ok(resultado);
        }

        /// <summary>
        /// Solicita el envío de instrucciones para recuperar la contraseña.
        /// </summary>
        [HttpPost("recuperar-password")]
        public async Task<IActionResult> RecuperarPassword([FromBody] RecuperarPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var enrutado = await _authService.SolicitarRecuperacionPasswordAsync(dto);
                return Ok(new
                {
                    exito = enrutado,
                    mensaje = "Si el correo electrónico existe en nuestra plataforma, se enviarán las instrucciones de restablecimiento."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Restablece la contraseña utilizando la nueva contraseña proporcionada.
        /// </summary>
        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var actualizado = await _authService.RestablecerPasswordAsync(dto);
                return Ok(new
                {
                    exito = actualizado,
                    mensaje = "La contraseña ha sido actualizada con éxito."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
