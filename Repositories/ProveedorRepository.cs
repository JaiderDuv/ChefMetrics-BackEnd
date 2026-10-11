using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public sealed class ProveedorRepository : IProveedorRepository
{
    private readonly ChefMetricsContext _context;

    public ProveedorRepository(ChefMetricsContext context)
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

    public async Task<int?> ObtenerNegocioIdDeInsumoAsync(int insumoId)
    {
        return await _context.Insumos
            .Where(i => i.Id == insumoId)
            .Select(i => (int?)i.NegocioId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExisteInsumoEnNegocioAsync(int negocioId, int insumoId)
    {
        return await _context.Insumos.AnyAsync(i => i.Id == insumoId && i.NegocioId == negocioId);
    }

    public async Task<List<Proveedor>> ListarPorNegocioAsync(int negocioId)
    {
        return await _context.Proveedors
            .AsNoTracking()
            .Where(p => p.NegocioId == negocioId)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Proveedor>> BuscarPorNombreAsync(int negocioId, string termino, int limite)
    {
        var prefijo = termino.Trim().ToLower();
        var top = limite < 1 ? 8 : Math.Min(limite, 20);

        return await _context.Proveedors
            .AsNoTracking()
            .Where(p => p.NegocioId == negocioId && p.Nombre.ToLower().StartsWith(prefijo))
            .OrderBy(p => p.Nombre)
            .Take(top)
            .ToListAsync();
    }

    public async Task<Proveedor?> ObtenerPorIdAsync(int proveedorId)
    {
        return await _context.Proveedors.FirstOrDefaultAsync(p => p.Id == proveedorId);
    }

    public async Task<Proveedor> CrearAsync(Proveedor proveedor)
    {
        await _context.Proveedors.AddAsync(proveedor);
        await _context.SaveChangesAsync();
        return proveedor;
    }

    public async Task<List<InsumoProveedor>> ListarPorInsumoAsync(int insumoId)
    {
        return await _context.InsumoProveedors
            .AsNoTracking()
            .Include(ip => ip.Proveedor)
            .Where(ip => ip.InsumoId == insumoId)
            .OrderBy(ip => ip.Costo)
            .ToListAsync();
    }

    public async Task<InsumoProveedor?> ObtenerAsociacionAsync(int insumoId, int proveedorId)
    {
        return await _context.InsumoProveedors
            .FirstOrDefaultAsync(ip => ip.InsumoId == insumoId && ip.ProveedorId == proveedorId);
    }

    public async Task<bool> ExisteAsociacionAsync(int insumoId, int proveedorId)
    {
        return await _context.InsumoProveedors
            .AnyAsync(ip => ip.InsumoId == insumoId && ip.ProveedorId == proveedorId);
    }

    public async Task<decimal?> ObtenerCantidadInsumoAsync(int insumoId)
    {
        return await _context.Insumos
            .Where(i => i.Id == insumoId)
            .Select(i => i.Cantidad)
            .FirstOrDefaultAsync();
    }

    public async Task<InsumoProveedor> AsociarAsync(int insumoId, int proveedorId, decimal costo, decimal? cantidad = null)
    {
        var esElPrimero = !await _context.InsumoProveedors.AnyAsync(ip => ip.InsumoId == insumoId);
        cantidad ??= await ObtenerCantidadInsumoAsync(insumoId);

        var nueva = new InsumoProveedor
        {
            InsumoId = insumoId,
            ProveedorId = proveedorId,
            Costo = costo,
            EsPrincipal = esElPrimero, // criterio 1: el primero queda principal automáticamente
            FechaActualizacion = DateTime.UtcNow
        };

        await _context.InsumoProveedors.AddAsync(nueva);
        await _context.SaveChangesAsync();

        // Primer registro del historial para esta asociación (valor inicial, sin variación)
        await _context.HistorialPrecioInsumos.AddAsync(new HistorialPrecioInsumo
        {
            InsumoProveedorId = nueva.Id,
            CostoAnterior = costo,
            CostoNuevo = costo,
            Cantidad = cantidad,
            FechaCambio = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return nueva;
    }

    public async Task<bool> DesasociarAsync(int insumoId, int proveedorId)
    {
        var asociacion = await ObtenerAsociacionAsync(insumoId, proveedorId);
        if (asociacion == null) return false;

        var eraPrincipal = asociacion.EsPrincipal;
        _context.InsumoProveedors.Remove(asociacion);
        await _context.SaveChangesAsync();

        if (eraPrincipal)
        {
            var siguiente = await _context.InsumoProveedors
                .Where(ip => ip.InsumoId == insumoId)
                .OrderBy(ip => ip.FechaActualizacion)
                .FirstOrDefaultAsync();

            if (siguiente != null)
            {
                siguiente.EsPrincipal = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> MarcarPrincipalAsync(int insumoId, int proveedorId)
    {
        var asociaciones = await _context.InsumoProveedors
            .Where(ip => ip.InsumoId == insumoId)
            .ToListAsync();

        var objetivo = asociaciones.FirstOrDefault(ip => ip.ProveedorId == proveedorId);
        if (objetivo == null) return false;

        // criterio 2: nunca dos principales a la vez
        foreach (var asociacion in asociaciones)
        {
            asociacion.EsPrincipal = asociacion.ProveedorId == proveedorId;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarCostoAsync(int insumoId, int proveedorId, decimal nuevoCosto, decimal? cantidad = null)
    {
        var asociacion = await ObtenerAsociacionAsync(insumoId, proveedorId);
        if (asociacion == null) return false;

        if (asociacion.Costo != nuevoCosto)
        {
            cantidad ??= await ObtenerCantidadInsumoAsync(insumoId);
            // criterio 4: se registra en el historial sin acción adicional del usuario
            await _context.HistorialPrecioInsumos.AddAsync(new HistorialPrecioInsumo
            {
                InsumoProveedorId = asociacion.Id,
                CostoAnterior = asociacion.Costo,
                CostoNuevo = nuevoCosto,
                Cantidad = cantidad,
                FechaCambio = DateTime.UtcNow
            });

            asociacion.Costo = nuevoCosto;
            asociacion.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<Proveedor?> ObtenerPorIdDelNegocioAsync(int negocioId, int proveedorId)
    {
        return await _context.Proveedors
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proveedorId && p.NegocioId == negocioId);
    }

    public async Task<int> ContarAsociacionesAsync(int proveedorId)
    {
        return await _context.InsumoProveedors.CountAsync(ip => ip.ProveedorId == proveedorId);
    }

    public async Task<int> PropagarNombreAInsumosAsync(int negocioId, int proveedorId, string nombreNuevo)
    {
        var nuevo = nombreNuevo.Trim();

        return await _context.Insumos
            .Where(i => i.NegocioId == negocioId
                && _context.InsumoProveedors.Any(ip =>
                    ip.InsumoId == i.Id
                    && ip.ProveedorId == proveedorId
                    && ip.EsPrincipal))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Proveedor, nuevo)
                .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
    }

    public async Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null)
    {
        var normalizado = nombre.Trim().ToLower();
        var query = _context.Proveedors
            .AsNoTracking()
            .Where(p => p.NegocioId == negocioId && p.Nombre.ToLower() == normalizado);

        if (excluirId.HasValue)
        {
            query = query.Where(p => p.Id != excluirId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task ActualizarAsync(Proveedor proveedor)
    {
        _context.Proveedors.Update(proveedor);
        await _context.SaveChangesAsync();
    }

    public void Eliminar(Proveedor proveedor)
    {
        _context.Proveedors.Remove(proveedor);
    }

    public async Task GuardarCambiosAsync()
    {
        await _context.SaveChangesAsync();
    }
}
