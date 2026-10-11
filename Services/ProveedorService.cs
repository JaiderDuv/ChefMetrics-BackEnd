using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;

namespace ChefMetrics_BackEnd.Services;

public sealed class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _repository;

    public ProveedorService(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ResultadoProveedorDto> ListarProveedoresDelNegocioAsync(int cuentaId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }

        var proveedores = await _repository.ListarPorNegocioAsync(negocioId.Value);
        return ResultadoProveedorDto.Ok(proveedores.Select(MapearProveedor).ToList(), "Proveedores del negocio.");
    }

    public async Task<ResultadoProveedorDto> BuscarProveedoresAsync(int cuentaId, string termino, int limite)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }

        if (string.IsNullOrWhiteSpace(termino))
        {
            return ResultadoProveedorDto.Ok(new List<ProveedorDto>(), "Sin término de búsqueda.");
        }

        var top = limite < 1 ? 8 : Math.Min(limite, 20);
        var proveedores = await _repository.BuscarPorNombreAsync(negocioId.Value, termino, top);
        return ResultadoProveedorDto.Ok(proveedores.Select(MapearProveedor).ToList(), "Sugerencias de proveedores.");
    }

    public async Task<ResultadoProveedorDto> CrearProveedorAsync(int cuentaId, CrearProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }

        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre))
        {
            return ResultadoProveedorDto.Error("Ya existe un proveedor con ese nombre.");
        }

        var creado = await _repository.CrearAsync(new Proveedor
        {
            NegocioId = negocioId.Value,
            Nombre = dto.Nombre.Trim(),
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            FechaCreacion = DateTime.UtcNow
        });

        return ResultadoProveedorDto.Ok(MapearProveedor(creado), "Proveedor creado correctamente.");
    }

    public async Task<ResultadoProveedorDto> ListarPorInsumoAsync(int cuentaId, int insumoId)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var asociaciones = await _repository.ListarPorInsumoAsync(insumoId);
        var masBarato = asociaciones.Any() ? asociaciones.MinBy(a => a.Costo)!.ProveedorId : (int?)null;

        var filas = asociaciones.Select(a => MapearFila(a, masBarato)).ToList();
        return ResultadoProveedorDto.Ok(filas, "Proveedores del insumo.");
    }

    public async Task<ResultadoProveedorDto> ListarDisponiblesParaInsumoAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
        {
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");
        }

        var todos = await _repository.ListarPorNegocioAsync(negocioId.Value);
        var yaAsociados = (await _repository.ListarPorInsumoAsync(insumoId))
            .Select(a => a.ProveedorId).ToHashSet();

        var disponibles = todos.Where(p => !yaAsociados.Contains(p.Id)).Select(MapearProveedor).ToList();
        return ResultadoProveedorDto.Ok(disponibles, "Proveedores disponibles para asociar.");
    }

    public async Task<ResultadoProveedorDto> AsociarAsync(int cuentaId, int insumoId, AsociarProveedorDto dto)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var proveedor = await _repository.ObtenerPorIdAsync(dto.ProveedorId);
        if (proveedor is null)
        {
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");
        }
        if (await _repository.ExisteAsociacionAsync(insumoId, dto.ProveedorId))
        {
            return ResultadoProveedorDto.Error("Este proveedor ya está asociado a este insumo.");
        }

        var asociacion = await _repository.AsociarAsync(insumoId, dto.ProveedorId, dto.Costo, dto.Cantidad);
        return ResultadoProveedorDto.Ok(MapearFilaConProveedor(asociacion, proveedor, asociacion.ProveedorId), "Proveedor asociado correctamente.");
    }

    public async Task<ResultadoProveedorDto> CrearYAsociarAsync(int cuentaId, int insumoId, CrearYAsociarProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
        {
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");
        }
        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre))
        {
            return ResultadoProveedorDto.Error("Ya existe un proveedor con ese nombre.");
        }

        var proveedor = await _repository.CrearAsync(new Proveedor
        {
            NegocioId = negocioId.Value,
            Nombre = dto.Nombre.Trim(),
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            FechaCreacion = DateTime.UtcNow
        });

        var asociacion = await _repository.AsociarAsync(insumoId, proveedor.Id, dto.Costo, dto.Cantidad);
        return ResultadoProveedorDto.Ok(MapearFilaConProveedor(asociacion, proveedor, proveedor.Id), "Proveedor creado y asociado correctamente.");
    }

    public async Task<ResultadoProveedorDto> DesasociarAsync(int cuentaId, int insumoId, int proveedorId)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var eliminado = await _repository.DesasociarAsync(insumoId, proveedorId);
        return eliminado
            ? ResultadoProveedorDto.Ok(true, "Proveedor desasociado correctamente.")
            : ResultadoProveedorDto.NoHallado("Esa asociación no existe.");
    }

    public async Task<ResultadoProveedorDto> MarcarPrincipalAsync(int cuentaId, int insumoId, int proveedorId)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var resultado = await _repository.MarcarPrincipalAsync(insumoId, proveedorId);
        return resultado
            ? ResultadoProveedorDto.Ok(true, "Proveedor marcado como principal.")
            : ResultadoProveedorDto.NoHallado("Esa asociación no existe.");
    }

    public async Task<ResultadoProveedorDto> ActualizarCostoAsync(int cuentaId, int insumoId, int proveedorId, decimal costo, decimal? cantidad = null)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var resultado = await _repository.ActualizarCostoAsync(insumoId, proveedorId, costo, cantidad);
        return resultado
            ? ResultadoProveedorDto.Ok(true, "Precio actualizado correctamente.")
            : ResultadoProveedorDto.NoHallado("Esa asociación no existe.");
    }

    private async Task<ResultadoProveedorDto?> ValidarInsumoDelNegocioAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
        {
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");
        }
        return null;
    }

    private static ProveedorDto MapearProveedor(Proveedor p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Telefono = p.Telefono,
        Correo = p.Correo
    };

    private static InsumoProveedorDto MapearFila(InsumoProveedor a, int? masBaratoId) => new()
    {
        ProveedorId = a.ProveedorId,
        Nombre = a.Proveedor.Nombre,
        Telefono = a.Proveedor.Telefono,
        Correo = a.Proveedor.Correo,
        Costo = a.Costo,
        EsPrincipal = a.EsPrincipal,
        EsMasBarato = a.ProveedorId == masBaratoId
    };

    private static InsumoProveedorDto MapearFilaConProveedor(InsumoProveedor a, Proveedor p, int? masBaratoId) => new()
    {
        ProveedorId = p.Id,
        Nombre = p.Nombre,
        Telefono = p.Telefono,
        Correo = p.Correo,
        Costo = a.Costo,
        EsPrincipal = a.EsPrincipal,
        EsMasBarato = p.Id == masBaratoId
    };

    public async Task<ResultadoProveedorDto> ActualizarProveedorAsync(int cuentaId, int proveedorId, ActualizarProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }

        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre, proveedorId))
        {
            return ResultadoProveedorDto.Error("Ya existe otro proveedor con ese nombre.");
        }

        var proveedor = await _repository.ObtenerPorIdDelNegocioAsync(negocioId.Value, proveedorId);
        if (proveedor is null)
        {
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");
        }

        var nombreAnterior = proveedor.Nombre;
        var nombreNuevo = dto.Nombre.Trim();
        var cambioNombre = !string.Equals(nombreAnterior, nombreNuevo, StringComparison.OrdinalIgnoreCase);

        proveedor.Nombre = nombreNuevo;
        proveedor.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        proveedor.Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim();

        await _repository.ActualizarAsync(proveedor);

        // Relación Proveedor -> Insumos: si cambió el nombre, se propaga al
        // campo Insumo.Proveedor (desnormalizado) de los insumos donde este
        // proveedor es el principal. Por Id, no por texto, para corregir
        // desincronizados previos.
        var mensaje = "Proveedor actualizado correctamente.";
        if (cambioNombre)
        {
            var afectados = await _repository.PropagarNombreAInsumosAsync(negocioId.Value, proveedorId, nombreNuevo);
            if (afectados > 0)
            {
                mensaje = $"Proveedor actualizado correctamente. Se actualizó el nombre en {afectados} insumo(s).";
            }
        }

        return ResultadoProveedorDto.Ok(MapearProveedor(proveedor), mensaje);
    }

    public async Task<ResultadoProveedorDto> EliminarProveedorAsync(int cuentaId, int proveedorId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
        {
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        }

        var proveedor = await _repository.ObtenerPorIdDelNegocioAsync(negocioId.Value, proveedorId);
        if (proveedor is null)
        {
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");
        }

        var asociaciones = await _repository.ContarAsociacionesAsync(proveedorId);
        if (asociaciones > 0)
        {
            return ResultadoProveedorDto.Error(
                $"No se puede eliminar: este proveedor está asociado a {asociaciones} insumo(s). Quítalo de esos insumos primero.");
        }

        _repository.Eliminar(proveedor);
        await _repository.GuardarCambiosAsync();
        return ResultadoProveedorDto.Ok(true, "Proveedor eliminado correctamente.");
    }
}