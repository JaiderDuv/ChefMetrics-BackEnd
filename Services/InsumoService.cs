using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public sealed class InsumoService : IInsumoService
{
    // Valores permitidos por CK_Insumo_UnidadMedida (comparación sin
    // distinción de mayúsculas; se guarda en minúsculas).
    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "l", "ml", "oz", "lb", "unidad", "taza"
    };

    // Valores permitidos por CK_Insumo_Categoria. Vacío => "otro".
    private static readonly HashSet<string> CategoriasValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "materia_prima", "empaque", "otro"
    };

    private const int TamanoPaginaMaximo = 100;

    private readonly IInsumoRepository _insumoRepository;

    public InsumoService(IInsumoRepository insumoRepository)
    {
        _insumoRepository = insumoRepository;
    }

    public async Task<ResultadoInsumoDto> CrearAsync(int cuentaId, CrearInsumoDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return ResultadoInsumoDto.NoHallado(
                "Aún no tienes un negocio registrado para administrar insumos.");
        }

        var nombre = dto.Nombre.Trim();
        var categoria = NormalizarCategoria(dto.Categoria);
        if (categoria is null)
        {
            return ResultadoInsumoDto.Error(
                "La categoría no es válida. Valores permitidos: materia_prima, empaque, otro.");
        }

        var validacion = ValidarCampos(
            nombre,
            dto.UnidadMedida,
            dto.CostoUnitario,
            dto.PorcentajeMerma,
            dto.Proveedor,
            out var unidadNormalizada,
            out var proveedorNormalizado);
        if (validacion is not null)
        {
            return ResultadoInsumoDto.Error(validacion);
        }

        if (await _insumoRepository.ExisteNombreAsync(negocioId.Value, nombre))
        {
            return ResultadoInsumoDto.Error("Ya existe un insumo con ese nombre en tu negocio.");
        }

        try
        {
            var insumo = await _insumoRepository.CrearAsync(new Insumo
            {
                NegocioId = negocioId.Value,
                Nombre = nombre,
                UnidadMedida = unidadNormalizada!,
                Categoria = categoria,
                CostoUnitario = dto.CostoUnitario,
                PorcentajeMerma = dto.PorcentajeMerma,
                Proveedor = proveedorNormalizado,
                Activo = dto.Activo ?? true,
                FechaActualizacion = DateTime.UtcNow
            });

            // Si trae proveedor, se registra la asociación principal para que el
            // precio quede atado a ese proveedor desde el inicio.
            if (proveedorNormalizado is not null)
            {
                var proveedor = await _insumoRepository.ObtenerOCrearProveedorAsync(
                    negocioId.Value, proveedorNormalizado);
                await _insumoRepository.CrearInsumoProveedorAsync(new InsumoProveedor
                {
                    InsumoId = insumo.Id,
                    ProveedorId = proveedor.Id,
                    Costo = dto.CostoUnitario,
                    EsPrincipal = true,
                    FechaActualizacion = DateTime.UtcNow
                });
            }

            return ResultadoInsumoDto.Ok(Mapear(insumo, 0), "El insumo fue creado correctamente.");
        }
        catch (DbUpdateException)
        {
            // Carrera contra la restricción UQ_Insumo_NegocioNombre.
            return ResultadoInsumoDto.Error("Ya existe un insumo con ese nombre en tu negocio.");
        }
    }

    public async Task<ListaPaginadaDto<InsumoDto>?> ListarAsync(int cuentaId, FiltrosInsumoDto filtros)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return null;
        }

        var ordenarPor = (filtros.OrdenarPor ?? "nombre").Trim().ToLowerInvariant();
        if (ordenarPor is not ("nombre" or "costo" or "merma"))
        {
            ordenarPor = "nombre";
        }

        var descendente = string.Equals(
            filtros.Direccion?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

        var pagina = filtros.Pagina < 1 ? 1 : filtros.Pagina;
        var tamano = filtros.TamanoPagina < 1
            ? 50
            : Math.Min(filtros.TamanoPagina, TamanoPaginaMaximo);

        var (items, total) = await _insumoRepository.ListarAsync(
            negocioId.Value,
            filtros.Busqueda,
            filtros.Categoria,
            filtros.IncluirInactivos,
            ordenarPor,
            descendente,
            pagina,
            tamano);

        var conteos = await _insumoRepository.ContarRecetasPorInsumosAsync(items.Select(i => i.Id));

        return new ListaPaginadaDto<InsumoDto>
        {
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamano,
            Datos = items.Select(i => Mapear(i, conteos.GetValueOrDefault(i.Id))).ToList()
        };
    }

    public async Task<ResultadoInsumoDto> ObtenerPorIdAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return ResultadoInsumoDto.NoHallado(
                "Aún no tienes un negocio registrado para administrar insumos.");
        }

        var insumo = await _insumoRepository.ObtenerPorIdAsync(negocioId.Value, insumoId);
        if (insumo is null)
        {
            return ResultadoInsumoDto.NoHallado("No se encontró el insumo solicitado.");
        }

        var numeroRecetas = await _insumoRepository.ContarRecetasAsociadasAsync(insumo.Id);
        return ResultadoInsumoDto.Ok(Mapear(insumo, numeroRecetas), "Detalle del insumo.");
    }

    public async Task<ResultadoInsumoDto> ActualizarParcialAsync(
        int cuentaId, int insumoId, ActualizarInsumoDto dto)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return ResultadoInsumoDto.NoHallado(
                "Aún no tienes un negocio registrado para administrar insumos.");
        }

        var insumo = await _insumoRepository.ObtenerPorIdAsync(negocioId.Value, insumoId);
        if (insumo is null)
        {
            return ResultadoInsumoDto.NoHallado("No se encontró el insumo solicitado.");
        }

        var hayCambios = false;

        if (dto.Nombre is not null)
        {
            var nombre = dto.Nombre.Trim();
            if (nombre.Length == 0)
            {
                return ResultadoInsumoDto.Error("El nombre del insumo no puede estar vacío.");
            }
            if (nombre.Length > 150)
            {
                return ResultadoInsumoDto.Error("El nombre no puede superar los 150 caracteres.");
            }
            if (!string.Equals(insumo.Nombre, nombre, StringComparison.Ordinal) &&
                await _insumoRepository.ExisteNombreAsync(negocioId.Value, nombre, insumo.Id))
            {
                return ResultadoInsumoDto.Error("Ya existe otro insumo con ese nombre en tu negocio.");
            }
            insumo.Nombre = nombre;
            hayCambios = true;
        }

        if (dto.UnidadMedida is not null)
        {
            if (!TryNormalizarUnidad(dto.UnidadMedida, out var unidad))
            {
                return ResultadoInsumoDto.Error(
                    $"La unidad de medida no es válida. Valores permitidos: {string.Join(", ", UnidadesValidas.OrderBy(u => u))}.");
            }
            insumo.UnidadMedida = unidad!;
            hayCambios = true;
        }

        if (dto.Categoria is not null)
        {
            var categoria = NormalizarCategoria(dto.Categoria);
            if (categoria is null)
            {
                return ResultadoInsumoDto.Error(
                    "La categoría no es válida. Valores permitidos: materia_prima, empaque, otro.");
            }
            insumo.Categoria = categoria;
            hayCambios = true;
        }

        if (dto.PorcentajeMerma.HasValue)
        {
            if (dto.PorcentajeMerma.Value < 0 || dto.PorcentajeMerma.Value > 100)
            {
                return ResultadoInsumoDto.Error("El porcentaje de merma debe estar entre 0 y 100.");
            }
            insumo.PorcentajeMerma = dto.PorcentajeMerma.Value;
            hayCambios = true;
        }

        if (dto.CostoUnitario.HasValue)
        {
            if (dto.CostoUnitario.Value <= 0)
            {
                return ResultadoInsumoDto.Error("El costo unitario debe ser mayor a 0.");
            }

            // El precio vive atado a un proveedor (tablas Proveedor/InsumoProveedor):
            // para cambiar el precio es obligatorio seleccionar el proveedor
            // en la misma petición, y el cambio queda registrado en el historial.
            if (string.IsNullOrWhiteSpace(dto.Proveedor))
            {
                return ResultadoInsumoDto.Error(
                    "Para cambiar el precio debes seleccionar el proveedor del insumo en la misma solicitud.");
            }

            var nombreProveedor = dto.Proveedor.Trim();
            if (nombreProveedor.Length > 150)
            {
                return ResultadoInsumoDto.Error("El proveedor no puede superar los 150 caracteres.");
            }

            var proveedor = await _insumoRepository.ObtenerOCrearProveedorAsync(
                negocioId.Value, nombreProveedor);
            var asociacion = await _insumoRepository.ObtenerInsumoProveedorAsync(
                insumo.Id, proveedor.Id);

            if (asociacion is null)
            {
                asociacion = await _insumoRepository.CrearInsumoProveedorAsync(new InsumoProveedor
                {
                    InsumoId = insumo.Id,
                    ProveedorId = proveedor.Id,
                    Costo = dto.CostoUnitario.Value,
                    EsPrincipal = false,
                    FechaActualizacion = DateTime.UtcNow
                });
            }

            if (asociacion.Costo != dto.CostoUnitario.Value)
            {
                await _insumoRepository.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
                {
                    InsumoProveedorId = asociacion.Id,
                    CostoAnterior = asociacion.Costo,
                    CostoNuevo = dto.CostoUnitario.Value,
                    FechaCambio = DateTime.UtcNow
                });
                asociacion.Costo = dto.CostoUnitario.Value;
            }

            asociacion.FechaActualizacion = DateTime.UtcNow;

            // El proveedor del cambio pasa a ser el principal: el precio visible
            // del insumo siempre refleja el precio de su proveedor principal.
            if (!asociacion.EsPrincipal)
            {
                asociacion.EsPrincipal = true;
            }
            await _insumoRepository.DesmarcarPrincipalesAsync(insumo.Id, asociacion.Id);
            await _insumoRepository.GuardarCambiosAsync();

            insumo.CostoUnitario = dto.CostoUnitario.Value;
            insumo.Proveedor = nombreProveedor;
            hayCambios = true;
        }
        else if (dto.Proveedor is not null)
        {
            // Cambio de proveedor sin cambio de precio: se permite libremente.
            var proveedor = string.IsNullOrWhiteSpace(dto.Proveedor) ? null : dto.Proveedor.Trim();
            if (proveedor is not null && proveedor.Length > 150)
            {
                return ResultadoInsumoDto.Error("El proveedor no puede superar los 150 caracteres.");
            }
            insumo.Proveedor = proveedor;
            hayCambios = true;
        }

        if (dto.Activo.HasValue)
        {
            insumo.Activo = dto.Activo.Value;
            hayCambios = true;
        }

        if (!hayCambios)
        {
            return ResultadoInsumoDto.Error("Debes enviar al menos un campo para actualizar.");
        }

        insumo.FechaActualizacion = DateTime.UtcNow;

        try
        {
            // La entidad viene de una lectura AsNoTracking: se adjunta por Id.
            await _insumoRepository.ActualizarAsync(insumo);
        }
        catch (DbUpdateException)
        {
            return ResultadoInsumoDto.Error("Ya existe otro insumo con ese nombre en tu negocio.");
        }

        var numeroRecetas = await _insumoRepository.ContarRecetasAsociadasAsync(insumo.Id);
        return ResultadoInsumoDto.Ok(Mapear(insumo, numeroRecetas), "El insumo fue actualizado correctamente.");
    }

    public async Task<ResultadoEliminarInsumoDto> EliminarAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return new ResultadoEliminarInsumoDto
            {
                Exito = false,
                NoEncontrado = true,
                Mensaje = "Aún no tienes un negocio registrado para administrar insumos."
            };
        }

        var insumo = await _insumoRepository.ObtenerPorIdAsync(negocioId.Value, insumoId);
        if (insumo is null)
        {
            return new ResultadoEliminarInsumoDto
            {
                Exito = false,
                NoEncontrado = true,
                Mensaje = "No se encontró el insumo solicitado."
            };
        }

        var numeroRecetas = await _insumoRepository.ContarRecetasAsociadasAsync(insumo.Id);
        if (numeroRecetas > 0)
        {
            return new ResultadoEliminarInsumoDto
            {
                Exito = false,
                BloqueadoPorRecetas = true,
                NumeroRecetas = numeroRecetas,
                Mensaje = $"No se puede eliminar el insumo porque está asociado a {numeroRecetas} receta(s)."
            };
        }

        _insumoRepository.Eliminar(insumo);
        await _insumoRepository.EliminarPreciosAsync(insumo.Id);
        await _insumoRepository.GuardarCambiosAsync();

        return new ResultadoEliminarInsumoDto
        {
            Exito = true,
            Mensaje = "El insumo fue eliminado correctamente."
        };
    }

    public async Task<ResultadoHistorialDto> ObtenerHistorialPreciosAsync(int cuentaId, int insumoId)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return new ResultadoHistorialDto
            {
                Exito = false,
                NoEncontrado = true,
                Mensaje = "Aún no tienes un negocio registrado para administrar insumos."
            };
        }

        var insumo = await _insumoRepository.ObtenerPorIdAsync(negocioId.Value, insumoId);
        if (insumo is null)
        {
            return new ResultadoHistorialDto
            {
                Exito = false,
                NoEncontrado = true,
                Mensaje = "No se encontró el insumo solicitado."
            };
        }

        // Historial real: tabla HistorialPrecioInsumo (un registro por cambio
        // de precio, atado al proveedor). Reciente primero.
        var movimientos = await _insumoRepository.ListarHistorialPorInsumoAsync(insumo.Id);

        var entradas = movimientos.Select(h => MapearHistorial(
            h.FechaCambio,
            h.CostoNuevo,
            h.InsumoProveedor.Proveedor.Nombre,
            h.CostoAnterior)).ToList();

        return new ResultadoHistorialDto
        {
            Exito = true,
            Mensaje = "Historial de precios del insumo.",
            Historial = new HistorialPreciosInsumoDto
            {
                InsumoId = insumo.Id,
                Nombre = insumo.Nombre,
                ProveedorActual = insumo.Proveedor,
                PrecioActual = insumo.CostoUnitario,
                HistorialCompletoDisponible = true,
                Precios = entradas
            }
        };
    }

    private static HistorialPrecioDto MapearHistorial(
        DateTime fecha, decimal precio, string? proveedor, decimal costoAnterior)
    {
        decimal? variacion = costoAnterior == 0
            ? null
            : Math.Round((precio - costoAnterior) / costoAnterior * 100, 2);

        return new HistorialPrecioDto
        {
            Fecha = fecha,
            Precio = precio,
            Proveedor = proveedor,
            VariacionPorcentual = variacion,
            EsSubida = variacion is null || variacion == 0
                ? null
                : variacion > 0
        };
    }

    public async Task<ResultadoHistorialGeneralDto> ObtenerHistorialGeneralAsync(
        int cuentaId, string? nombre, string? proveedor)
    {
        var negocioId = await _insumoRepository.ObtenerNegocioIdPorCuentaAsync(cuentaId);
        if (!negocioId.HasValue)
        {
            return new ResultadoHistorialGeneralDto
            {
                Exito = false,
                NoEncontrado = true,
                Mensaje = "Aún no tienes un negocio registrado para administrar insumos."
            };
        }

        // Historial real: tabla HistorialPrecioInsumo. Reciente primero.
        var movimientos = await _insumoRepository.ListarHistorialGeneralAsync(
            negocioId.Value, nombre, proveedor);

        var entradas = movimientos.Select(h => new HistorialPrecioGeneralDto
        {
            InsumoId = h.InsumoProveedor.InsumoId,
            NombreInsumo = h.InsumoProveedor.Insumo.Nombre,
            Fecha = h.FechaCambio,
            Precio = h.CostoNuevo,
            Proveedor = h.InsumoProveedor.Proveedor.Nombre,
            VariacionPorcentual = h.CostoAnterior == 0
                ? null
                : Math.Round((h.CostoNuevo - h.CostoAnterior) / h.CostoAnterior * 100, 2),
            EsSubida = h.CostoAnterior == 0 || h.CostoNuevo == h.CostoAnterior
                ? null
                : h.CostoNuevo > h.CostoAnterior
        }).ToList();

        return new ResultadoHistorialGeneralDto
        {
            Exito = true,
            Mensaje = "Historial general de precios.",
            Historial = new HistorialGeneralDto
            {
                HistorialCompletoDisponible = true,
                Precios = entradas
            }
        };
    }

    // Validación compartida del POST. Null => válido.
    private static string? ValidarCampos(
        string nombre,
        string unidadMedida,
        decimal costoUnitario,
        decimal porcentajeMerma,
        string? proveedor,
        out string? unidadNormalizada,
        out string? proveedorNormalizado)
    {
        unidadNormalizada = null;
        proveedorNormalizado = string.IsNullOrWhiteSpace(proveedor) ? null : proveedor.Trim();

        if (nombre.Length == 0)
        {
            return "El nombre del insumo no puede estar vacío.";
        }
        if (nombre.Length > 150)
        {
            return "El nombre no puede superar los 150 caracteres.";
        }
        if (!TryNormalizarUnidad(unidadMedida, out unidadNormalizada))
        {
            return $"La unidad de medida no es válida. Valores permitidos: {string.Join(", ", UnidadesValidas.OrderBy(u => u))}.";
        }
        if (costoUnitario <= 0)
        {
            return "El costo unitario debe ser mayor a 0.";
        }
        if (porcentajeMerma < 0 || porcentajeMerma > 100)
        {
            return "El porcentaje de merma debe estar entre 0 y 100.";
        }
        if (proveedorNormalizado is not null && proveedorNormalizado.Length > 150)
        {
            return "El proveedor no puede superar los 150 caracteres.";
        }

        return null;
    }

    private static string? NormalizarCategoria(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "otro";
        }

        var candidato = valor.Trim().ToLowerInvariant();
        return CategoriasValidas.Contains(candidato) ? candidato : null;
    }

    private static bool TryNormalizarUnidad(string? valor, out string? normalizada)
    {
        normalizada = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var candidato = valor.Trim().ToLowerInvariant();
        if (candidato.Length > 10 || !UnidadesValidas.Contains(candidato))
        {
            return false;
        }

        normalizada = candidato;
        return true;
    }

    private static InsumoDto Mapear(Insumo insumo, int numeroRecetas)
    {
        return new InsumoDto
        {
            Id = insumo.Id,
            Nombre = insumo.Nombre,
            UnidadMedida = insumo.UnidadMedida,
            Categoria = insumo.Categoria,
            CostoUnitario = insumo.CostoUnitario,
            PorcentajeMerma = insumo.PorcentajeMerma,
            Proveedor = insumo.Proveedor,
            Activo = insumo.Activo,
            FechaActualizacion = insumo.FechaActualizacion,
            NumeroRecetas = numeroRecetas
        };
    }
}
