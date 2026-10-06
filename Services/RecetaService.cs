using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public interface IRecetaService
{
    Task<List<RecetaDto>?> ListarAsync(int cuentaId, string? busqueda);
    Task<ResultadoRecetaDto> ObtenerPorIdAsync(int cuentaId, int recetaId);
    Task<ResultadoRecetaDto> CrearAsync(int cuentaId, GuardarRecetaDto dto);
    Task<ResultadoRecetaDto> ActualizarAsync(int cuentaId, int recetaId, GuardarRecetaDto dto);
    Task<ResultadoEliminarRecetaDto> EliminarAsync(int cuentaId, int recetaId);
}

public sealed class RecetaService : IRecetaService
{
    // Mismas unidades que CK_Insumo_UnidadMedida.
    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "l", "ml", "oz", "lb", "unidad", "taza"
    };

    // Familias: una receta solo puede usar unidades de la misma familia que el insumo.
    private static readonly Dictionary<string, string> FamiliaPorUnidad = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = "masa",
        ["kg"] = "masa",
        ["oz"] = "masa",
        ["lb"] = "masa",
        ["ml"] = "volumen",
        ["l"] = "volumen",
        ["taza"] = "volumen",
        ["unidad"] = "conteo"
    };

    private const string SinNegocio = "Aún no tienes un negocio registrado para administrar recetas.";
    private const string NoExiste = "No se encontró la receta solicitada.";
    private const string NombreDuplicado = "Ya existe una receta con ese nombre en tu negocio.";

    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;

    public RecetaService(IRecetaRepository recetaRepository, IInsumoRepository insumoRepository)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
    }

    public async Task<List<RecetaDto>?> ListarAsync(int cuentaId, string? busqueda)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return null;

        var recetas = await _recetaRepository.ListarAsync(negocioId.Value, busqueda);
        return recetas.Select(Mapear).ToList();
    }

    public async Task<ResultadoRecetaDto> ObtenerPorIdAsync(int cuentaId, int recetaId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoRecetaDto.NoHallado(SinNegocio);

        var receta = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, recetaId);
        return receta is null
            ? ResultadoRecetaDto.NoHallado(NoExiste)
            : ResultadoRecetaDto.Ok(Mapear(receta), "Detalle de la receta.");
    }

    public async Task<ResultadoRecetaDto> CrearAsync(int cuentaId, GuardarRecetaDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoRecetaDto.NoHallado(SinNegocio);

        var nombre = dto.Nombre.Trim();
        var (error, ingredientes) = await ValidarAsync(negocioId.Value, nombre, dto, null);
        if (error is not null) return ResultadoRecetaDto.Error(error);

        try
        {
            var receta = await _recetaRepository.CrearAsync(new Receta
            {
                NegocioId = negocioId.Value,
                Nombre = nombre,
                PorcionesBase = dto.PorcionesBase,
                Instrucciones = NormalizarTexto(dto.Instrucciones),
                FechaCreacion = DateTime.UtcNow,
                RecetaInsumos = ingredientes!
            });

            var creada = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, receta.Id);
            return ResultadoRecetaDto.Ok(Mapear(creada!), "La receta fue creada correctamente.");
        }
        catch (DbUpdateException)
        {
            return ResultadoRecetaDto.Error(NombreDuplicado);
        }
    }

    public async Task<ResultadoRecetaDto> ActualizarAsync(int cuentaId, int recetaId, GuardarRecetaDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoRecetaDto.NoHallado(SinNegocio);

        var receta = await _recetaRepository.ObtenerParaEditarAsync(negocioId.Value, recetaId);
        if (receta is null) return ResultadoRecetaDto.NoHallado(NoExiste);

        var nombre = dto.Nombre.Trim();
        var (error, ingredientes) = await ValidarAsync(negocioId.Value, nombre, dto, receta.Id);
        if (error is not null) return ResultadoRecetaDto.Error(error);

        receta.Nombre = nombre;
        receta.PorcionesBase = dto.PorcionesBase;
        receta.Instrucciones = NormalizarTexto(dto.Instrucciones);

        // Reemplazo completo de ingredientes.
        _recetaRepository.ReemplazarInsumos(receta, ingredientes!);

        try
        {
            await _recetaRepository.GuardarCambiosAsync();
        }
        catch (DbUpdateException)
        {
            return ResultadoRecetaDto.Error(NombreDuplicado);
        }

        var actualizada = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, receta.Id);
        return ResultadoRecetaDto.Ok(Mapear(actualizada!), "La receta fue actualizada correctamente.");
    }

    public async Task<ResultadoEliminarRecetaDto> EliminarAsync(int cuentaId, int recetaId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return new ResultadoEliminarRecetaDto { NoEncontrado = true, Mensaje = SinNegocio };
        }

        var receta = await _recetaRepository.ObtenerParaEditarAsync(negocioId.Value, recetaId);
        if (receta is null)
        {
            return new ResultadoEliminarRecetaDto { NoEncontrado = true, Mensaje = NoExiste };
        }

        var productos = await _recetaRepository.ContarProductosMenuAsync(receta.Id);
        if (productos > 0)
        {
            return new ResultadoEliminarRecetaDto
            {
                BloqueadaPorMenu = true,
                NumeroProductos = productos,
                Mensaje = $"No se puede eliminar la receta porque está asociada a {productos} producto(s) del menú."
            };
        }

        _recetaRepository.Eliminar(receta);
        await _recetaRepository.GuardarCambiosAsync();

        return new ResultadoEliminarRecetaDto
        {
            Exito = true,
            Mensaje = "La receta fue eliminada correctamente."
        };
    }

    // ---------- Validación / estandarización ----------

    // Devuelve (error, ingredientes normalizados). error null => válido.
    private async Task<(string? Error, List<RecetaInsumo>? Ingredientes)> ValidarAsync(
        int negocioId, string nombre, GuardarRecetaDto dto, int? recetaId)
    {
        if (nombre.Length == 0) return ("El nombre de la receta no puede estar vacío.", null);
        if (nombre.Length > 150) return ("El nombre no puede superar los 150 caracteres.", null);

        if (dto.PorcionesBase < 1) return ("Las porciones base deben ser al menos 1.", null);

        if (dto.Insumos.Count == 0) return ("La receta necesita al menos un insumo.", null);

        var ids = dto.Insumos.Select(i => i.InsumoId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            return ("Hay insumos repetidos en la receta.", null);

        if (await _recetaRepository.ExisteNombreAsync(negocioId, nombre, recetaId))
            return (NombreDuplicado, null);

        var insumos = (await _recetaRepository.ObtenerInsumosActivosAsync(negocioId, ids))
            .ToDictionary(i => i.Id);
        if (insumos.Count != ids.Count)
            return ("Alguno de los insumos no existe en tu negocio o está inactivo.", null);

        var resultado = new List<RecetaInsumo>();
        foreach (var item in dto.Insumos)
        {
            if (item.Cantidad <= 0)
                return ("La cantidad de cada insumo debe ser mayor a 0.", null);

            var unidad = item.UnidadMedida?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(unidad) || !UnidadesValidas.Contains(unidad))
            {
                return ($"La unidad de medida no es válida. Valores permitidos: {string.Join(", ", UnidadesValidas.OrderBy(u => u))}.", null);
            }

            var insumo = insumos[item.InsumoId];
            if (FamiliaPorUnidad[unidad] != FamiliaPorUnidad[insumo.UnidadMedida])
            {
                return ($"La unidad '{unidad}' no es compatible con el insumo '{insumo.Nombre}', que se mide en '{insumo.UnidadMedida}'.", null);
            }

            resultado.Add(new RecetaInsumo
            {
                InsumoId = item.InsumoId,
                Cantidad = item.Cantidad,
                UnidadMedida = unidad
            });
        }

        return (null, resultado);
    }

    private static string? NormalizarTexto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static RecetaDto Mapear(Receta r) => new()
    {
        Id = r.Id,
        Nombre = r.Nombre,
        PorcionesBase = r.PorcionesBase,
        Instrucciones = r.Instrucciones,
        FechaCreacion = r.FechaCreacion,
        Insumos = r.RecetaInsumos.Select(ri => new RecetaInsumoDto
        {
            InsumoId = ri.InsumoId,
            InsumoNombre = ri.Insumo.Nombre,
            Cantidad = ri.Cantidad,
            UnidadMedida = ri.UnidadMedida
        }).ToList()
    };
}