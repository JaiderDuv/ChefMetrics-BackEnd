using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChefMetrics_BackEnd.Health;

// Comprueba que la API pueda hablar con SQL Server sin exponer detalles
// internos: el endpoint /health solo devuelve Healthy/Unhealthy.
public sealed class DbHealthCheck : IHealthCheck
{
    private readonly ChefMetricsContext _db;

    public DbHealthCheck(ChefMetricsContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var disponible = await _db.Database.CanConnectAsync(cancellationToken);
            return disponible
                ? HealthCheckResult.Healthy("Base de datos disponible.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error al verificar la base de datos.", ex);
        }
    }
}
