using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories;

public interface IProveedorRepository
{
    // Mismo patrón que InsumoRepository.ObtenerNegocioIdPorCuentaAsync
    Task<int?> ObtenerNegocioIdPorCuentaAsync(int cuentaId);

    Task<int?> ObtenerNegocioIdDeInsumoAsync(int insumoId);
    Task<bool> ExisteInsumoEnNegocioAsync(int negocioId, int insumoId);

    Task<List<Proveedor>> ListarPorNegocioAsync(int negocioId);
    Task<Proveedor?> ObtenerPorIdAsync(int proveedorId);
    Task<Proveedor> CrearAsync(Proveedor proveedor);

    Task<List<InsumoProveedor>> ListarPorInsumoAsync(int insumoId);
    Task<InsumoProveedor?> ObtenerAsociacionAsync(int insumoId, int proveedorId);
    Task<bool> ExisteAsociacionAsync(int insumoId, int proveedorId);
    Task<InsumoProveedor> AsociarAsync(int insumoId, int proveedorId, decimal costo);
    Task<bool> DesasociarAsync(int insumoId, int proveedorId);
    Task<bool> MarcarPrincipalAsync(int insumoId, int proveedorId);
    Task<bool> ActualizarCostoAsync(int insumoId, int proveedorId, decimal nuevoCosto);

    Task<Proveedor?> ObtenerPorIdDelNegocioAsync(int negocioId, int proveedorId);
    Task<int> ContarAsociacionesAsync(int proveedorId);
    Task ActualizarAsync(Proveedor proveedor);
    void Eliminar(Proveedor proveedor);
    Task GuardarCambiosAsync();
    Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null);
}