using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Services;

public interface IEvaluacionMenuService
{
    Task<ResultadoEvaluacionDto> ObtenerUmbralAsync(int cuentaId);
    Task<ResultadoEvaluacionDto> ActualizarUmbralAsync(int cuentaId, ActualizarUmbralDto dto);
    Task<ResultadoEvaluacionDto> EvaluarAsync(int cuentaId);
}