using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Services;

public interface IProveedorService
{
    Task<ResultadoProveedorDto> ListarProveedoresDelNegocioAsync(int cuentaId);
    Task<ResultadoProveedorDto> BuscarProveedoresAsync(int cuentaId, string termino, int limite);
    Task<ResultadoProveedorDto> CrearProveedorAsync(int cuentaId, CrearProveedorDto dto);

    Task<ResultadoProveedorDto> ListarPorInsumoAsync(int cuentaId, int insumoId);
    Task<ResultadoProveedorDto> ListarDisponiblesParaInsumoAsync(int cuentaId, int insumoId);

    Task<ResultadoProveedorDto> AsociarAsync(int cuentaId, int insumoId, AsociarProveedorDto dto);
    Task<ResultadoProveedorDto> CrearYAsociarAsync(int cuentaId, int insumoId, CrearYAsociarProveedorDto dto);

    Task<ResultadoProveedorDto> DesasociarAsync(int cuentaId, int insumoId, int proveedorId);
    Task<ResultadoProveedorDto> MarcarPrincipalAsync(int cuentaId, int insumoId, int proveedorId);
    Task<ResultadoProveedorDto> ActualizarCostoAsync(int cuentaId, int insumoId, int proveedorId, decimal costo, decimal? cantidad = null);
    Task<ResultadoProveedorDto> ActualizarProveedorAsync(int cuentaId, int proveedorId, ActualizarProveedorDto dto);
    Task<ResultadoProveedorDto> EliminarProveedorAsync(int cuentaId, int proveedorId);
}