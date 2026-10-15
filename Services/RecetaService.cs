using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public interface IRecetaService
{
    Task<List<RecetaDto>?> ListarAsync(int cuentaId, string? busqueda);
    Task<ResultadoRecetaDto> ObtenerPorIdAsync(int cuentaId, int recetaId);
    Task<ResultadoRecetaDto> CrearAsync(int cuentaId, GuardarRecetaDto dto);
    Task<ResultadoRecetaDto> ActualizarAsync(int cuentaId, int recetaId, GuardarRecetaDto dto);
    Task<ResultadoEliminarRecetaDto> EliminarAsync(int cuentaId, int recetaId);
    Task<ResultadoRecetaDto> GuardarImagenAsync(int cuentaId, int recetaId, IFormFile archivo);
    Task<ResultadoRecetaDto> QuitarImagenAsync(int cuentaId, int recetaId);
}

public sealed class RecetaService : IRecetaService
{
    private const string SinNegocio = "Aún no tienes un negocio registrado para administrar recetas.";
    private const string NoExiste = "No se encontró la receta solicitada.";
    private const string NombreDuplicado = "Ya existe una receta con ese nombre en tu negocio.";
    private const long MaxImagenBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> ExtensionesImagen = new() { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> TiposImagen = new() { "image/jpeg", "image/png", "image/webp" };

    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IWebHostEnvironment _env;

    public RecetaService(IRecetaRepository recetaRepository, IInsumoRepository insumoRepository,
                         IWebHostEnvironment env)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _env = env;
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
        _recetaRepository.ReemplazarInsumos(receta, ingredientes!);

        try { await _recetaRepository.GuardarCambiosAsync(); }
        catch (DbUpdateException) { return ResultadoRecetaDto.Error(NombreDuplicado); }

        var actualizada = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, receta.Id);
        return ResultadoRecetaDto.Ok(Mapear(actualizada!), "La receta fue actualizada correctamente.");
    }

    public async Task<ResultadoEliminarRecetaDto> EliminarAsync(int cuentaId, int recetaId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
            return new ResultadoEliminarRecetaDto { NoEncontrado = true, Mensaje = SinNegocio };

        var receta = await _recetaRepository.ObtenerParaEditarAsync(negocioId.Value, recetaId);
        if (receta is null)
            return new ResultadoEliminarRecetaDto { NoEncontrado = true, Mensaje = NoExiste };

        var productos = await _recetaRepository.ContarProductosMenuAsync(receta.Id);
        if (productos > 0)
            return new ResultadoEliminarRecetaDto
            {
                BloqueadaPorMenu = true,
                NumeroProductos = productos,
                Mensaje = $"No se puede eliminar la receta porque está asociada a {productos} producto(s) del menú."
            };

        var imagen = receta.ImagenUrl;
        _recetaRepository.Eliminar(receta);
        await _recetaRepository.GuardarCambiosAsync();
        BorrarArchivo(imagen);

        return new ResultadoEliminarRecetaDto { Exito = true, Mensaje = "La receta fue eliminada correctamente." };
    }

    // ---------- Imágenes ----------

    public async Task<ResultadoRecetaDto> GuardarImagenAsync(int cuentaId, int recetaId, IFormFile archivo)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoRecetaDto.NoHallado(SinNegocio);

        var receta = await _recetaRepository.ObtenerParaEditarAsync(negocioId.Value, recetaId);
        if (receta is null) return ResultadoRecetaDto.NoHallado(NoExiste);

        if (archivo is null || archivo.Length == 0)
            return ResultadoRecetaDto.Error("No se recibió ninguna imagen.");
        if (archivo.Length > MaxImagenBytes)
            return ResultadoRecetaDto.Error("La imagen no puede superar los 2 MB.");

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesImagen.Contains(ext) || !TiposImagen.Contains(archivo.ContentType.ToLowerInvariant()))
            return ResultadoRecetaDto.Error("Solo se permiten imágenes JPG, PNG o WEBP.");

        // Verifica los primeros bytes: evita que renombren un .exe como .jpg.
        var cabecera = new byte[12];
        await using (var lectura = archivo.OpenReadStream())
            await lectura.ReadExactlyAsync(cabecera, 0, (int)Math.Min(12, archivo.Length));
        if (!FirmaValida(cabecera))
            return ResultadoRecetaDto.Error("El archivo no es una imagen válida.");

        var carpeta = Path.Combine(RaizWeb(), "uploads", "recetas");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{Guid.NewGuid():N}{ext}";   // el nombre original nunca se usa
        await using (var destino = File.Create(Path.Combine(carpeta, nombreArchivo)))
            await archivo.CopyToAsync(destino);

        var anterior = receta.ImagenUrl;
        receta.ImagenUrl = $"/uploads/recetas/{nombreArchivo}";
        await _recetaRepository.GuardarCambiosAsync();
        BorrarArchivo(anterior);

        var actualizada = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, receta.Id);
        return ResultadoRecetaDto.Ok(Mapear(actualizada!), "La imagen fue guardada correctamente.");
    }

    public async Task<ResultadoRecetaDto> QuitarImagenAsync(int cuentaId, int recetaId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoRecetaDto.NoHallado(SinNegocio);

        var receta = await _recetaRepository.ObtenerParaEditarAsync(negocioId.Value, recetaId);
        if (receta is null) return ResultadoRecetaDto.NoHallado(NoExiste);

        var anterior = receta.ImagenUrl;
        receta.ImagenUrl = null;
        await _recetaRepository.GuardarCambiosAsync();
        BorrarArchivo(anterior);

        var actualizada = await _recetaRepository.ObtenerPorIdAsync(negocioId.Value, receta.Id);
        return ResultadoRecetaDto.Ok(Mapear(actualizada!), "La imagen fue eliminada.");
    }

    private string RaizWeb() => _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

    private void BorrarArchivo(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;
        var nombre = Path.GetFileName(rutaRelativa);   // GetFileName evita rutas maliciosas
        var completa = Path.Combine(RaizWeb(), "uploads", "recetas", nombre);
        try { if (File.Exists(completa)) File.Delete(completa); } catch { /* no bloquear por esto */ }
    }

    private static bool FirmaValida(byte[] b) =>
        (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) ||                                   // JPEG
        (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) ||                   // PNG
        (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
         b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50);                   // WEBP

    // ---------- Validación / estandarización ----------

    private async Task<(string? Error, List<RecetaInsumo>? Ingredientes)> ValidarAsync(
        int negocioId, string nombre, GuardarRecetaDto dto, int? recetaId)
    {
        if (nombre.Length < 3) return ("El nombre debe tener al menos 3 caracteres.", null);
        if (nombre.Length > 100) return ("El nombre no puede superar los 100 caracteres.", null);
        if (dto.Insumos.Count == 0) return ("La receta necesita al menos un insumo.", null);

        var ids = dto.Insumos.Select(i => i.InsumoId).ToList();
        if (ids.Distinct().Count() != ids.Count) return ("Hay insumos repetidos en la receta.", null);

        if (await _recetaRepository.ExisteNombreAsync(negocioId, nombre, recetaId))
            return (NombreDuplicado, null);

        var insumos = (await _recetaRepository.ObtenerInsumosActivosAsync(negocioId, ids))
            .ToDictionary(i => i.Id);
        if (insumos.Count != ids.Count)
            return ("Alguno de los insumos no existe en tu negocio o está inactivo.", null);

        var resultado = new List<RecetaInsumo>();
        foreach (var item in dto.Insumos)
        {
            if (item.Cantidad <= 0) return ("La cantidad de cada insumo debe ser mayor a 0.", null);
            if (decimal.Round(item.Cantidad, 3) != item.Cantidad)
                return ("La cantidad admite máximo 3 decimales.", null);

            var unidad = item.UnidadMedida?.Trim().ToLowerInvariant();
            if (!ConversorUnidades.EsValida(unidad)) return ("La unidad de medida no es válida.", null);

            var insumo = insumos[item.InsumoId];
            if (!ConversorUnidades.Compatibles(unidad!, insumo.UnidadMedida))
                return ($"La unidad '{unidad}' no es compatible con el insumo '{insumo.Nombre}', que se mide en '{insumo.UnidadMedida}'.", null);

            resultado.Add(new RecetaInsumo { InsumoId = item.InsumoId, Cantidad = item.Cantidad, UnidadMedida = unidad! });
        }
        return (null, resultado);
    }

    private static string? NormalizarTexto(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    // Insumo.CostoUnitario es el total del paquete (Insumo.Cantidad): se divide para el precio por unidad.
    private static decimal CostoPorUnidad(Insumo i) =>
        i.Cantidad.HasValue && i.Cantidad.Value > 0 ? i.CostoUnitario / i.Cantidad.Value : i.CostoUnitario;

    private static RecetaDto Mapear(Receta r)
    {
        var lineas = r.RecetaInsumos.Select(ri => new RecetaInsumoDto
        {
            InsumoId = ri.InsumoId,
            InsumoNombre = ri.Insumo.Nombre,
            Cantidad = ri.Cantidad,
            UnidadMedida = ri.UnidadMedida,
            CostoLinea = ConversorUnidades.CostoLinea(ri.Cantidad, ri.UnidadMedida,
                ri.Insumo.UnidadMedida, CostoPorUnidad(ri.Insumo), ri.Insumo.PorcentajeMerma)
        }).ToList();

        var total = decimal.Round(lineas.Sum(l => l.CostoLinea), 2);
        return new RecetaDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            PorcionesBase = r.PorcionesBase,
            Instrucciones = r.Instrucciones,
            ImagenUrl = r.ImagenUrl,
            FechaCreacion = r.FechaCreacion,
            Insumos = lineas,
            CostoTotal = total,
            CostoPorPorcion = r.PorcionesBase > 0 ? decimal.Round(total / r.PorcionesBase, 2) : 0
        };
    }
}