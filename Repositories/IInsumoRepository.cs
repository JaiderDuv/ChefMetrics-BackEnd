using ChefMetrics_BackEnd.Model;

namespace ChefMetrics_BackEnd.Repositories;

public interface IInsumoRepository
{
    // Resuelve el negocio del usuario autenticado. Null si no tiene negocio.
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

    // Cantidad de filas en RecetaInsumo que usan el insumo.
    Task<int> ContarRecetasAsociadasAsync(int insumoId);

    Task<Dictionary<int, int>> ContarRecetasPorInsumosAsync(IEnumerable<int> insumoIds);

    Task<Insumo> CrearAsync(Insumo insumo);

    // Insumos del negocio para el historial global de precios (filtros opcionales).
    Task<List<Insumo>> ListarParaHistorialAsync(int negocioId, string? nombre, string? proveedor);

    // Busca el proveedor por nombre dentro del negocio (comparación exacta
    // sin distinción de mayúsculas) o lo crea si no existe.
    Task<Proveedor> ObtenerOCrearProveedorAsync(int negocioId, string nombre);

    // Fila de asociación insumo-proveedor (null si no existe).
    Task<InsumoProveedor?> ObtenerInsumoProveedorAsync(int insumoId, int proveedorId);

    // Fila principal del insumo (null si aún no tiene proveedor).
    Task<InsumoProveedor?> ObtenerInsumoProveedorPrincipalAsync(int insumoId);

    Task<InsumoProveedor> CrearInsumoProveedorAsync(InsumoProveedor asociacion);

    // Quita la marca de principal a las demás filas del insumo.
    Task DesmarcarPrincipalesAsync(int insumoId, int exceptoId);

    Task<HistorialPrecioInsumo> RegistrarCambioPrecioAsync(HistorialPrecioInsumo cambio);

    // Borra historial y asociaciones de proveedores del insumo (sin guardar).
    // Se usa al eliminar el insumo, tras verificar que no está en recetas.
    Task EliminarPreciosAsync(int insumoId);

    // Historial real de un insumo (joins con proveedor), reciente primero.
    Task<List<HistorialPrecioInsumo>> ListarHistorialPorInsumoAsync(int insumoId);

    // Historial real global del negocio, reciente primero (filtros opcionales).
    Task<List<HistorialPrecioInsumo>> ListarHistorialGeneralAsync(
        int negocioId, string? nombre, string? proveedor);

    Task ActualizarAsync(Insumo insumo);

    void Eliminar(Insumo insumo);

    Task GuardarCambiosAsync();
}
