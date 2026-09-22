using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ChefMetricsContext _context;

        public UserRepository(ChefMetricsContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteCorreoAsync(string email)
        {
            return await _context.Cuenta
                .AnyAsync(c => c.Correo.ToLower() == email.ToLower());
        }

        public async Task<Cuenta> RegistrarCuentaAsync(Cuenta cuenta)
        {
            await _context.Cuenta.AddAsync(cuenta);
            await _context.SaveChangesAsync();
            return cuenta;
        }

        public async Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor usuario)
        {
            await _context.UsuarioEmprendedors.AddAsync(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<Negocio> RegistrarNegocioAsync(Negocio negocio)
        {
            await _context.Negocios.AddAsync(negocio);
            await _context.SaveChangesAsync();
            return negocio;
        }

        public async Task<TipoEmprendimiento> ObtenerOCrearTipoEmprendimientoAsync(string nombre)
        {
            var normalizado = string.IsNullOrWhiteSpace(nombre) ? "General" : nombre.Trim();
            var existente = await _context.TipoEmprendimientos
                .FirstOrDefaultAsync(t => t.Nombre.ToLower() == normalizado.ToLower());
            if (existente != null)
            {
                return existente;
            }

            var nuevo = new TipoEmprendimiento { Nombre = normalizado };
            await _context.TipoEmprendimientos.AddAsync(nuevo);
            await _context.SaveChangesAsync();
            return nuevo;
        }

        public async Task<Cuenta?> ObtenerCuentaPorEmailAsync(string email)
        {
            return await _context.Cuenta
                .Include(c => c.UsuarioEmprendedor)
                .FirstOrDefaultAsync(c => c.Correo.ToLower() == email.ToLower());
        }

        public async Task<RefreshToken> RegistrarRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<RefreshToken?> ObtenerRefreshTokenPorHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(t => t.Cuenta)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task RevocarRefreshTokensPorCuentaAsync(int cuentaId)
        {
            await _context.RefreshTokens
                .Where(t => t.CuentaId == cuentaId && !t.Revocado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Revocado, true));
        }

        public async Task<PasswordResetToken> RegistrarPasswordResetTokenAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken?> ObtenerPasswordResetPorHashAsync(string tokenHash)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.Cuenta)
                .ThenInclude(c => c.UsuarioEmprendedor)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task ConfirmarEmailPorTokenAsync(Cuenta cuenta, PasswordResetToken token)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            token.Usado = true;
            cuenta.EmailVerificado = true;
            cuenta.Activo = true;

            _context.PasswordResetTokens.Update(token);
            _context.Cuenta.Update(cuenta);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task RestablecerPasswordYRevocarSesionesAsync(
            Cuenta cuenta,
            PasswordResetToken tokenRecuperacion,
            string nuevoPasswordHash)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            cuenta.PasswordHash = nuevoPasswordHash;
            tokenRecuperacion.Usado = true;

            _context.Cuenta.Update(cuenta);
            _context.PasswordResetTokens.Update(tokenRecuperacion);

            await _context.RefreshTokens
                .Where(t => t.CuentaId == cuenta.Id && !t.Revocado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Revocado, true));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ActualizarCuentaAsync(Cuenta cuenta)
        {
            _context.Cuenta.Update(cuenta);
            await _context.SaveChangesAsync();
        }
    }
}
