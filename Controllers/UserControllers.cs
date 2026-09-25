using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChefMetrics_BackEnd.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ChefMetrics_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserControllers : ControllerBase
    {
        private readonly ChefMetricsContext _context;

        public UserControllers(ChefMetricsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarioActual()
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(subject, out var cuentaId))
            {
                return Unauthorized();
            }

            var usuario = await _context.UsuarioEmprendedors
                .AsNoTracking()
                .Include(u => u.Cuenta)
                .Where(u => u.CuentaId == cuentaId)
                .Select(u => new
                {
                    u.Id,
                    u.NombreEmprendimiento,
                    u.OnboardingCompletado,
                    Correo = u.Cuenta.Correo
                })
                .SingleOrDefaultAsync();

            return usuario is null ? NotFound() : Ok(usuario);
        }
    }
}
