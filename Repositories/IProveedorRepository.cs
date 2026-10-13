using ChefMetrics_BackEnd.Model;
namespace ChefMetrics_BackEnd.Repositories;

public interface IProveedorRepository
{
    Task<int?> ObtenerNegocioIdPorCuentaAsync(int cuentaId);

    Task<int?> ObtenerNegocioIdDeInsumoAsync(int insumoId);
    Task<bool> ExisteInsumoEnNegocioAsync(int negocioId, int insumoId);

    Task<List<Proveedor>> ListarPorNegocioAsync(int negocioId);
    Task<List<Proveedor>> BuscarPorNombreAsync(int negocioId, string termino, int limite);
    Task<Proveedor?> ObtenerPorIdAsync(int proveedorId);
    Task<Proveedor> CrearAsync(Proveedor proveedor);

    Task<List<InsumoProveedor>> ListarPorInsumoAsync(int insumoId);
    Task<InsumoProveedor?> ObtenerAsociacionAsync(int insumoId, int proveedorId);
    Task<bool> ExisteAsociacionAsync(int insumoId, int proveedorId);
    Task<decimal?> ObtenerCantidadInsumoAsync(int insumoId);
    Task<InsumoProveedor> AsociarAsync(int insumoId, int proveedorId, decimal costo, decimal cantidad, string unidadMedida);
    Task<bool> DesasociarAsync(int insumoId, int proveedorId);
    Task<bool> MarcarPrincipalAsync(int insumoId, int proveedorId);
    Task<bool> ActualizarCostoAsync(int insumoId, int proveedorId, decimal nuevoCosto, decimal? cantidad = null, string? unidadMedida = null);

    Task<Proveedor?> ObtenerPorIdDelNegocioAsync(int negocioId, int proveedorId);
    Task<int> ContarAsociacionesAsync(int proveedorId);
    Task<int> PropagarNombreAInsumosAsync(int negocioId, int proveedorId, string nombreNuevo);
    Task EliminarAsociacionesDeProveedorAsync(int proveedorId);
    Task<List<string>> ObtenerNombresInsumosAsociadosAsync(int proveedorId);
    Task ActualizarAsync(Proveedor proveedor);
    void Eliminar(Proveedor proveedor);
    Task GuardarCambiosAsync();
    Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null);
    Task SincronizarInsumoConPrincipalAsync(int insumoId);
}