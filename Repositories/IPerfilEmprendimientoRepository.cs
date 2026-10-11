using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories;

public interface IPerfilEmprendimientoRepository
{
    Task<Cuenta?> ObtenerPerfilCompletoAsync(int cuentaId);
    Task<PerfilEditGrant?> ObtenerPermisoEdicionAsync(string tokenHash);
    Task<PerfilEditGrant> RegistrarPermisoEdicionAsync(PerfilEditGrant permiso);
    Task RevocarPermisosEdicionAsync(int cuentaId);
    Task<bool> ExisteCorreoAsync(string correo, int cuentaId);
    Task<bool> ExisteNitAsync(string nit, int cuentaId);
    Task<bool> ExisteDocumentoAsync(string numeroDocumento, int cuentaId);
    Task<List<Role>> ObtenerRolesAsync();
    Task<bool> ExisteRolAsync(int rolId);
    Task<bool> ExisteEmpleadoCorreoAsync(int negocioId, string correo, int? excluirId = null);
    Task GuardarEmpleadosAsync(
        PerfilEditGrant permiso,
        IEnumerable<Empleado> empleadosNuevos,
        IEnumerable<Empleado> empleadosEliminados);
    Task GuardarCambiosUsuarioAsync();
}
