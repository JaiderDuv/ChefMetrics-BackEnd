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
        services.AddScoped<IProductoMenuRepository, ProductoMenuRepository>();
        services.AddScoped<ICostoProductoService, CostoProductoService>();
        services.AddScoped<IProductoMenuService, ProductoMenuService>();
        services.AddScoped<ISugerirPrecioProductoService, SugerirPrecioProductoService>();
        services.AddScoped<IPuntoEquilibrioService, PuntoEquilibrioService>();
        services.AddScoped<ISimuladorVentasService, SimuladorVentasService>();
        return services;
    }
}