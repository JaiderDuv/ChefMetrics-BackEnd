namespace ChefMetrics_BackEnd.Costeo;

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