using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Costeo;

public interface ICostosFijosService
{
    Task<List<CostoFijoDto>?> ListarAsync(int cuentaId, bool incluirInactivos);
    Task<ResultadoCosteo<CostoFijoDto>> ObtenerAsync(int cuentaId, int id);
    Task<ResultadoCosteo<CostoFijoDto>> CrearAsync(int cuentaId, CrearCostoFijoDto dto);
    Task<ResultadoCosteo<CostoFijoDto>> ActualizarAsync(int cuentaId, int id, ActualizarCostoFijoDto dto);
    Task<ResultadoCosteo<bool>> EliminarAsync(int cuentaId, int id);
}

public sealed class CostosFijosService : ICostosFijosService
{
    private const string SinNegocio = "Aún no tienes un negocio registrado para administrar costos fijos.";
    private const string NoExiste = "No se encontró el costo fijo solicitado.";
    private const string Duplicado = "Ya existe un costo fijo con ese nombre en tu negocio.";

    // Valores permitidos por CK_CostoFijo_UnidadProrrateo.
    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "recetas_activas", "horas_produccion", "unidades_producidas"
    };

    private readonly ICosteoRepository _repo;
    private readonly IInsumoRepository _insumoRepository;

    public CostosFijosService(ICosteoRepository repo, IInsumoRepository insumoRepository)
    {
        _repo = repo;
        _insumoRepository = insumoRepository;
    }

    public async Task<List<CostoFijoDto>?> ListarAsync(int cuentaId, bool incluirInactivos)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return null;
        var lista = await _repo.ListarCostosFijosAsync(negocioId.Value, soloActivos: !incluirInactivos);
        return lista.Select(Mapear).ToList();
    }

    public async Task<ResultadoCosteo<CostoFijoDto>> ObtenerAsync(int cuentaId, int id)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoCosteo<CostoFijoDto>.NoHallado(SinNegocio);
        var c = await _repo.ObtenerCostoFijoAsync(negocioId.Value, id, false);
        return c is null ? ResultadoCosteo<CostoFijoDto>.NoHallado(NoExiste) : ResultadoCosteo<CostoFijoDto>.Ok(Mapear(c));
    }

    public async Task<ResultadoCosteo<CostoFijoDto>> CrearAsync(int cuentaId, CrearCostoFijoDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoCosteo<CostoFijoDto>.NoHallado(SinNegocio);

        var nombre = dto.Nombre.Trim();
        if (nombre.Length < 3) return ResultadoCosteo<CostoFijoDto>.Error("El nombre debe tener al menos 3 caracteres.");

        var unidad = NormalizarUnidad(dto.UnidadProrrateo);
        if (unidad is null) return ResultadoCosteo<CostoFijoDto>.Error(MensajeUnidad());

        if (decimal.Round(dto.MontoMensual, 2) != dto.MontoMensual)
            return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");
        if (decimal.Round(dto.ValorDivisor, 2) != dto.ValorDivisor)
            return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor admite máximo 2 decimales.");

        if (await _repo.ExisteNombreCostoFijoAsync(negocioId.Value, nombre))
            return ResultadoCosteo<CostoFijoDto>.Error(Duplicado);

        var creado = await _repo.CrearCostoFijoAsync(new CostoFijo
        {
            NegocioId = negocioId.Value,
            Nombre = nombre,
            MontoMensual = dto.MontoMensual,
            UnidadProrrateo = unidad,
            ValorDivisor = dto.ValorDivisor,
            Activo = dto.Activo ?? true
        });
        return ResultadoCosteo<CostoFijoDto>.Ok(Mapear(creado), "El costo fijo fue creado correctamente.");
    }

    public async Task<ResultadoCosteo<CostoFijoDto>> ActualizarAsync(int cuentaId, int id, ActualizarCostoFijoDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoCosteo<CostoFijoDto>.NoHallado(SinNegocio);

        var c = await _repo.ObtenerCostoFijoAsync(negocioId.Value, id, true);
        if (c is null) return ResultadoCosteo<CostoFijoDto>.NoHallado(NoExiste);

        var hayCambios = false;
        if (dto.Nombre is not null)
        {
            var nombre = dto.Nombre.Trim();
            if (nombre.Length < 3) return ResultadoCosteo<CostoFijoDto>.Error("El nombre debe tener al menos 3 caracteres.");
            if (!string.Equals(c.Nombre, nombre, StringComparison.Ordinal) &&
                await _repo.ExisteNombreCostoFijoAsync(negocioId.Value, nombre, c.Id))
                return ResultadoCosteo<CostoFijoDto>.Error(Duplicado);
            c.Nombre = nombre; hayCambios = true;
        }
        if (dto.MontoMensual.HasValue)
        {
            if (decimal.Round(dto.MontoMensual.Value, 2) != dto.MontoMensual.Value)
                return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");
            c.MontoMensual = dto.MontoMensual.Value; hayCambios = true;
        }
        if (dto.UnidadProrrateo is not null)
        {
            var unidad = NormalizarUnidad(dto.UnidadProrrateo);
            if (unidad is null) return ResultadoCosteo<CostoFijoDto>.Error(MensajeUnidad());
            c.UnidadProrrateo = unidad; hayCambios = true;
        }
        if (dto.ValorDivisor.HasValue)
        {
            if (decimal.Round(dto.ValorDivisor.Value, 2) != dto.ValorDivisor.Value)
                return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor admite máximo 2 decimales.");
            c.ValorDivisor = dto.ValorDivisor.Value; hayCambios = true;
        }
        if (dto.Activo.HasValue) { c.Activo = dto.Activo.Value; hayCambios = true; }

        if (!hayCambios) return ResultadoCosteo<CostoFijoDto>.Error("Debes enviar al menos un campo para actualizar.");

        await _repo.GuardarCambiosAsync();
        return ResultadoCosteo<CostoFijoDto>.Ok(Mapear(c), "El costo fijo fue actualizado correctamente.");
    }

    public async Task<ResultadoCosteo<bool>> EliminarAsync(int cuentaId, int id)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoCosteo<bool>.NoHallado(SinNegocio);

        var c = await _repo.ObtenerCostoFijoAsync(negocioId.Value, id, true);
        if (c is null) return ResultadoCosteo<bool>.NoHallado(NoExiste);

        _repo.EliminarCostoFijo(c);
        await _repo.GuardarCambiosAsync();
        return ResultadoCosteo<bool>.Ok(true, "El costo fijo fue eliminado correctamente.");
    }

    private static string? NormalizarUnidad(string? valor)
    {
        var u = valor?.Trim().ToLowerInvariant();
        return u is not null && UnidadesValidas.Contains(u) ? u : null;
    }

    private static string MensajeUnidad() =>
        $"La unidad de prorrateo no es válida. Valores permitidos: {string.Join(", ", UnidadesValidas.OrderBy(u => u))}.";

    private static CostoFijoDto Mapear(CostoFijo c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        MontoMensual = c.MontoMensual,
        UnidadProrrateo = c.UnidadProrrateo,
        ValorDivisor = c.ValorDivisor,
        Activo = c.Activo
    };
}