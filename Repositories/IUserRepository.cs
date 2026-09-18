using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExisteCorreoAsync(string email);
        Task<UsuarioEmprendedor> RegistrarUsuarioAsync(UsuarioEmprendedor user);
        Task<UsuarioEmprendedor?> ObtenerPorEmailAsync(string email);
        Task<RefreshToken> RegistrarRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> ObtenerPorTokenVerificacionHashAsync(string tokenHash);
        Task<RefreshToken?> ObtenerPorTokenRecuperacionHashAsync(string tokenHash);
        Task ActualizarRefreshTokenAsync(RefreshToken refreshToken);
        Task RestablecerPasswordYRevocarSesionesAsync(
            UsuarioEmprendedor usuario,
            RefreshToken tokenRecuperacion);
        Task ActualizarAsync(UsuarioEmprendedor user);
    }
}
