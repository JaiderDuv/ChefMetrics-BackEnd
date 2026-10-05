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

    Task ActualizarAsync(Insumo insumo);

    void Eliminar(Insumo insumo);

    Task GuardarCambiosAsync();
}
