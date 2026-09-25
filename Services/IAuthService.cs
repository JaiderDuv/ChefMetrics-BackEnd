using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> IniciarSesionAsync(LoginDto dto);
        Task<LoginResponseDto?> RenovarSesionAsync(RefreshTokenDto dto);
        Task<bool> CerrarSesionAsync(RefreshTokenDto dto);
        Task<bool> SolicitarRecuperacionPasswordAsync(RecuperarPasswordDto dto);
        Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto);
    }
}
