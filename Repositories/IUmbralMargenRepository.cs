namespace ChefMetrics_BackEnd.Repositories;

public interface IUmbralMargenRepository
{
    Task<decimal?> ObtenerAsync(int negocioId);
    Task<bool> ActualizarAsync(int negocioId, decimal umbral);
}