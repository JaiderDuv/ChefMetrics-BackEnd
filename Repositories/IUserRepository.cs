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
        Task ActualizarRefreshTokenAsync(RefreshToken refreshToken);
        Task ActualizarAsync(UsuarioEmprendedor user);
    }
}
