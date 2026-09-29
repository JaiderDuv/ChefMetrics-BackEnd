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
    Task GuardarCambiosAsync(
        PerfilEditGrant permiso,
        IEnumerable<SocioNegocio> sociosNuevos,
        IEnumerable<SocioNegocio> sociosEliminados);
    Task GuardarCambiosUsuarioAsync();
}
