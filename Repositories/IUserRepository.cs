using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExisteCorreoAsync(string email);
        Task<Cuenta> RegistrarCuentaAsync(Cuenta cuenta);
        Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor usuario);
        Task<Negocio> RegistrarNegocioAsync(Negocio negocio);
        Task<TipoEmprendimiento> ObtenerOCrearTipoEmprendimientoAsync(string nombre);
        Task<Cuenta?> ObtenerCuentaPorEmailAsync(string email);
        Task<RefreshToken> RegistrarRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> ObtenerRefreshTokenPorHashAsync(string tokenHash);
        Task RevocarRefreshTokensPorCuentaAsync(int cuentaId);
        Task<PasswordResetToken> RegistrarPasswordResetTokenAsync(PasswordResetToken token);
        Task<PasswordResetToken?> ObtenerPasswordResetPorHashAsync(string tokenHash);
        Task ConfirmarEmailPorTokenAsync(Cuenta cuenta, PasswordResetToken token);
        Task RestablecerPasswordYRevocarSesionesAsync(
            Cuenta cuenta,
            PasswordResetToken tokenRecuperacion,
            string nuevoPasswordHash);
        Task ActualizarCuentaAsync(Cuenta cuenta);
    }
}
