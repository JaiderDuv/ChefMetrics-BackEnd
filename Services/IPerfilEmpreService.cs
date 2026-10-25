using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Services;

public interface IPerfilEmpreService
{
    Task<PermisoEdicionPerfilDto?> ConfirmarIdentidadAsync(
        int cuentaId,
        ConfirmarEdicionPerfilDto dto);

    // Marca como usados los permisos pendientes (al salir del formulario o cerrar sesión).
    Task RevocarPermisoAsync(int cuentaId);

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

    // Catálogo de roles para el desplegable de empleados (tabla Roles).
    Task<List<RolDto>> ObtenerRolesAsync();
}
