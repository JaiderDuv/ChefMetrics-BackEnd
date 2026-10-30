using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefMetrics_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [AllowAnonymous]
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

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new { exito = true });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var resultado = await _authService.RenovarSesionAsync(dto);
            return resultado is null ? Unauthorized() : Ok(resultado);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _authService.CerrarSesionAsync(dto);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("recuperar-password")]
        public async Task<IActionResult> RecuperarPassword([FromBody] RecuperarPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _authService.SolicitarRecuperacionPasswordAsync(dto);

                // Siempre se responde igual para no revelar si el correo existe.
                return Ok(new
                {
                    exito = true,
                    mensaje = "Si el correo electrónico existe en nuestra plataforma, se enviarán las instrucciones de restablecimiento."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar la recuperación de contraseña.");
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "No se pudo procesar la solicitud de recuperación. Intenta de nuevo más tarde."
                });
            }
        }

        [AllowAnonymous]
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
                _logger.LogError(ex, "Error al restablecer la contraseña.");
                return BadRequest(new
                {
                    exito = false,
                    mensaje = "No se pudo procesar el restablecimiento de contraseña. Intenta de nuevo más tarde."
                });
            }
        }
    }
}
