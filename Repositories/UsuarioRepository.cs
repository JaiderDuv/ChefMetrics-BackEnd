using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ChedMetricsContext _context;

        public UsuarioRepository(ChedMetricsContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            return await _context.UsuarioEmprendedors
                .AnyAsync(u => u.CorreoElectronico.ToLower() == correo.ToLower());
        }

        public async Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor usuario)
        {
            await _context.UsuarioEmprendedors.AddAsync(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }
    }
}
