using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> IniciarSesionAsync(LoginDto dto);
        Task<bool> SolicitarRecuperacionPasswordAsync(RecuperarPasswordDto dto);
        Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto);
    }
}
