using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories;

public interface IInsumoRepository
{
   Task<int?> ObtenerNegocioIdPorCuentaAsync(int cuentaId);

    Task<Insumo?> ObtenerPorIdAsync(int negocioId, int insumoId);

    Task<(List<Insumo> Items, int Total)> ListarAsync(
        int negocioId,
        string? busqueda,
        string? categoria,
        bool incluirInactivos,
        string ordenarPor,
        bool descendente,
        int pagina,
        int tamanoPagina);

    Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null);
    Task<int> ContarRecetasAsociadasAsync(int insumoId);

    Task<Dictionary<int, int>> ContarRecetasPorInsumosAsync(IEnumerable<int> insumoIds);

    Task<Insumo> CrearAsync(Insumo insumo);
    Task<List<Insumo>> ListarParaHistorialAsync(int negocioId, string? nombre, string? proveedor);

    Task<Proveedor> ObtenerOCrearProveedorAsync(int negocioId, string nombre);

    Task<InsumoProveedor?> ObtenerInsumoProveedorAsync(int insumoId, int proveedorId);

    Task<InsumoProveedor?> ObtenerInsumoProveedorPrincipalAsync(int insumoId);
    
    Task<string?> ObtenerNombreProveedorPrincipalAsync(int insumoId);

    Task<Dictionary<int, string>> ObtenerNombresProveedoresPrincipalesAsync(IEnumerable<int> insumoIds);

    Task<InsumoProveedor> CrearInsumoProveedorAsync(InsumoProveedor asociacion);

    Task DesmarcarPrincipalesAsync(int insumoId, int exceptoId);

    Task<HistorialPrecioInsumo> RegistrarCambioPrecioAsync(HistorialPrecioInsumo cambio);

    Task EliminarPreciosAsync(int insumoId);

    Task<List<HistorialPrecioInsumo>> ListarHistorialPorInsumoAsync(int insumoId);

    Task<List<HistorialPrecioInsumo>> ListarHistorialGeneralAsync(
        int negocioId, string? nombre, string? proveedor);

    Task ActualizarAsync(Insumo insumo);

    void Eliminar(Insumo insumo);

    Task GuardarCambiosAsync();
}
