using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public interface IProductoMenuRepository
{
    Task<List<ProductoMenu>> ListarAsync(int negocioId, string? busqueda, bool incluirInactivos);
    Task<ProductoMenu?> ObtenerPorIdAsync(int negocioId, int id);
    Task<ProductoMenu?> ObtenerParaEditarAsync(int negocioId, int id);
    Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null);
    Task<List<Receta>> ObtenerRecetasConInsumosAsync(int negocioId, IEnumerable<int> recetaIds);
    Task<List<Insumo>> ObtenerInsumosActivosAsync(int negocioId, IEnumerable<int> insumoIds);
    Task<ProductoMenu> CrearAsync(ProductoMenu producto);
    void ReemplazarRecetas(ProductoMenu producto, IEnumerable<ProductoMenuRecetum> nuevas);
    void Eliminar(ProductoMenu producto);
    Task GuardarCambiosAsync();
}

public sealed class ProductoMenuRepository : IProductoMenuRepository
{
    private readonly ChefMetricsContext _db;
    public ProductoMenuRepository(ChefMetricsContext db) => _db = db;

    public Task<List<ProductoMenu>> ListarAsync(int negocioId, string? busqueda, bool incluirInactivos)
    {
        var q = _db.ProductoMenus.AsNoTracking()
            .Include(p => p.ProductoMenuReceta).ThenInclude(pr => pr.Receta)
            .Where(p => p.NegocioId == negocioId && (incluirInactivos || p.Activo));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.Trim().ToLower();
            q = q.Where(p => p.Nombre.ToLower().Contains(b));
        }

        return q.OrderBy(p => p.Nombre).ToListAsync();
    }

    public Task<ProductoMenu?> ObtenerPorIdAsync(int negocioId, int id) =>
        _db.ProductoMenus.AsNoTracking()
            .Include(p => p.ProductoMenuReceta).ThenInclude(pr => pr.Receta)
                .ThenInclude(r => r.RecetaInsumos).ThenInclude(ri => ri.Insumo)
            .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);

    public Task<ProductoMenu?> ObtenerParaEditarAsync(int negocioId, int id) =>
        _db.ProductoMenus
            .Include(p => p.ProductoMenuReceta)
            .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId);

    public Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null)
    {
        var n = nombre.ToLower();
        return _db.ProductoMenus.AnyAsync(p =>
            p.NegocioId == negocioId && p.Nombre.ToLower() == n &&
            (excluirId == null || p.Id != excluirId));
    }

    public Task<List<Receta>> ObtenerRecetasConInsumosAsync(int negocioId, IEnumerable<int> recetaIds)
    {
        var ids = recetaIds.ToList();
        return _db.Receta.AsNoTracking()
            .Include(r => r.RecetaInsumos).ThenInclude(ri => ri.Insumo)
            .Where(r => r.NegocioId == negocioId && ids.Contains(r.Id))
            .ToListAsync();
    }

    public Task<List<Insumo>> ObtenerInsumosActivosAsync(int negocioId, IEnumerable<int> insumoIds)
    {
        var ids = insumoIds.ToList();
        return _db.Insumos.AsNoTracking()
            .Where(i => i.NegocioId == negocioId && i.Activo && ids.Contains(i.Id))
            .ToListAsync();
    }

    public async Task<ProductoMenu> CrearAsync(ProductoMenu producto)
    {
        _db.ProductoMenus.Add(producto);
        await _db.SaveChangesAsync();
        return producto;
    }

    public void ReemplazarRecetas(ProductoMenu producto, IEnumerable<ProductoMenuRecetum> nuevas)
    {
        _db.ProductoMenuReceta.RemoveRange(producto.ProductoMenuReceta);
        foreach (var n in nuevas)
        {
            n.ProductoMenuId = producto.Id;
            _db.ProductoMenuReceta.Add(n);
        }
    }

    public void Eliminar(ProductoMenu producto)
    {
        _db.ProductoMenuReceta.RemoveRange(producto.ProductoMenuReceta);
        _db.ProductoMenus.Remove(producto);
    }

    public Task GuardarCambiosAsync() => _db.SaveChangesAsync();
}
