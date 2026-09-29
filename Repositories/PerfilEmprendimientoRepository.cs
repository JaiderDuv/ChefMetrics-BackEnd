using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Repositories;

public sealed class PerfilEmprendimientoRepository : IPerfilEmprendimientoRepository
{
    private readonly ChefMetricsContext _context;

    public PerfilEmprendimientoRepository(ChefMetricsContext context)
    {
        _context = context;
    }

    public async Task<Cuenta?> ObtenerPerfilCompletoAsync(int cuentaId)
    {
        return await _context.Cuenta
            .Include(c => c.UsuarioEmprendedor!)
                .ThenInclude(u => u.Negocio!)
                    .ThenInclude(n => n.TipoEmprendimiento)
            .Include(c => c.UsuarioEmprendedor!)
                .ThenInclude(u => u.SociosNegocio)
            .FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<PerfilEditGrant?> ObtenerPermisoEdicionAsync(string tokenHash)
    {
        return await _context.PerfilEditGrants
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash);
    }

    public async Task<PerfilEditGrant> RegistrarPermisoEdicionAsync(PerfilEditGrant permiso)
    {
        await _context.PerfilEditGrants.AddAsync(permiso);
        await _context.SaveChangesAsync();
        return permiso;
    }

    public async Task RevocarPermisosEdicionAsync(int cuentaId)
    {
        await _context.PerfilEditGrants
            .Where(p => p.CuentaId == cuentaId && !p.Usado)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Usado, true));
    }

    public Task<bool> ExisteCorreoAsync(string correo, int cuentaId)
    {
        var normalizado = correo.Trim().ToLowerInvariant();
        return _context.Cuenta.AnyAsync(c =>
            c.Id != cuentaId && c.Correo.ToLower() == normalizado);
    }

    public Task<bool> ExisteNitAsync(string nit, int cuentaId)
    {
        var normalizado = nit.Trim();
        return _context.UsuarioEmprendedors.AnyAsync(u =>
            u.CuentaId != cuentaId && u.Nit != null && u.Nit == normalizado);
    }

    public async Task GuardarCambiosAsync(
        PerfilEditGrant permiso,
        IEnumerable<SocioNegocio> sociosNuevos,
        IEnumerable<SocioNegocio> sociosEliminados)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.SociosNegocio.RemoveRange(sociosEliminados);
        await _context.SociosNegocio.AddRangeAsync(sociosNuevos);
        permiso.Usado = true;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task GuardarCambiosUsuarioAsync()
    {
        await _context.SaveChangesAsync();
    }
}
