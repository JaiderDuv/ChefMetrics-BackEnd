using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public interface IRecetaRepository
{
    Task<List<Receta>> ListarAsync(int negocioId, string? busqueda);
    Task<Receta?> ObtenerPorIdAsync(int negocioId, int recetaId);          // solo lectura
    Task<Receta?> ObtenerParaEditarAsync(int negocioId, int recetaId);      // con tracking
    Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirRecetaId = null);
    Task<List<Insumo>> ObtenerInsumosActivosAsync(int negocioId, IEnumerable<int> ids);
    Task<int> ContarProductosMenuAsync(int recetaId);
    Task<Receta> CrearAsync(Receta receta);
    void ReemplazarInsumos(Receta receta, IEnumerable<RecetaInsumo> nuevos);
    void Eliminar(Receta receta);
    Task GuardarCambiosAsync();
}

public sealed class RecetaRepository : IRecetaRepository
{
    private readonly ChefMetricsContext _db;

    public RecetaRepository(ChefMetricsContext db) => _db = db;

    public Task<List<Receta>> ListarAsync(int negocioId, string? busqueda)
    {
        var query = _db.Receta.AsNoTracking()
            .Include(r => r.RecetaInsumos).ThenInclude(ri => ri.Insumo)
            .Where(r => r.NegocioId == negocioId);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var b = busqueda.Trim().ToLower();
            query = query.Where(r => r.Nombre.ToLower().Contains(b));
        }

        return query.OrderBy(r => r.Nombre).ToListAsync();
    }

    public Task<Receta?> ObtenerPorIdAsync(int negocioId, int recetaId) =>
        _db.Receta.AsNoTracking()
            .Include(r => r.RecetaInsumos).ThenInclude(ri => ri.Insumo)
            .FirstOrDefaultAsync(r => r.Id == recetaId && r.NegocioId == negocioId);

    public Task<Receta?> ObtenerParaEditarAsync(int negocioId, int recetaId) =>
        _db.Receta
            .Include(r => r.RecetaInsumos)
            .FirstOrDefaultAsync(r => r.Id == recetaId && r.NegocioId == negocioId);

    public Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirRecetaId = null)
    {
        var n = nombre.ToLower();
        return _db.Receta.AnyAsync(r =>
            r.NegocioId == negocioId &&
            r.Nombre.ToLower() == n &&
            (excluirRecetaId == null || r.Id != excluirRecetaId));
    }

    public Task<List<Insumo>> ObtenerInsumosActivosAsync(int negocioId, IEnumerable<int> ids)
    {
        var lista = ids.ToList();
        return _db.Insumos.AsNoTracking()
            .Where(i => i.NegocioId == negocioId && i.Activo && lista.Contains(i.Id))
            .ToListAsync();
    }

    public Task<int> ContarProductosMenuAsync(int recetaId) =>
        _db.ProductoMenuReceta.CountAsync(p => p.RecetaId == recetaId);

    public async Task<Receta> CrearAsync(Receta receta)
    {
        _db.Receta.Add(receta);
        await _db.SaveChangesAsync();   // aquí se genera el Id
        return receta;
    }

    public void ReemplazarInsumos(Receta receta, IEnumerable<RecetaInsumo> nuevos)
    {
        _db.RecetaInsumos.RemoveRange(receta.RecetaInsumos);
        foreach (var n in nuevos)
        {
            n.RecetaId = receta.Id;
            _db.RecetaInsumos.Add(n);
        }
    }

    public void Eliminar(Receta receta)
    {
        _db.RecetaInsumos.RemoveRange(receta.RecetaInsumos);
        _db.Receta.Remove(receta);
    }

    public Task GuardarCambiosAsync() => _db.SaveChangesAsync();
}