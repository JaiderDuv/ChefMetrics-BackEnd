using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ChedMetricsContext _context;

        public UserRepository(ChedMetricsContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteCorreoAsync(string email)
        {
            return await _context.UsuarioEmprendedors
                .AnyAsync(u => u.CorreoElectronico.ToLower() == email.ToLower());
        }

        public async Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor user)
        {
            await _context.UsuarioEmprendedors.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<UsuarioEmprendedor?> ObtenerPorEmailAsync(string email)
        {
            return await _context.UsuarioEmprendedors
                .FirstOrDefaultAsync(u => u.CorreoElectronico.ToLower() == email.ToLower());
        }

        public async Task<RefreshToken> RegistrarRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<RefreshToken?> ObtenerPorTokenVerificacionHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenVerificacionHash == tokenHash);
        }

        public async Task<RefreshToken?> ObtenerPorTokenRecuperacionHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(t => t.IdUsuarioEmprendedorNavigation)
                .FirstOrDefaultAsync(t => t.PasswordResetTokenHash == tokenHash);
        }

        public async Task ActualizarRefreshTokenAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task RestablecerPasswordYRevocarSesionesAsync(
            UsuarioEmprendedor usuario,
            RefreshToken tokenRecuperacion)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            tokenRecuperacion.PasswordResetTokenUsado = true;
            _context.UsuarioEmprendedors.Update(usuario);
            _context.RefreshTokens.Update(tokenRecuperacion);

            await _context.RefreshTokens
                .Where(t => t.IdUsuarioEmprendedor == usuario.IdUsuarioEmprendedor &&
                            t.RefreshTokenHash != null &&
                            !t.RefreshTokenRevocado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.RefreshTokenRevocado, true));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ActualizarAsync(UsuarioEmprendedor user)
        {
            _context.UsuarioEmprendedors.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
