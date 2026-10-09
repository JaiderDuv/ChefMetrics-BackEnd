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
        string? categoria,
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
                _context.InsumoProveedors.Any(ip =>
                    ip.InsumoId == i.Id && ip.Proveedor.Nombre.ToLower().Contains(termino)));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var categoriaNormalizada = categoria.Trim().ToLower();
            query = query.Where(i => i.Categoria.ToLower() == categoriaNormalizada);
        }

        var total = await query.CountAsync();

        query = (ordenarPor, descendente) switch
        {
            ("costo", false) => query.OrderBy(i => i.CostoUnitario),
            ("costo", true) => query.OrderByDescending(i => i.CostoUnitario),
            ("merma", false) => query.OrderBy(i => i.PorcentajeMerma),
            ("merma", true) => query.OrderByDescending(i => i.PorcentajeMerma),
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

    public async Task<Proveedor> ObtenerOCrearProveedorAsync(int negocioId, string nombre)
    {
        var normalizado = nombre.Trim();
        var existente = await _context.Proveedors
            .FirstOrDefaultAsync(p =>
                p.NegocioId == negocioId && p.Nombre.ToLower() == normalizado.ToLower());

        if (existente is not null)
        {
            return existente;
        }

        var nuevo = new Proveedor
        {
            NegocioId = negocioId,
            Nombre = normalizado,
            FechaCreacion = DateTime.UtcNow
        };

        await _context.Proveedors.AddAsync(nuevo);
        await _context.SaveChangesAsync();
        return nuevo;
    }

    public Task<InsumoProveedor?> ObtenerInsumoProveedorAsync(int insumoId, int proveedorId)
    {
        return _context.InsumoProveedors
            .Include(ip => ip.Proveedor)
            .FirstOrDefaultAsync(ip => ip.InsumoId == insumoId && ip.ProveedorId == proveedorId)!;
    }

    public Task<InsumoProveedor?> ObtenerInsumoProveedorPrincipalAsync(int insumoId)
    {
        return _context.InsumoProveedors
            .Include(ip => ip.Proveedor)
            .FirstOrDefaultAsync(ip => ip.InsumoId == insumoId && ip.EsPrincipal)!;
    }

    public async Task<string?> ObtenerNombreProveedorPrincipalAsync(int insumoId)
    {
        return await _context.InsumoProveedors
            .AsNoTracking()
            .Where(ip => ip.InsumoId == insumoId && ip.EsPrincipal)
            .Select(ip => ip.Proveedor.Nombre)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<int, string>> ObtenerNombresProveedoresPrincipalesAsync(IEnumerable<int> insumoIds)
    {
        var ids = insumoIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return await _context.InsumoProveedors
            .AsNoTracking()
            .Where(ip => ids.Contains(ip.InsumoId) && ip.EsPrincipal)
            .Select(ip => new { ip.InsumoId, ip.Proveedor.Nombre })
            .ToDictionaryAsync(x => x.InsumoId, x => x.Nombre);
    }

    public async Task<InsumoProveedor> CrearInsumoProveedorAsync(InsumoProveedor asociacion)
    {
        await _context.InsumoProveedors.AddAsync(asociacion);
        await _context.SaveChangesAsync();
        return asociacion;
    }

    public async Task DesmarcarPrincipalesAsync(int insumoId, int exceptoId)
    {
        await _context.InsumoProveedors
            .Where(ip => ip.InsumoId == insumoId && ip.Id != exceptoId && ip.EsPrincipal)
            .ExecuteUpdateAsync(setters => setters.SetProperty(ip => ip.EsPrincipal, false));
    }

    public async Task<HistorialPrecioInsumo> RegistrarCambioPrecioAsync(HistorialPrecioInsumo cambio)
    {
        await _context.HistorialPrecioInsumos.AddAsync(cambio);
        await _context.SaveChangesAsync();
        return cambio;
    }

    public async Task EliminarPreciosAsync(int insumoId)
    {
        var historial = await _context.HistorialPrecioInsumos
            .Where(h => h.InsumoProveedor.InsumoId == insumoId)
            .ToListAsync();
        _context.HistorialPrecioInsumos.RemoveRange(historial);

        var asociaciones = await _context.InsumoProveedors
            .Where(ip => ip.InsumoId == insumoId)
            .ToListAsync();
        _context.InsumoProveedors.RemoveRange(asociaciones);
    }

    public async Task<List<HistorialPrecioInsumo>> ListarHistorialPorInsumoAsync(int insumoId)
    {
        return await _context.HistorialPrecioInsumos
            .AsNoTracking()
            .Include(h => h.InsumoProveedor)
                .ThenInclude(ip => ip.Proveedor)
            .Include(h => h.InsumoProveedor)
                .ThenInclude(ip => ip.Insumo)
            .Where(h => h.InsumoProveedor.InsumoId == insumoId)
            .OrderByDescending(h => h.FechaCambio)
            .ToListAsync();
    }

    public async Task<List<HistorialPrecioInsumo>> ListarHistorialGeneralAsync(
        int negocioId, string? nombre, string? proveedor)
    {
        var query = _context.HistorialPrecioInsumos
            .AsNoTracking()
            .Include(h => h.InsumoProveedor)
                .ThenInclude(ip => ip.Proveedor)
            .Include(h => h.InsumoProveedor)
                .ThenInclude(ip => ip.Insumo)
            .Where(h => h.InsumoProveedor.Insumo.NegocioId == negocioId);

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var termino = nombre.Trim().ToLower();
            query = query.Where(h => h.InsumoProveedor.Insumo.Nombre.ToLower().Contains(termino));
        }

        if (!string.IsNullOrWhiteSpace(proveedor))
        {
            var termino = proveedor.Trim().ToLower();
            query = query.Where(h =>
                h.InsumoProveedor.Proveedor.Nombre.ToLower().Contains(termino));
        }

        return await query
            .OrderByDescending(h => h.FechaCambio)
            .ToListAsync();
    }

    public async Task<Insumo> CrearAsync(Insumo insumo)
    {
        await _context.Insumos.AddAsync(insumo);
        await _context.SaveChangesAsync();
        return insumo;
    }

    public async Task<List<Insumo>> ListarParaHistorialAsync(int negocioId, string? nombre, string? proveedor)
    {
        var query = _context.Insumos
            .AsNoTracking()
            .Where(i => i.NegocioId == negocioId);

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var termino = nombre.Trim().ToLower();
            query = query.Where(i => i.Nombre.ToLower().Contains(termino));
        }

        if (!string.IsNullOrWhiteSpace(proveedor))
        {
            var termino = proveedor.Trim().ToLower();
            query = query.Where(i => _context.InsumoProveedors.Any(ip =>
                ip.InsumoId == i.Id && ip.Proveedor.Nombre.ToLower().Contains(termino)));
        }

        return await query
            .OrderByDescending(i => i.FechaActualizacion)
            .ToListAsync();
    }

    public async Task ActualizarAsync(Insumo insumo)
    {
        _context.Insumos.Update(insumo);
        await _context.SaveChangesAsync();
    }

    public void Eliminar(Insumo insumo)
    {
        _context.Insumos.Remove(insumo);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}
