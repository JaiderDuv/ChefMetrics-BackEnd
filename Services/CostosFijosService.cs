using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public interface ICostosFijosService
{
    Task<List<CostoFijoDto>?> ListarAsync(int cuentaId, bool incluirInactivos);
    Task<ResultadoCosteo<ConfiguracionCostosFijosDto>> ObtenerConfiguracionAsync(int cuentaId, bool incluirInactivos);
    Task<ResultadoCosteo<ConfiguracionCostosFijosDto>> ActualizarPreparacionesBaseMesAsync(int cuentaId, ActualizarPreparacionesBaseMesDto dto);
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
    private const int MaxPreparacionesBaseMes = 1_000_000;

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

    public async Task<ResultadoCosteo<ConfiguracionCostosFijosDto>> ObtenerConfiguracionAsync(
        int cuentaId, bool incluirInactivos)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<ConfiguracionCostosFijosDto>.NoHallado(SinNegocio);

        var configuracion = await ConstruirConfiguracionAsync(negocioId.Value, incluirInactivos);
        return ResultadoCosteo<ConfiguracionCostosFijosDto>.Ok(configuracion);
    }

    public async Task<ResultadoCosteo<ConfiguracionCostosFijosDto>> ActualizarPreparacionesBaseMesAsync(
        int cuentaId, ActualizarPreparacionesBaseMesDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return ResultadoCosteo<ConfiguracionCostosFijosDto>.NoHallado(SinNegocio);

        if (dto.PreparacionesBaseMes is < 1 or > MaxPreparacionesBaseMes)
            return ResultadoCosteo<ConfiguracionCostosFijosDto>.Error(
                "Las preparaciones base al mes deben estar entre 1 y 1.000.000.");

        var actualizado = await _repo.ActualizarPreparacionesBaseMesAsync(
            negocioId.Value, dto.PreparacionesBaseMes);
        if (!actualizado)
            return ResultadoCosteo<ConfiguracionCostosFijosDto>.NoHallado(SinNegocio);

        var configuracion = await ConstruirConfiguracionAsync(negocioId.Value, incluirInactivos: true);
        return ResultadoCosteo<ConfiguracionCostosFijosDto>.Ok(
            configuracion, "Preparaciones base al mes actualizadas correctamente.");
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

        var categoria = NormalizarCategoria(dto.Categoria);
        if (categoria is null)
            return ResultadoCosteo<CostoFijoDto>.Error("La categoría debe tener entre 2 y 50 caracteres.");

        if (dto.MontoMensual <= 0)
            return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual debe ser mayor a 0.");
        if (decimal.Round(dto.MontoMensual, 2) != dto.MontoMensual)
            return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");

        if (await _repo.ExisteNombreCostoFijoAsync(negocioId.Value, nombre))
            return ResultadoCosteo<CostoFijoDto>.Error(Duplicado);

        var creado = await _repo.CrearCostoFijoAsync(new CostoFijo
        {
            NegocioId = negocioId.Value,
            Nombre = nombre,
            Categoria = categoria,
            MontoMensual = dto.MontoMensual,
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
        if (dto.Categoria is not null)
        {
            var categoria = NormalizarCategoria(dto.Categoria);
            if (categoria is null)
                return ResultadoCosteo<CostoFijoDto>.Error("La categoría debe tener entre 2 y 50 caracteres.");
            c.Categoria = categoria; hayCambios = true;
        }
        if (dto.MontoMensual.HasValue)
        {
            if (dto.MontoMensual.Value <= 0)
                return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual debe ser mayor a 0.");
            if (decimal.Round(dto.MontoMensual.Value, 2) != dto.MontoMensual.Value)
                return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");
            c.MontoMensual = dto.MontoMensual.Value; hayCambios = true;
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

    private async Task<ConfiguracionCostosFijosDto> ConstruirConfiguracionAsync(
        int negocioId, bool incluirInactivos)
    {
        var preparacionesBaseMes = await _repo.ObtenerPreparacionesBaseMesAsync(negocioId);
        var costos = await _repo.ListarCostosFijosAsync(negocioId, soloActivos: !incluirInactivos);
        return new ConfiguracionCostosFijosDto
        {
            PreparacionesBaseMes = preparacionesBaseMes,
            Costos = costos.Select(Mapear).ToList()
        };
    }

    // Categoría libre: se normaliza y se limita a 50 caracteres. Si llega vacía,
    // se usa "Otros" para no romper clientes antiguos que aún no la envían.
    private static string? NormalizarCategoria(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "Otros";
        var categoria = valor.Trim();
        return categoria.Length is >= 2 and <= 50 ? categoria : null;
    }

    private static CostoFijoDto Mapear(CostoFijo c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Categoria = c.Categoria,
        MontoMensual = c.MontoMensual,
        Activo = c.Activo
    };
}
