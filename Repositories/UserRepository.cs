using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Utilities;
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

        public async Task<Cuenta?> ObtenerCuentaPorIdAsync(int cuentaId)
        {
            return await _context.Cuenta
                .Include(c => c.UsuarioEmprendedor)
                .FirstOrDefaultAsync(c => c.Id == cuentaId);
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
                .ThenInclude(c => c.UsuarioEmprendedor)
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
            // Vigencia única de 15 minutos para todos los tokens por correo.
            token.FechaExpiracion = DateTime.UtcNow.AddMinutes(15);

            // Al emitir uno nuevo, los anteriores sin usar del mismo tipo
            // quedan invalidados: solo el último enlace es válido.
            await _context.PasswordResetTokens
                .Where(t => t.CuentaId == token.CuentaId && t.Tipo == token.Tipo && !t.Usado)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Usado, true));

            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken?> ObtenerTokenPorHashAsync(string tokenHash, string tipo)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.Cuenta)
                .ThenInclude(c => c.UsuarioEmprendedor)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Tipo == tipo);
        }

        public async Task<bool> RotarRefreshTokenAsync(RefreshToken actual, RefreshToken nuevo)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var revocado = await _context.RefreshTokens
                .Where(t => t.Id == actual.Id && !t.Revocado && t.FechaExpiracion > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Revocado, true));

            if (revocado != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _context.RefreshTokens.AddAsync(nuevo);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<bool> RevocarRefreshTokenAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Where(t => t.TokenHash == tokenHash && !t.Revocado)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Revocado, true)) == 1;
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