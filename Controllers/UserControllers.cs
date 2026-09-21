using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserControllers : ControllerBase
    {
        private readonly ChefMetricsContext _context;

        public UserControllers(ChefMetricsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.UsuarioEmprendedors
                .AsNoTracking()
                .Include(u => u.Cuenta)
                .Select(u => new
                {
                    u.Id,
                    u.NombreEmprendimiento,
                    u.OnboardingCompletado,
                    Correo = u.Cuenta.Correo
                })
                .ToListAsync();

            return Ok(usuarios);
        }
    }
}
