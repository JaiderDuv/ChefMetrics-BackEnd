using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public sealed class EvaluacionMenuService : IEvaluacionMenuService
{
    private const string SinNegocio = "Aún no tienes un negocio registrado para evaluar el menú.";

    private readonly IUmbralMargenRepository _umbralRepo;
    private readonly IInsumoRepository _insumos; // ya resuelve negocioId por cuentaId en otras HU
    private readonly IProductoMenuService _productos;

    public EvaluacionMenuService(
        IUmbralMargenRepository umbralRepo,
        IInsumoRepository insumos,
        IProductoMenuService productos)
    {
        _umbralRepo = umbralRepo;
        _insumos = insumos;
        _productos = productos;
    }

    public async Task<ResultadoEvaluacionDto> ObtenerUmbralAsync(int cuentaId)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoEvaluacionDto.NoHallado(SinNegocio);

        var umbral = await _umbralRepo.ObtenerAsync(negocioId.Value) ?? 20m;
        return ResultadoEvaluacionDto.Ok(new UmbralMargenDto { UmbralMargenMinimo = umbral });
    }

    public async Task<ResultadoEvaluacionDto> ActualizarUmbralAsync(int cuentaId, ActualizarUmbralDto dto)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoEvaluacionDto.NoHallado(SinNegocio);

        if (dto.UmbralMargenMinimo < 0 || dto.UmbralMargenMinimo > 100)
            return ResultadoEvaluacionDto.Error("El umbral debe estar entre 0 y 100 %.");

        var actualizado = await _umbralRepo.ActualizarAsync(negocioId.Value, dto.UmbralMargenMinimo);
        if (!actualizado) return ResultadoEvaluacionDto.NoHallado(SinNegocio);

        return ResultadoEvaluacionDto.Ok(
            new UmbralMargenDto { UmbralMargenMinimo = dto.UmbralMargenMinimo },
            "Umbral actualizado correctamente.");
    }

    public async Task<ResultadoEvaluacionDto> EvaluarAsync(int cuentaId)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoEvaluacionDto.NoHallado(SinNegocio);

        // Mismo umbral por defecto que ObtenerUmbralAsync y Negocio.UmbralMargenMinimo.
        var umbral = await _umbralRepo.ObtenerAsync(negocioId.Value) ?? 20m;

        // Reutiliza el costeo ya calculado en HU10.1; solo evalúa el menú vigente.
        var productos = await _productos.ListarAsync(cuentaId, busqueda: null, incluirInactivos: false);
        productos ??= new();

        var evaluados = productos.Select(p =>
        {
            var sinPrecio = !p.PrecioVenta.HasValue;
            var noCalculable = !p.MargenDirecto.HasValue;

            string? motivo = null;
            if (sinPrecio) motivo = "El producto no tiene precio de venta definido.";
            else if (noCalculable) motivo = "El costo directo del producto no está disponible todavía.";

            // La evaluación se basa en el margen directo. Los costos fijos se
            // cubren con el punto de equilibrio y no se cargan a cada producto,
            // así que no deben convertir un producto rentable en "en riesgo".
            var enRiesgo = p.MargenDirecto.HasValue && p.MargenDirecto.Value < umbral;

            return new ProductoEvaluacionDto
            {
                ProductoId = p.Id,
                Nombre = p.Nombre,
                CostoDirecto = p.CostoDirecto,
                CostoTotal = p.CostoTotal,
                PrecioVenta = p.PrecioVenta,
                MargenDirecto = p.MargenDirecto,
                MargenReal = p.MargenReal,
                MargenNoCalculable = noCalculable,
                MotivoNoCalculable = motivo,
                EnRiesgo = enRiesgo
            };
        });

        // Primero el margen directo más bajo, para que lo urgente aparezca
        // arriba; sin margen calculable, al final.
        var ordenados = evaluados
            .OrderBy(p => p.MargenNoCalculable)
            .ThenBy(p => p.MargenDirecto ?? decimal.MaxValue)
            .ToList();

        return ResultadoEvaluacionDto.Ok(new EvaluacionMenuDto
        {
            UmbralMargenMinimo = umbral,
            Productos = ordenados
        });
    }
}