using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

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

    // Valores permitidos por CK_CostoFijo_UnidadProrrateo en la BD.
    // 'horas_produccion' se retiró de las opciones: el sistema no registra la
    // duración de cada receta, así que no puede prorratear por hora real de producción
    // (cobraría el mismo $/hora a una receta de 5 min que a una de 3 h).
    // Los costos fijos existentes con esa unidad se conservan ("valores heredados"):
    // siguen calculándose igual (monto ÷ divisor) y pueden seguir editándose,
    // pero no pueden cambiarse a otro valor inválido ni usarse en registros nuevos.
    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "recetas_activas", "unidades_producidas"
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

        var categoria = NormalizarCategoria(dto.Categoria);
        if (categoria is null)
            return ResultadoCosteo<CostoFijoDto>.Error("La categoría debe tener entre 2 y 50 caracteres.");

        var unidad = NormalizarUnidad(dto.UnidadProrrateo);
        if (unidad is null) return ResultadoCosteo<CostoFijoDto>.Error(MensajeUnidad());

        if (decimal.Round(dto.MontoMensual, 2) != dto.MontoMensual)
            return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");
        if (!dto.ValorDivisor.HasValue)
            return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor es obligatorio.");
        if (dto.ValorDivisor.Value < 1m)
            return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor debe ser al menos 1 preparación al mes.");
        if (decimal.Round(dto.ValorDivisor.Value, 2) != dto.ValorDivisor.Value)
            return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor admite máximo 2 decimales.");

        if (await _repo.ExisteNombreCostoFijoAsync(negocioId.Value, nombre))
            return ResultadoCosteo<CostoFijoDto>.Error(Duplicado);

        var creado = await _repo.CrearCostoFijoAsync(new CostoFijo
        {
            NegocioId = negocioId.Value,
            Nombre = nombre,
            Categoria = categoria,
            MontoMensual = dto.MontoMensual,
            UnidadProrrateo = unidad,
            ValorDivisor = dto.ValorDivisor.Value,
            Activo = dto.Activo ?? true
        });
        return ResultadoCosteo<CostoFijoDto>.Ok(Mapear(creado),
            "El costo fijo fue creado correctamente." + AdvertenciaDivisor(creado.ValorDivisor));
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
            if (decimal.Round(dto.MontoMensual.Value, 2) != dto.MontoMensual.Value)
                return ResultadoCosteo<CostoFijoDto>.Error("El monto mensual admite máximo 2 decimales.");
            c.MontoMensual = dto.MontoMensual.Value; hayCambios = true;
        }
        if (dto.UnidadProrrateo is not null)
        {
            var unidad = NormalizarUnidad(dto.UnidadProrrateo);
            if (unidad is null)
            {
                // Valor heredado ('horas_produccion'): ya no se ofrece, pero un costo
                // fijo existente puede conservarlo. Solo se rechaza si intenta cambiarse
                // por un valor distinto e inválido.
                var entrante = dto.UnidadProrrateo.Trim().ToLowerInvariant();
                if (!string.Equals(c.UnidadProrrateo, entrante, StringComparison.OrdinalIgnoreCase))
                    return ResultadoCosteo<CostoFijoDto>.Error(MensajeUnidad());
                // Mismo valor heredado: no hay cambio que aplicar en la unidad.
            }
            else
            {
                c.UnidadProrrateo = unidad; hayCambios = true;
            }
        }
        if (dto.ValorDivisor.HasValue)
        {
            if (dto.ValorDivisor.Value < 1m)
                return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor debe ser al menos 1 preparación al mes.");
            if (decimal.Round(dto.ValorDivisor.Value, 2) != dto.ValorDivisor.Value)
                return ResultadoCosteo<CostoFijoDto>.Error("El valor divisor admite máximo 2 decimales.");
            c.ValorDivisor = dto.ValorDivisor.Value; hayCambios = true;
        }
        if (dto.Activo.HasValue) { c.Activo = dto.Activo.Value; hayCambios = true; }

        if (!hayCambios) return ResultadoCosteo<CostoFijoDto>.Error("Debes enviar al menos un campo para actualizar.");

        await _repo.GuardarCambiosAsync();
        return ResultadoCosteo<CostoFijoDto>.Ok(Mapear(c),
            "El costo fijo fue actualizado correctamente." + AdvertenciaDivisor(c.ValorDivisor));
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

    // El prorrateo es muy sensible con divisores pequeños: con 1, todo el
    // costo mensual se carga a una sola preparación. No se bloquea (puede ser
    // un negocio pequeño real), pero el mensaje lo advierte explícitamente.
    private static string AdvertenciaDivisor(decimal divisor) =>
        divisor < 10m
            ? " Advertencia: con un divisor menor a 10, la cuota prorrateada por preparación puede quedar muy alta."
            : string.Empty;

    private static string? NormalizarUnidad(string? valor)
    {
        var u = valor?.Trim().ToLowerInvariant();
        return u is not null && UnidadesValidas.Contains(u) ? u : null;
    }

    // Categoría libre: se normaliza y se limita a 50 caracteres. Si llega vacía,
    // se usa "Otros" para no romper clientes antiguos que aún no la envían.
    private static string? NormalizarCategoria(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "Otros";
        var categoria = valor.Trim();
        return categoria.Length is >= 2 and <= 50 ? categoria : null;
    }

    private static string MensajeUnidad() =>
        $"La unidad de prorrateo no es válida. Valores permitidos: {string.Join(", ", UnidadesValidas.OrderBy(u => u))}.";

    private static CostoFijoDto Mapear(CostoFijo c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Categoria = c.Categoria,
        MontoMensual = c.MontoMensual,
        UnidadProrrateo = c.UnidadProrrateo,
        ValorDivisor = c.ValorDivisor,
        Activo = c.Activo
    };
}