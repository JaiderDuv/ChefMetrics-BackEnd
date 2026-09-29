using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface IPerfilEmpreService
{
    Task<PermisoEdicionPerfilDto?> ConfirmarIdentidadAsync(
        int cuentaId,
        ConfirmarEdicionPerfilDto dto);

    Task<PerfilEmprendedorDto?> ObtenerPerfilAsync(
        int cuentaId,
        string editToken);

    Task<ActualizarPerfilResultadoDto> ActualizarPerfilAsync(
        int cuentaId,
        string editToken,
        PerfilEmprendedorDto dto);

    // Perfil de usuario: libre, sin contraseña ni token de edición. Solo JWT.
    Task<PerfilUsuarioDto?> ObtenerPerfilUsuarioAsync(int cuentaId);

    Task<ActualizarPerfilResultadoDto> ActualizarPerfilUsuarioAsync(
        int cuentaId,
        PerfilUsuarioDto dto);
}
