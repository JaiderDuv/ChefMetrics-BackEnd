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
                .ThenInclude(u => u.Negocio!)
                    .ThenInclude(n => n.Empleados)
                        .ThenInclude(e => e.Rol)
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

    public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int cuentaId)
    {
        var normalizado = numeroDocumento.Trim();
        return _context.UsuarioEmprendedors.AnyAsync(u =>
            u.CuentaId != cuentaId && u.NumeroDocumento != null && u.NumeroDocumento == normalizado);
    }

    public async Task<List<Role>> ObtenerRolesAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }

    public Task<bool> ExisteRolAsync(int rolId)
    {
        return _context.Roles.AnyAsync(r => r.Id == rolId);
    }

    public Task<bool> ExisteEmpleadoCorreoAsync(int negocioId, string correo, int? excluirId = null)
    {
        var normalizado = correo.Trim().ToLowerInvariant();
        var query = _context.Empleados
            .Where(e => e.NegocioId == negocioId && e.CorreoInvitado.ToLower() == normalizado);

        if (excluirId.HasValue)
        {
            query = query.Where(e => e.Id != excluirId.Value);
        }

        return query.AnyAsync();
    }

    public async Task GuardarEmpleadosAsync(
        PerfilEditGrant permiso,
        IEnumerable<Empleado> empleadosNuevos,
        IEnumerable<Empleado> empleadosEliminados)
    {
        var eliminados = empleadosEliminados.ToList();
        var cuentaIdsSolicitadas = eliminados
            .Where(e => e.CuentaId.HasValue)
            .Select(e => e.CuentaId!.Value)
            .Distinct()
            .ToArray();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var cuentaIdsEliminables = await _context.Cuenta
            .Where(c => cuentaIdsSolicitadas.Contains(c.Id) &&
                        c.TipoCuenta == "empleado" &&
                        !_context.UsuarioEmprendedors.Any(u => u.CuentaId == c.Id))
            .Select(c => c.Id)
            .ToArrayAsync();

        var cuentaIdsInseguras = cuentaIdsSolicitadas.Except(cuentaIdsEliminables).ToArray();
        if (cuentaIdsInseguras.Length > 0)
        {
            throw new InvalidOperationException(
                "No se puede eliminar un registro de empleado vinculado a una cuenta de otro tipo.");
        }

        if (cuentaIdsEliminables.Length > 0)
        {
            await _context.RefreshTokens
                .Where(t => cuentaIdsEliminables.Contains(t.CuentaId))
                .ExecuteDeleteAsync();
            await _context.PasswordResetTokens
                .Where(t => cuentaIdsEliminables.Contains(t.CuentaId))
                .ExecuteDeleteAsync();
            await _context.PerfilEditGrants
                .Where(g => cuentaIdsEliminables.Contains(g.CuentaId))
                .ExecuteDeleteAsync();
        }

        _context.Empleados.RemoveRange(eliminados);
        await _context.Empleados.AddRangeAsync(empleadosNuevos);
        permiso.Usado = true;

        await _context.SaveChangesAsync();
        if (cuentaIdsEliminables.Length > 0)
        {
            await _context.Cuenta
                .Where(c => cuentaIdsEliminables.Contains(c.Id))
                .ExecuteDeleteAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task GuardarCambiosUsuarioAsync()
    {
        await _context.SaveChangesAsync();
    }
}
