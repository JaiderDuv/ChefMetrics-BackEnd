using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public sealed class InsumoRepository : IInsumoRepository
{
    private readonly ChefMetricsContext _context;

    public InsumoRepository(ChefMetricsContext context)
    {
        _context = context;
    }

    public async Task<int?> ObtenerNegocioIdPorCuentaAsync(int cuentaId)
    {
        return await _context.Negocios
            .Where(n => n.Usuario.CuentaId == cuentaId)
            .Select(n => (int?)n.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Insumo?> ObtenerPorIdAsync(int negocioId, int insumoId)
    {
        return await _context.Insumos
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.NegocioId == negocioId && i.Id == insumoId);
    }

    public async Task<(List<Insumo> Items, int Total)> ListarAsync(
        int negocioId,
        string? busqueda,
        bool incluirInactivos,
        string ordenarPor,
        bool descendente,
        int pagina,
        int tamanoPagina)
    {
        var query = _context.Insumos
            .AsNoTracking()
            .Where(i => i.NegocioId == negocioId);

        if (!incluirInactivos)
        {
            query = query.Where(i => i.Activo);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(i =>
                i.Nombre.ToLower().Contains(termino) ||
                (i.Proveedor != null && i.Proveedor.ToLower().Contains(termino)));
        }

        var total = await query.CountAsync();

        query = (ordenarPor, descendente) switch
        {
            ("costo", false) => query.OrderBy(i => i.CostoUnitario),
            ("costo", true) => query.OrderByDescending(i => i.CostoUnitario),
            ("merma", false) => query.OrderBy(i => i.PorcentajeMerma),
            ("merma", true) => query.OrderByDescending(i => i.PorcentajeMerma),
            ("fecha", false) => query.OrderBy(i => i.FechaActualizacion),
            ("fecha", true) => query.OrderByDescending(i => i.FechaActualizacion),
            (_, true) => query.OrderByDescending(i => i.Nombre),
            _ => query.OrderBy(i => i.Nombre),
        };

        var items = await query
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null)
    {
        var normalizado = nombre.Trim().ToLower();
        var query = _context.Insumos
            .AsNoTracking()
            .Where(i => i.NegocioId == negocioId && i.Nombre.ToLower() == normalizado);

        if (excluirId.HasValue)
        {
            query = query.Where(i => i.Id != excluirId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> ContarRecetasAsociadasAsync(int insumoId)
    {
        return await _context.RecetaInsumos
            .AsNoTracking()
            .CountAsync(ri => ri.InsumoId == insumoId);
    }

    public async Task<Dictionary<int, int>> ContarRecetasPorInsumosAsync(IEnumerable<int> insumoIds)
    {
        var ids = insumoIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _context.RecetaInsumos
            .AsNoTracking()
            .Where(ri => ids.Contains(ri.InsumoId))
            .GroupBy(ri => ri.InsumoId)
            .Select(g => new { InsumoId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.InsumoId, x => x.Total);
    }

    public async Task<Insumo> CrearAsync(Insumo insumo)
    {
        await _context.Insumos.AddAsync(insumo);
        await _context.SaveChangesAsync();
        return insumo;
    }

    public async Task ActualizarAsync(Insumo insumo)
    {
        // Las lecturas usan AsNoTracking: se adjunta y se marca modificada.
        _context.Insumos.Update(insumo);
        await _context.SaveChangesAsync();
    }

    public void Eliminar(Insumo insumo)
    {
        // Se adjunta por Id: las lecturas usan AsNoTracking.
        _context.Insumos.Remove(insumo);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}
