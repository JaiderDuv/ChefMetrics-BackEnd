using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public sealed class UmbralMargenRepository : IUmbralMargenRepository
{
    private readonly ChefMetricsContext _db;
    public UmbralMargenRepository(ChefMetricsContext db) => _db = db;

    public Task<decimal?> ObtenerAsync(int negocioId) =>
        _db.Negocios.AsNoTracking()
            .Where(n => n.Id == negocioId)
            .Select(n => (decimal?)n.UmbralMargenMinimo)
            .FirstOrDefaultAsync();

    public async Task<bool> ActualizarAsync(int negocioId, decimal umbral)
    {
        var filas = await _db.Negocios
            .Where(n => n.Id == negocioId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.UmbralMargenMinimo, umbral));
        return filas > 0;
    }
}