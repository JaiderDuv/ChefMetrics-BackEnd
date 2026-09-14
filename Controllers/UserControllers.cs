using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Controllers
{
    public class UserControllers : ControllerBase
    {
        private readonly ChedMetricsContext _context;

        public UserControllers(ChedMetricsContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.UsuarioEmprendedors.ToListAsync();

            return Ok(usuarios);
        }
    }
}
