using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public static class CosteoServiceExtensions
{
    public static IServiceCollection AddCosteo(this IServiceCollection services)
    {
        services.AddScoped<ICosteoRepository, CosteoRepository>();
        services.AddScoped<ICostoRecetaService, CostoRecetaService>();
        services.AddScoped<ICostosFijosService, CostosFijosService>();
        services.AddScoped<ISugerirPrecioService, SugerirPrecioService>();
        return services;
    }
}