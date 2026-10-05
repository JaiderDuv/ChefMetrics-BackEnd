using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface IInsumoService
{
    Task<ResultadoInsumoDto> CrearAsync(int cuentaId, CrearInsumoDto dto);

    // Null => la cuenta no tiene negocio (el controlador responde 404).
    Task<ListaPaginadaDto<InsumoDto>?> ListarAsync(int cuentaId, FiltrosInsumoDto filtros);

    Task<ResultadoInsumoDto> ObtenerPorIdAsync(int cuentaId, int insumoId);

    Task<ResultadoInsumoDto> ActualizarParcialAsync(int cuentaId, int insumoId, ActualizarInsumoDto dto);

    Task<ResultadoEliminarInsumoDto> EliminarAsync(int cuentaId, int insumoId);
}
