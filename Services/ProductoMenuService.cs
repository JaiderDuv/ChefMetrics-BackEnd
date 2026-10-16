using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public interface IProductoMenuService
{
    Task<List<ProductoMenuDto>?> ListarAsync(int cuentaId, string? busqueda, bool incluirInactivos);
    Task<ResultadoProductoMenuDto> ObtenerAsync(int cuentaId, int id);
    Task<ResultadoProductoMenuDto> CrearAsync(int cuentaId, CrearProductoMenuDto dto);
    Task<ResultadoProductoMenuDto> ActualizarAsync(int cuentaId, int id, ActualizarProductoMenuDto dto);
    Task<ResultadoProductoMenuDto> ActualizarParcialAsync(int cuentaId, int id, PatchProductoMenuDto dto);
    Task<ResultadoCosteo<bool>> EliminarAsync(int cuentaId, int id);
}

public sealed class ProductoMenuService : IProductoMenuService
{
    private const string SinNegocio = "Aún no tienes un negocio registrado para administrar el menú.";
    private const string NoExiste = "No se encontró el producto solicitado.";
    private const string Duplicado = "Ya existe un producto con ese nombre en tu negocio.";

    private readonly IProductoMenuRepository _repo;
    private readonly IInsumoRepository _insumos;
    private readonly ICostoProductoService _costos;

    public ProductoMenuService(
        IProductoMenuRepository repo,
        IInsumoRepository insumos,
        ICostoProductoService costos)
    {
        _repo = repo;
        _insumos = insumos;
        _costos = costos;
    }

    public async Task<List<ProductoMenuDto>?> ListarAsync(int cuentaId, string? busqueda, bool incluirInactivos)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return null;

        var productos = await _repo.ListarAsync(negocioId.Value, busqueda, incluirInactivos);
        var lista = new List<ProductoMenuDto>();
        foreach (var p in productos)
        {
            // Costo resumido por producto (recetas almacenadas; sin insumos directos
            // transitorios porque no se persisten sin cambios de BD).
            var costo = await _costos.CalcularAsync(cuentaId, p.Id);
            var total = costo.Exito && costo.Datos is not null ? costo.Datos.CostoTotal : 0;
            lista.Add(new ProductoMenuDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                PrecioVenta = p.PrecioVenta,
                CostoTotal = total,
                MargenReal = Margen(p.PrecioVenta, total),
                NumRecetas = p.ProductoMenuReceta.Count,
                Activo = p.Activo,
                FechaCreacion = p.FechaCreacion
            });
        }
        return lista.OrderBy(p => p.Nombre).ToList();
    }

    public async Task<ResultadoProductoMenuDto> ObtenerAsync(int cuentaId, int id)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoProductoMenuDto.NoHallado(SinNegocio);

        var costo = await _costos.CalcularAsync(cuentaId, id);
        if (costo.NoEncontrado) return ResultadoProductoMenuDto.NoHallado(costo.Mensaje);
        if (!costo.Exito || costo.Datos is null) return ResultadoProductoMenuDto.Error(costo.Mensaje);

        var c = costo.Datos;
        var producto = await _repo.ObtenerPorIdAsync(negocioId.Value, id);
        return ResultadoProductoMenuDto.Ok(new ProductoMenuDetalleDto
        {
            Id = id,
            Nombre = c.Producto,
            PrecioVenta = c.PrecioVenta,
            CostoTotal = c.CostoTotal,
            MargenReal = c.MargenReal,
            NumRecetas = c.Recetas.Count,
            Activo = producto!.Activo,
            FechaCreacion = producto.FechaCreacion,
            Recetas = c.Recetas,
            InsumosDirectos = c.InsumosDirectos,
            CostoRecetas = c.CostoRecetas,
            CostoInsumosDirectos = c.CostoInsumosDirectos,
            Advertencias = c.Advertencias
        }, "Detalle del producto.");
    }

    public async Task<ResultadoProductoMenuDto> CrearAsync(int cuentaId, CrearProductoMenuDto dto)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoProductoMenuDto.NoHallado(SinNegocio);

        var nombre = dto.Nombre.Trim();
        var error = ValidarNombre(nombre);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);
        error = ValidarRecetas(dto.Recetas);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);
        error = ValidarPrecio(dto.PrecioVenta);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);

        if (await _repo.ExisteNombreAsync(negocioId.Value, nombre))
            return ResultadoProductoMenuDto.Error(Duplicado);

        var recetas = await _repo.ObtenerRecetasConInsumosAsync(
            negocioId.Value, dto.Recetas.Select(r => r.RecetaId).Distinct());
        if (recetas.Count != dto.Recetas.Select(r => r.RecetaId).Distinct().Count())
            return ResultadoProductoMenuDto.Error("Alguna de las recetas no existe en tu negocio.");

        try
        {
            var creado = await _repo.CrearAsync(new ProductoMenu
            {
                NegocioId = negocioId.Value,
                Nombre = nombre,
                PrecioVenta = NormalizarPrecio(dto.PrecioVenta),
                Activo = dto.Activo ?? true,
                FechaCreacion = DateTime.UtcNow,
                ProductoMenuReceta = dto.Recetas.Select(r => new ProductoMenuRecetum
                {
                    RecetaId = r.RecetaId,
                    Cantidad = r.Cantidad
                }).ToList()
            });
            return await ObtenerAsync(cuentaId, creado.Id);
        }
        catch (DbUpdateException)
        {
            return ResultadoProductoMenuDto.Error(Duplicado);
        }
    }

    // PUT estandarizado: reemplazo total (nombre, precio, activo y recetas obligatorios).
    public async Task<ResultadoProductoMenuDto> ActualizarAsync(int cuentaId, int id, ActualizarProductoMenuDto dto)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoProductoMenuDto.NoHallado(SinNegocio);

        var producto = await _repo.ObtenerParaEditarAsync(negocioId.Value, id);
        if (producto is null) return ResultadoProductoMenuDto.NoHallado(NoExiste);

        var nombre = dto.Nombre.Trim();
        var error = ValidarNombre(nombre);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);
        error = ValidarRecetas(dto.Recetas);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);
        error = ValidarPrecio(dto.PrecioVenta);
        if (error is not null) return ResultadoProductoMenuDto.Error(error);

        if (await _repo.ExisteNombreAsync(negocioId.Value, nombre, id))
            return ResultadoProductoMenuDto.Error(Duplicado);

        var recetas = await _repo.ObtenerRecetasConInsumosAsync(
            negocioId.Value, dto.Recetas.Select(r => r.RecetaId).Distinct());
        if (recetas.Count != dto.Recetas.Select(r => r.RecetaId).Distinct().Count())
            return ResultadoProductoMenuDto.Error("Alguna de las recetas no existe en tu negocio.");

        producto.Nombre = nombre;
        producto.PrecioVenta = NormalizarPrecio(dto.PrecioVenta);
        producto.Activo = dto.Activo;
        _repo.ReemplazarRecetas(producto, dto.Recetas.Select(r => new ProductoMenuRecetum
        {
            RecetaId = r.RecetaId,
            Cantidad = r.Cantidad
        }));

        try { await _repo.GuardarCambiosAsync(); }
        catch (DbUpdateException) { return ResultadoProductoMenuDto.Error(Duplicado); }

        return await ObtenerAsync(cuentaId, id);
    }

    // PATCH: solo los campos enviados (evita pérdida de datos).
    public async Task<ResultadoProductoMenuDto> ActualizarParcialAsync(int cuentaId, int id, PatchProductoMenuDto dto)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoProductoMenuDto.NoHallado(SinNegocio);

        var producto = await _repo.ObtenerParaEditarAsync(negocioId.Value, id);
        if (producto is null) return ResultadoProductoMenuDto.NoHallado(NoExiste);

        var hayCambios = false;

        if (dto.Nombre is not null)
        {
            var nombre = dto.Nombre.Trim();
            var error = ValidarNombre(nombre);
            if (error is not null) return ResultadoProductoMenuDto.Error(error);
            if (!string.Equals(producto.Nombre, nombre, StringComparison.Ordinal) &&
                await _repo.ExisteNombreAsync(negocioId.Value, nombre, id))
                return ResultadoProductoMenuDto.Error(Duplicado);
            producto.Nombre = nombre; hayCambios = true;
        }

        if (dto.QuitarPrecioVenta == true)
        {
            producto.PrecioVenta = null; hayCambios = true;
        }
        else if (dto.PrecioVenta.HasValue)
        {
            var errorPrecio = ValidarPrecio(dto.PrecioVenta);
            if (errorPrecio is not null)
                return ResultadoProductoMenuDto.Error(errorPrecio);
            producto.PrecioVenta = NormalizarPrecio(dto.PrecioVenta); hayCambios = true;
        }

        if (dto.Activo.HasValue)
        {
            producto.Activo = dto.Activo.Value; hayCambios = true;
        }

        if (dto.Recetas is not null)
        {
            var error = ValidarRecetas(dto.Recetas);
            if (error is not null) return ResultadoProductoMenuDto.Error(error);
            var recetas = await _repo.ObtenerRecetasConInsumosAsync(
                negocioId.Value, dto.Recetas.Select(r => r.RecetaId).Distinct());
            if (recetas.Count != dto.Recetas.Select(r => r.RecetaId).Distinct().Count())
                return ResultadoProductoMenuDto.Error("Alguna de las recetas no existe en tu negocio.");
            _repo.ReemplazarRecetas(producto, dto.Recetas.Select(r => new ProductoMenuRecetum
            {
                RecetaId = r.RecetaId,
                Cantidad = r.Cantidad
            }));
            hayCambios = true;
        }

        if (!hayCambios)
            return ResultadoProductoMenuDto.Error("Debes enviar al menos un campo para actualizar.");

        try { await _repo.GuardarCambiosAsync(); }
        catch (DbUpdateException) { return ResultadoProductoMenuDto.Error(Duplicado); }

        return await ObtenerAsync(cuentaId, id);
    }

    public async Task<ResultadoCosteo<bool>> EliminarAsync(int cuentaId, int id)
    {
        var negocioId = await _insumos.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue) return ResultadoCosteo<bool>.NoHallado(SinNegocio);

        var producto = await _repo.ObtenerParaEditarAsync(negocioId.Value, id);
        if (producto is null) return ResultadoCosteo<bool>.NoHallado(NoExiste);

        _repo.Eliminar(producto);
        await _repo.GuardarCambiosAsync();
        return ResultadoCosteo<bool>.Ok(true, "El producto fue eliminado correctamente.");
    }

    private static string? ValidarNombre(string nombre)
    {
        if (nombre.Length < 3) return "El nombre debe tener al menos 3 caracteres.";
        if (nombre.Length > 75) return "El nombre no puede superar los 75 caracteres.";
        return null;
    }

    private static string? ValidarPrecio(decimal? precio)
    {
        if (!precio.HasValue) return null;
        if (precio.Value <= 0) return "El precio de venta debe ser mayor a 0.";
        if (precio.Value > 999999.99m)
            return "El precio de venta no puede exceder los 6 dígitos (máximo 999.999,99).";
        if (decimal.Round(precio.Value, 2) != precio.Value)
            return "El precio de venta admite máximo 2 decimales.";
        return null;
    }

    // Toda fracción, unidad o decena se aproxima hacia arriba a la centena (143 → 200).
    private static decimal? NormalizarPrecio(decimal? precio) =>
        precio.HasValue ? decimal.Ceiling(precio.Value / 100m) * 100 : null;

    private static string? ValidarRecetas(List<RecetaEnProductoDto> recetas)
    {
        if (recetas.Count == 0) return "El producto necesita al menos una receta.";
        if (recetas.Count > 50) return "Máximo 50 recetas por producto.";
        var ids = recetas.Select(r => r.RecetaId).ToList();
        if (ids.Any(i => i <= 0)) return "Selecciona una receta válida.";
        if (ids.Distinct().Count() != ids.Count) return "Hay recetas repetidas en el producto.";
        foreach (var r in recetas)
        {
            if (r.Cantidad <= 0) return "La cantidad de cada receta debe ser mayor a 0.";
            if (decimal.Round(r.Cantidad, 3) != r.Cantidad)
                return "La cantidad de receta admite máximo 3 decimales.";
            if (r.Cantidad > 10000) return "La cantidad no puede superar 10000.";
        }
        return null;
    }

    // Margen real coherente con el precio por suma: (precio − costo) ÷ costo × 100.
    private static decimal? Margen(decimal? precio, decimal costo) =>
        precio.HasValue && precio.Value > 0 && costo > 0
            ? decimal.Round((precio.Value - costo) / costo * 100m, 2)
            : null;
}
