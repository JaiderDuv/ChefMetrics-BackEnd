using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

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
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");

        var proveedores = await _repository.ListarPorNegocioAsync(negocioId.Value);
        return ResultadoProveedorDto.Ok(proveedores.Select(MapearProveedor).ToList(), "Proveedores del negocio.");
    }

    public async Task<ResultadoProveedorDto> BuscarProveedoresAsync(int cuentaId, string termino, int limite)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");

        if (string.IsNullOrWhiteSpace(termino))
            return ResultadoProveedorDto.Ok(new List<ProveedorDto>(), "Sin término de búsqueda.");

        var top = limite < 1 ? 8 : Math.Min(limite, 20);
        var proveedores = await _repository.BuscarPorNombreAsync(negocioId.Value, termino, top);
        return ResultadoProveedorDto.Ok(proveedores.Select(MapearProveedor).ToList(), "Sugerencias de proveedores.");
    }

    public async Task<ResultadoProveedorDto> CrearProveedorAsync(int cuentaId, CrearProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");

        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre))
            return ResultadoProveedorDto.Error("Ya existe un proveedor con ese nombre.");

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

        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        var unidadInsumo = negocioId is null
            ? null
            : await _repository.ObtenerUnidadInsumoAsync(negocioId.Value, insumoId);

        var asociaciones = await _repository.ListarPorInsumoAsync(insumoId);

        // "más barato" compara el precio por unidad base del insumo, no el precio
        // total del paquete; paquetes en otra familia no se comparan.
        var masBaratoId = asociaciones
            .Where(a => a.Cantidad > 0)
            .Select(a => new
            {
                a.ProveedorId,
                Unidad = ConversorUnidades.PrecioPorUnidad(a.Costo, a.Cantidad, a.UnidadMedida, unidadInsumo ?? string.Empty)
            })
            .Where(x => x.Unidad.HasValue)
            .OrderBy(x => x.Unidad!.Value)
            .FirstOrDefault()?.ProveedorId;

        var filas = asociaciones
            .Select(a => MapearFila(a, masBaratoId, unidadInsumo))
            .ToList();
        return ResultadoProveedorDto.Ok(filas, "Proveedores del insumo.");
    }

    public async Task<ResultadoProveedorDto> ListarDisponiblesParaInsumoAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");

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
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");
        if (await _repository.ExisteAsociacionAsync(insumoId, dto.ProveedorId))
            return ResultadoProveedorDto.Error("Este proveedor ya está asociado a este insumo.");
        if (dto.Costo > 999999.99m)
            return ResultadoProveedorDto.Error("El costo no puede superar 999999.99 (6 enteros y 2 decimales).");

        var asociacion = await _repository.AsociarAsync(
            insumoId, dto.ProveedorId, dto.Costo, dto.Cantidad, dto.UnidadMedida);
        await _repository.SincronizarInsumoConPrincipalAsync(insumoId);
        var unidadInsumo = await UnidadInsumoActualAsync(cuentaId, insumoId);
        return ResultadoProveedorDto.Ok(
            MapearFilaConProveedor(asociacion, proveedor, asociacion.ProveedorId, unidadInsumo),
            "Proveedor asociado correctamente.");
    }

    public async Task<ResultadoProveedorDto> CrearYAsociarAsync(int cuentaId, int insumoId, CrearYAsociarProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");
        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre))
            return ResultadoProveedorDto.Error("Ya existe un proveedor con ese nombre.");
        if (dto.Costo > 999999.99m)
            return ResultadoProveedorDto.Error("El costo no puede superar 999999.99 (6 enteros y 2 decimales).");

        var proveedor = await _repository.CrearAsync(new Proveedor
        {
            NegocioId = negocioId.Value,
            Nombre = dto.Nombre.Trim(),
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            FechaCreacion = DateTime.UtcNow
        });

        var asociacion = await _repository.AsociarAsync(
            insumoId, proveedor.Id, dto.Costo, dto.Cantidad, dto.UnidadMedida);
        await _repository.SincronizarInsumoConPrincipalAsync(insumoId);
        var unidadInsumo = await UnidadInsumoActualAsync(cuentaId, insumoId);
        return ResultadoProveedorDto.Ok(
            MapearFilaConProveedor(asociacion, proveedor, proveedor.Id, unidadInsumo),
            "Proveedor creado y asociado correctamente.");
    }

    public async Task<ResultadoProveedorDto> DesasociarAsync(int cuentaId, int insumoId, int proveedorId)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var eliminado = await _repository.DesasociarAsync(insumoId, proveedorId);
        if (!eliminado)
            return ResultadoProveedorDto.NoHallado("Esa asociación no existe.");

        await _repository.SincronizarInsumoConPrincipalAsync(insumoId);
        return ResultadoProveedorDto.Ok(true, "Proveedor desasociado correctamente.");
    }

    public async Task<ResultadoProveedorDto> MarcarPrincipalAsync(int cuentaId, int insumoId, int proveedorId)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;

        var resultado = await _repository.MarcarPrincipalAsync(insumoId, proveedorId);
        if (!resultado)
            return ResultadoProveedorDto.NoHallado("Esa asociación no existe.");

        await _repository.SincronizarInsumoConPrincipalAsync(insumoId);
        return ResultadoProveedorDto.Ok(true, "Proveedor marcado como principal.");
    }

    public async Task<ResultadoProveedorDto> ActualizarCostoAsync(
        int cuentaId, int insumoId, int proveedorId, decimal costo, decimal? cantidad = null, string? unidadMedida = null)
    {
        var validacion = await ValidarInsumoDelNegocioAsync(cuentaId, insumoId);
        if (validacion != null) return validacion;
        if (costo > 999999.99m)
            return ResultadoProveedorDto.Error("El costo no puede superar 999999.99 (6 enteros y 2 decimales).");

        var resultado = await _repository.ActualizarCostoAsync(insumoId, proveedorId, costo, cantidad, unidadMedida);
        if (!resultado)
            return ResultadoProveedorDto.NoHallado("Esa asociación no existe.");

        await _repository.SincronizarInsumoConPrincipalAsync(insumoId);
        return ResultadoProveedorDto.Ok(true, "Precio actualizado correctamente.");
    }

    public async Task<ResultadoProveedorDto> ActualizarProveedorAsync(
        int cuentaId, int proveedorId, ActualizarProveedorDto dto)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");

        if (await _repository.ExisteNombreAsync(negocioId.Value, dto.Nombre, proveedorId))
            return ResultadoProveedorDto.Error("Ya existe otro proveedor con ese nombre.");

        var proveedor = await _repository.ObtenerPorIdDelNegocioAsync(negocioId.Value, proveedorId);
        if (proveedor is null)
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");

        var nombreAnterior = proveedor.Nombre;
        var nombreNuevo = dto.Nombre.Trim();
        var cambioNombre = !string.Equals(nombreAnterior, nombreNuevo, StringComparison.OrdinalIgnoreCase);

        proveedor.Nombre = nombreNuevo;
        proveedor.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        proveedor.Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim();

        await _repository.ActualizarAsync(proveedor);

        var mensaje = "Proveedor actualizado correctamente.";
        if (cambioNombre)
        {
            var afectados = await _repository.PropagarNombreAInsumosAsync(negocioId.Value, proveedorId, nombreNuevo);
            if (afectados > 0)
                mensaje = $"Proveedor actualizado correctamente. Se actualizó el nombre en {afectados} insumo(s).";
        }

        return ResultadoProveedorDto.Ok(MapearProveedor(proveedor), mensaje);
    }

    public async Task<ResultadoChequeoEliminarDto> ChequearEliminarProveedorAsync(int cuentaId, int proveedorId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoChequeoEliminarDto.NoHallado("Aún no tienes un negocio registrado.");

        var proveedor = await _repository.ObtenerPorIdDelNegocioAsync(negocioId.Value, proveedorId);
        if (proveedor is null)
            return ResultadoChequeoEliminarDto.NoHallado("El proveedor no existe.");

        var nombresInsumos = await _repository.ObtenerNombresInsumosAsociadosAsync(proveedorId);

        return ResultadoChequeoEliminarDto.Ok(new ChequeoEliminarProveedorDto
        {
            ProveedorId = proveedor.Id,
            Nombre = proveedor.Nombre,
            TieneInsumos = nombresInsumos.Count > 0,
            NombresInsumos = nombresInsumos
        });
    }

    public async Task<ResultadoProveedorDto> EliminarProveedorAsync(int cuentaId, int proveedorId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");

        var proveedor = await _repository.ObtenerPorIdDelNegocioAsync(negocioId.Value, proveedorId);
        if (proveedor is null)
            return ResultadoProveedorDto.NoHallado("El proveedor no existe.");

        // Elimina todas las asociaciones y su historial antes de eliminar el proveedor
        await _repository.EliminarAsociacionesDeProveedorAsync(proveedorId);

        _repository.Eliminar(proveedor);
        await _repository.GuardarCambiosAsync();
        return ResultadoProveedorDto.Ok(true, "Proveedor eliminado correctamente.");
    }

    private async Task<ResultadoProveedorDto?> ValidarInsumoDelNegocioAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (negocioId is null)
            return ResultadoProveedorDto.NoHallado("Aún no tienes un negocio registrado.");
        if (!await _repository.ExisteInsumoEnNegocioAsync(negocioId.Value, insumoId))
            return ResultadoProveedorDto.NoHallado("No se encontró el insumo solicitado.");
        return null;
    }

    private static ProveedorDto MapearProveedor(Proveedor p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Telefono = p.Telefono,
        Correo = p.Correo
    };

    private static InsumoProveedorDto MapearFila(InsumoProveedor a, int? masBaratoId, string? unidadInsumo) => new()
    {
        ProveedorId = a.ProveedorId,
        Nombre = a.Proveedor.Nombre,
        Telefono = a.Proveedor.Telefono,
        Correo = a.Proveedor.Correo,
        Costo = a.Costo,
        Cantidad = a.Cantidad,
        UnidadMedida = a.UnidadMedida,
        PrecioUnitario = ConversorUnidades.PrecioPorUnidad(a.Costo, a.Cantidad, a.UnidadMedida, unidadInsumo ?? string.Empty) is { } p
            ? Math.Round(p, 2) : (decimal?)null,
        EsPrincipal = a.EsPrincipal,
        EsMasBarato = a.ProveedorId == masBaratoId
    };

    private static InsumoProveedorDto MapearFilaConProveedor(InsumoProveedor a, Proveedor p, int? masBaratoId, string? unidadInsumo) => new()
    {
        ProveedorId = p.Id,
        Nombre = p.Nombre,
        Telefono = p.Telefono,
        Correo = p.Correo,
        Costo = a.Costo,
        Cantidad = a.Cantidad,
        UnidadMedida = a.UnidadMedida,
        PrecioUnitario = ConversorUnidades.PrecioPorUnidad(a.Costo, a.Cantidad, a.UnidadMedida, unidadInsumo ?? string.Empty) is { } pr
            ? Math.Round(pr, 2) : (decimal?)null,
        EsPrincipal = a.EsPrincipal,
        EsMasBarato = p.Id == masBaratoId
    };

    private async Task<string?> UnidadInsumoActualAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _repository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        return negocioId is null ? null : await _repository.ObtenerUnidadInsumoAsync(negocioId.Value, insumoId);
    }
}