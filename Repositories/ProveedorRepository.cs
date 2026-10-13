using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using ChefMetrics_BackEnd.Utilities;
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

    public async Task<InsumoProveedor> AsociarAsync(
        int insumoId, int proveedorId, decimal costo, decimal cantidad, string unidadMedida)
    {
        var esElPrimero = !await _context.InsumoProveedors.AnyAsync(ip => ip.InsumoId == insumoId);

        var nueva = new InsumoProveedor
        {
            InsumoId = insumoId,
            ProveedorId = proveedorId,
            Costo = costo,
            Cantidad = cantidad,
            UnidadMedida = unidadMedida,
            EsPrincipal = esElPrimero,
            FechaActualizacion = DateTime.UtcNow
        };

        await _context.InsumoProveedors.AddAsync(nueva);
        await _context.SaveChangesAsync();

        await _context.HistorialPrecioInsumos.AddAsync(new HistorialPrecioInsumo
        {
            InsumoProveedorId = nueva.Id,
            CostoAnterior = costo,
            CostoNuevo = costo,
            Cantidad = cantidad,
            FechaCambio = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Si es el primero (principal), sincroniza Insumo.Cantidad
        if (esElPrimero)
        {
            await _context.Insumos
                .Where(i => i.Id == insumoId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Cantidad, cantidad)
                    .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
        }

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

                // Sincroniza Insumo.Cantidad con el nuevo principal
                await _context.Insumos
                    .Where(i => i.Id == insumoId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(i => i.Cantidad, siguiente.Cantidad)
                        .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
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
        if (objetivo.EsPrincipal) return true;

        // Primero se quita el principal actual y luego se pone el nuevo,
        // para no violar el índice único UQ_InsumoProveedor_Principal.
        foreach (var a in asociaciones.Where(a => a.EsPrincipal))
            a.EsPrincipal = false;
        await _context.SaveChangesAsync();

        objetivo.EsPrincipal = true;
        await _context.SaveChangesAsync();

        // Insumo.Cantidad, costo, unidad y proveedor los copia SincronizarInsumoConPrincipalAsync.
        return true;
    }

    public async Task<bool> ActualizarCostoAsync(
        int insumoId, int proveedorId, decimal nuevoCosto, decimal? cantidad = null, string? unidadMedida = null)
    {
        var asociacion = await ObtenerAsociacionAsync(insumoId, proveedorId);
        if (asociacion == null) return false;

        var cantidadFinal = cantidad ?? asociacion.Cantidad;
        var unidadFinal = string.IsNullOrWhiteSpace(unidadMedida)
            ? asociacion.UnidadMedida
        : unidadMedida.Trim().ToLowerInvariant();
        var hayCambio = asociacion.Costo != nuevoCosto ||
                        (cantidad.HasValue && asociacion.Cantidad != cantidad.Value) ||
                        (unidadMedida != null && asociacion.UnidadMedida != unidadFinal);

        if (!hayCambio) return true;

        await _context.HistorialPrecioInsumos.AddAsync(new HistorialPrecioInsumo
        {
            InsumoProveedorId = asociacion.Id,
            CostoAnterior = asociacion.Costo,
            CostoNuevo = nuevoCosto,
            Cantidad = cantidadFinal,
            FechaCambio = DateTime.UtcNow
        });

        asociacion.Costo = nuevoCosto;
        asociacion.Cantidad = cantidadFinal;
        asociacion.UnidadMedida = unidadFinal;
        asociacion.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Si es principal, sincroniza Insumo.Cantidad
        if (asociacion.EsPrincipal)
        {
            await _context.Insumos
                .Where(i => i.Id == insumoId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Cantidad, cantidadFinal)
                    .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
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
                    ip.InsumoId == i.Id && ip.ProveedorId == proveedorId && ip.EsPrincipal))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Proveedor, nuevo)
                .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
    }

    public async Task<bool> ExisteNombreAsync(int negocioId, string nombre, int? excluirId = null)
    {
        
        var normalizado = NormalizadorTexto.Normalizar(nombre);

        var query = _context.Proveedors
            .AsNoTracking()
            .Where(p => p.NegocioId == negocioId);

        if (excluirId.HasValue)
            query = query.Where(p => p.Id != excluirId.Value);
        return await query.AnyAsync();
    }

    public async Task<List<string>> ObtenerNombresInsumosAsociadosAsync(int proveedorId)
    {
        return await _context.InsumoProveedors
            .AsNoTracking()
            .Where(ip => ip.ProveedorId == proveedorId)
            .Select(ip => ip.Insumo.Nombre)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }

    public async Task ActualizarAsync(Proveedor proveedor)
    {
        _context.Proveedors.Update(proveedor);
        await _context.SaveChangesAsync();
    }

    public void Eliminar(Proveedor proveedor) => _context.Proveedors.Remove(proveedor);
    public async Task EliminarAsociacionesDeProveedorAsync(int proveedorId)
    {
        // Primero borra el historial de precios de todas las asociaciones
        var asociacionIds = await _context.InsumoProveedors
            .Where(ip => ip.ProveedorId == proveedorId)
            .Select(ip => ip.Id)
            .ToListAsync();

        if (asociacionIds.Any())
        {
            var historial = await _context.HistorialPrecioInsumos
                .Where(h => asociacionIds.Contains(h.InsumoProveedorId))
                .ToListAsync();
            _context.HistorialPrecioInsumos.RemoveRange(historial);

            var asociaciones = await _context.InsumoProveedors
                .Where(ip => ip.ProveedorId == proveedorId)
                .ToListAsync();

            // Para cada asociación que era principal, limpiar el campo Proveedor del insumo
            var insumoIdsConPrincipal = asociaciones
                .Where(ip => ip.EsPrincipal)
                .Select(ip => ip.InsumoId)
                .ToList();

            if (insumoIdsConPrincipal.Any())
            {
                await _context.Insumos
                    .Where(i => insumoIdsConPrincipal.Contains(i.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(i => i.Proveedor, (string?)null)
                        .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
            }

            _context.InsumoProveedors.RemoveRange(asociaciones);
            await _context.SaveChangesAsync();
        }
    }

    public async Task GuardarCambiosAsync() => await _context.SaveChangesAsync();

    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
{
    "g", "kg", "l", "ml", "oz", "lb", "unidad", "taza"
};

    public async Task SincronizarInsumoConPrincipalAsync(int insumoId)
    {
        var unidadActual = await _context.Insumos
            .Where(i => i.Id == insumoId)
            .Select(i => i.UnidadMedida)
            .FirstOrDefaultAsync();
        if (unidadActual is null) return; // el insumo no existe

        var asociaciones = await _context.InsumoProveedors
            .AsNoTracking()
            .Where(a => a.InsumoId == insumoId)
            .Select(a => new
            {
                a.Id,
                a.Costo,
                a.Cantidad,
                a.UnidadMedida,
                a.EsPrincipal,
                Nombre = a.Proveedor.Nombre
            })
            .ToListAsync();

        if (asociaciones.Count == 0)
        {
            await _context.Insumos
                .Where(i => i.Id == insumoId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Proveedor, (string?)null)
                    .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
            return;
        }

        var principal = asociaciones.FirstOrDefault(a => a.EsPrincipal);
        if (principal is null)
        {
            principal = asociaciones
                .OrderBy(a => a.Cantidad > 0 ? a.Costo / a.Cantidad : decimal.MaxValue)
                .First();

            var idPrincipal = principal.Id;
            await _context.InsumoProveedors
                .Where(a => a.Id == idPrincipal)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.EsPrincipal, true));
        }

        // Solo se copia la unidad si es una de las permitidas por CK_Insumo_UnidadMedida.
        var unidadPrincipal = principal.UnidadMedida?.Trim() ?? string.Empty;
        var unidad = UnidadesValidas.Contains(unidadPrincipal)
            ? unidadPrincipal.ToLowerInvariant()
            : unidadActual;

        var costo = principal.Costo;
        var cantidad = (decimal?)principal.Cantidad;
        var nombre = principal.Nombre;

        await _context.Insumos
            .Where(i => i.Id == insumoId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.CostoUnitario, costo)
                .SetProperty(i => i.Cantidad, cantidad)
                .SetProperty(i => i.UnidadMedida, unidad)
                .SetProperty(i => i.Proveedor, nombre)
                .SetProperty(i => i.FechaActualizacion, DateTime.UtcNow));
    }

}
