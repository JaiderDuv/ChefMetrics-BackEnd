using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public interface ICosteoRepository
{
    Task<Receta?> ObtenerRecetaConInsumosAsync(int negocioId, int recetaId);
    Task<Dictionary<int, PrecioProveedorPrincipal>> ObtenerCostosProveedorPrincipalAsync(IEnumerable<int> insumoIds);
    Task<List<CostoFijo>> ListarCostosFijosAsync(int negocioId, bool soloActivos);
    Task<int?> ObtenerPreparacionesBaseMesAsync(int negocioId);
    Task<bool> ActualizarPreparacionesBaseMesAsync(int negocioId, int? preparacionesBaseMes);
    Task<CostoFijo?> ObtenerCostoFijoAsync(int negocioId, int id, bool conTracking);
    Task<bool> ExisteNombreCostoFijoAsync(int negocioId, string nombre, int? excluirId = null);
    Task<CostoFijo> CrearCostoFijoAsync(CostoFijo costo);
    void EliminarCostoFijo(CostoFijo costo);
    Task GuardarCambiosAsync();
}

public sealed class CosteoRepository : ICosteoRepository
{
    private readonly ChefMetricsContext _db;
    public CosteoRepository(ChefMetricsContext db) => _db = db;

    public Task<Receta?> ObtenerRecetaConInsumosAsync(int negocioId, int recetaId) =>
        _db.Receta.AsNoTracking()
            .Include(r => r.RecetaInsumos).ThenInclude(ri => ri.Insumo)
            .FirstOrDefaultAsync(r => r.Id == recetaId && r.NegocioId == negocioId);

    // Cotización del proveedor marcado como principal (si hay varios, el más reciente).
    // Devuelve el paquete completo: el precio por unidad se calcula en el servicio.
    public async Task<Dictionary<int, PrecioProveedorPrincipal>> ObtenerCostosProveedorPrincipalAsync(IEnumerable<int> insumoIds)
    {
        var ids = insumoIds.ToList();
        var filas = await _db.InsumoProveedors.AsNoTracking()
            .Where(ip => ids.Contains(ip.InsumoId) && ip.EsPrincipal)
            .Select(ip => new { ip.InsumoId, ip.Costo, ip.Cantidad, ip.UnidadMedida, ip.FechaActualizacion })
            .ToListAsync();
        return filas.GroupBy(f => f.InsumoId)
            .ToDictionary(g => g.Key, g =>
            {
                var x = g.OrderByDescending(f => f.FechaActualizacion).First();
                return new PrecioProveedorPrincipal(x.Costo, x.Cantidad, x.UnidadMedida);
            });
    }

    public Task<List<CostoFijo>> ListarCostosFijosAsync(int negocioId, bool soloActivos) =>
        _db.CostoFijos.AsNoTracking()
            .Where(c => c.NegocioId == negocioId && (!soloActivos || c.Activo))
            .OrderBy(c => c.Nombre).ToListAsync();

    public Task<int?> ObtenerPreparacionesBaseMesAsync(int negocioId) =>
        _db.Negocios.AsNoTracking()
            .Where(n => n.Id == negocioId)
            .Select(n => n.PreparacionesBaseMes)
            .FirstOrDefaultAsync();

    public async Task<bool> ActualizarPreparacionesBaseMesAsync(int negocioId, int? preparacionesBaseMes)
    {
        var negocio = await _db.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId);
        if (negocio is null) return false;

        negocio.PreparacionesBaseMes = preparacionesBaseMes;
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<CostoFijo?> ObtenerCostoFijoAsync(int negocioId, int id, bool conTracking)
    {
        var q = conTracking ? _db.CostoFijos.AsQueryable() : _db.CostoFijos.AsNoTracking();
        return q.FirstOrDefaultAsync(c => c.Id == id && c.NegocioId == negocioId);
    }

    public Task<bool> ExisteNombreCostoFijoAsync(int negocioId, string nombre, int? excluirId = null)
    {
        var n = nombre.ToLower();
        return _db.CostoFijos.AnyAsync(c => c.NegocioId == negocioId && c.Nombre.ToLower() == n
                                            && (excluirId == null || c.Id != excluirId));
    }

    public async Task<CostoFijo> CrearCostoFijoAsync(CostoFijo costo)
    {
        _db.CostoFijos.Add(costo);
        await _db.SaveChangesAsync();
        return costo;
    }

    public void EliminarCostoFijo(CostoFijo costo) => _db.CostoFijos.Remove(costo);
    public Task GuardarCambiosAsync() => _db.SaveChangesAsync();
}