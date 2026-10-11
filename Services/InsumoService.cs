using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public sealed class InsumoService : IInsumoService
{
    private static readonly HashSet<string> UnidadesValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "l", "ml", "oz", "lb", "unidad", "taza"
    };

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

        if (dto.Cantidad.HasValue && dto.Cantidad.Value < 0)
        {
            return ResultadoInsumoDto.Error("La cantidad no puede ser negativa.");
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
                Cantidad = dto.Cantidad,
                Activo = dto.Activo ?? true,
                FechaActualizacion = DateTime.UtcNow
            });

            if (proveedorNormalizado is not null)
            {
                var proveedor = await _insumoRepository.ObtenerOCrearProveedorAsync(
                    negocioId.Value, proveedorNormalizado);
                var asociacion = await _insumoRepository.CrearInsumoProveedorAsync(new InsumoProveedor
                {
                    InsumoId = insumo.Id,
                    ProveedorId = proveedor.Id,
                    Costo = dto.CostoUnitario,
                    EsPrincipal = true,
                    FechaActualizacion = DateTime.UtcNow
                });
                await _insumoRepository.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
                {
                    InsumoProveedorId = asociacion.Id,
                    CostoAnterior = dto.CostoUnitario,
                    CostoNuevo = dto.CostoUnitario,
                    Cantidad = dto.Cantidad,
                    FechaCambio = DateTime.UtcNow
                });
            }

            return ResultadoInsumoDto.Ok(Mapear(insumo, 0, proveedorNormalizado), "El insumo fue creado correctamente.");
        }
        catch (DbUpdateException)
        {
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
        if (ordenarPor is not ("nombre" or "costo" or "merma" or "cantidad"))
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
        var principales = await _insumoRepository.ObtenerNombresProveedoresPrincipalesAsync(items.Select(i => i.Id));

        return new ListaPaginadaDto<InsumoDto>
        {
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamano,
            Datos = items.Select(i => Mapear(i, conteos.GetValueOrDefault(i.Id), principales.GetValueOrDefault(i.Id))).ToList()
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
        var principal = await _insumoRepository.ObtenerNombreProveedorPrincipalAsync(insumo.Id);
        return ResultadoInsumoDto.Ok(Mapear(insumo, numeroRecetas, principal), "Detalle del insumo.");
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

        if (dto.Cantidad.HasValue)
        {
            if (dto.Cantidad.Value < 0)
            {
                return ResultadoInsumoDto.Error("La cantidad no puede ser negativa.");
            }
            insumo.Cantidad = dto.Cantidad.Value;
            hayCambios = true;
        }

        if (dto.CostoUnitario.HasValue)
        {
            if (dto.CostoUnitario.Value <= 0)
            {
                return ResultadoInsumoDto.Error("El costo unitario debe ser mayor a 0.");
            }

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

                await _insumoRepository.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
                {
                    InsumoProveedorId = asociacion.Id,
                    CostoAnterior = dto.CostoUnitario.Value,
                    CostoNuevo = dto.CostoUnitario.Value,
                    Cantidad = dto.Cantidad ?? insumo.Cantidad,
                    FechaCambio = DateTime.UtcNow
                });
            }

            if (asociacion.Costo != dto.CostoUnitario.Value)
            {
                await _insumoRepository.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
                {
                    InsumoProveedorId = asociacion.Id,
                    CostoAnterior = asociacion.Costo,
                    CostoNuevo = dto.CostoUnitario.Value,
                    Cantidad = dto.Cantidad ?? insumo.Cantidad,
                    FechaCambio = DateTime.UtcNow
                });
                asociacion.Costo = dto.CostoUnitario.Value;
            }

            asociacion.FechaActualizacion = DateTime.UtcNow;

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
            var texto = string.IsNullOrWhiteSpace(dto.Proveedor) ? null : dto.Proveedor.Trim();
            if (texto is not null && texto.Length > 150)
            {
                return ResultadoInsumoDto.Error("El proveedor no puede superar los 150 caracteres.");
            }

            var principalActual = await _insumoRepository.ObtenerInsumoProveedorPrincipalAsync(insumo.Id);
            var nombrePrincipalActual = principalActual?.Proveedor.Nombre;

            if (!string.Equals(nombrePrincipalActual ?? insumo.Proveedor, texto, StringComparison.OrdinalIgnoreCase))
            {
                if (texto is null)
                {
                    await _insumoRepository.DesmarcarPrincipalesAsync(insumo.Id, -1);
                }
                else
                {
                    var proveedorEnt = await _insumoRepository.ObtenerOCrearProveedorAsync(negocioId.Value, texto);
                    var asociacion = await _insumoRepository.ObtenerInsumoProveedorAsync(insumo.Id, proveedorEnt.Id);
                    if (asociacion is null)
                    {
                        asociacion = await _insumoRepository.CrearInsumoProveedorAsync(new InsumoProveedor
                        {
                            InsumoId = insumo.Id,
                            ProveedorId = proveedorEnt.Id,
                            Costo = insumo.CostoUnitario,
                            EsPrincipal = false,
                            FechaActualizacion = DateTime.UtcNow
                        });
                        await _insumoRepository.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
                        {
                            InsumoProveedorId = asociacion.Id,
                            CostoAnterior = insumo.CostoUnitario,
                            CostoNuevo = insumo.CostoUnitario,
                            Cantidad = insumo.Cantidad,
                            FechaCambio = DateTime.UtcNow
                        });
                    }

                    if (!asociacion.EsPrincipal)
                    {
                        asociacion.EsPrincipal = true;
                    }
                    asociacion.FechaActualizacion = DateTime.UtcNow;
                    await _insumoRepository.DesmarcarPrincipalesAsync(insumo.Id, asociacion.Id);
                    await _insumoRepository.GuardarCambiosAsync();
                }

                insumo.Proveedor = texto;
                hayCambios = true;
            }
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
            await _insumoRepository.ActualizarAsync(insumo);
        }
        catch (DbUpdateException)
        {
            return ResultadoInsumoDto.Error("Ya existe otro insumo con ese nombre en tu negocio.");
        }

        var numeroRecetas = await _insumoRepository.ContarRecetasAsociadasAsync(insumo.Id);
        var nombrePrincipal = await _insumoRepository.ObtenerNombreProveedorPrincipalAsync(insumo.Id);
        return ResultadoInsumoDto.Ok(Mapear(insumo, numeroRecetas, nombrePrincipal), "El insumo fue actualizado correctamente.");
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

        var movimientos = await _insumoRepository.ListarHistorialPorInsumoAsync(insumo.Id);
        var anteriores = EmparejarAnteriores(movimientos);

        var entradas = movimientos.Select(h => MapearHistorial(
            h.FechaCambio,
            h.CostoNuevo,
            h.InsumoProveedor.Proveedor.Nombre,
            h.CostoAnterior,
            h.Cantidad,
            anteriores.GetValueOrDefault(h.Id))).ToList();

        var proveedorActual = await _insumoRepository.ObtenerNombreProveedorPrincipalAsync(insumo.Id);

        return new ResultadoHistorialDto
        {
            Exito = true,
            Mensaje = "Historial de precios del insumo.",
            Historial = new HistorialPreciosInsumoDto
            {
                InsumoId = insumo.Id,
                Nombre = insumo.Nombre,
                ProveedorActual = proveedorActual,
                PrecioActual = insumo.CostoUnitario,
                HistorialCompletoDisponible = true,
                Precios = entradas
            }
        };
    }

    private static Dictionary<int, HistorialPrecioInsumo> EmparejarAnteriores(
        List<HistorialPrecioInsumo> movimientosRecientesPrimero)
    {
        var ultimoPorAsociacion = new Dictionary<int, HistorialPrecioInsumo>();
        var resultado = new Dictionary<int, HistorialPrecioInsumo>();
        for (var i = movimientosRecientesPrimero.Count - 1; i >= 0; i--)
        {
            var actual = movimientosRecientesPrimero[i];
            if (ultimoPorAsociacion.TryGetValue(actual.InsumoProveedorId, out var anterior))
            {
                resultado[actual.Id] = anterior;
            }
            ultimoPorAsociacion[actual.InsumoProveedorId] = actual;
        }
        return resultado;
    }

    private static decimal? PrecioUnitario(decimal costo, decimal? cantidad) =>
        cantidad is > 0 ? Math.Round(costo / cantidad.Value, 2) : null;

    private static HistorialPrecioDto MapearHistorial(
        DateTime fecha, decimal precio, string? proveedor, decimal costoAnterior,
        decimal? cantidad, HistorialPrecioInsumo? anterior)
    {
        var unitarioNuevo = PrecioUnitario(precio, cantidad);
        var unitarioAnterior = anterior is null
            ? null
            : PrecioUnitario(anterior.CostoNuevo, anterior.Cantidad);

        decimal? variacion;
        decimal? diferencia;
        if (unitarioNuevo.HasValue && unitarioAnterior.HasValue && unitarioAnterior.Value != 0)
        {
            variacion = Math.Round((unitarioNuevo.Value - unitarioAnterior.Value) / unitarioAnterior.Value * 100, 2);
            diferencia = unitarioNuevo.Value - unitarioAnterior.Value;
        }
        else
        {
            variacion = costoAnterior == 0
                ? null
                : Math.Round((precio - costoAnterior) / costoAnterior * 100, 2);
            diferencia = variacion is null ? null : precio - costoAnterior;
        }

        return new HistorialPrecioDto
        {
            Fecha = fecha,
            Precio = precio,
            Proveedor = proveedor,
            Cantidad = cantidad,
            PrecioUnitario = unitarioNuevo,
            VariacionPorcentual = variacion,
            Diferencia = diferencia,
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

        var movimientos = await _insumoRepository.ListarHistorialGeneralAsync(
            negocioId.Value, nombre, proveedor);
        var anteriores = EmparejarAnteriores(movimientos);

        var entradas = movimientos.Select(h =>
        {
            var unitarioNuevo = PrecioUnitario(h.CostoNuevo, h.Cantidad);
            var anterior = anteriores.GetValueOrDefault(h.Id);
            var unitarioAnterior = anterior is null
                ? null
                : PrecioUnitario(anterior.CostoNuevo, anterior.Cantidad);

            decimal? variacion;
            decimal? diferencia;
            if (unitarioNuevo.HasValue && unitarioAnterior.HasValue && unitarioAnterior.Value != 0)
            {
                variacion = Math.Round((unitarioNuevo.Value - unitarioAnterior.Value) / unitarioAnterior.Value * 100, 2);
                diferencia = unitarioNuevo.Value - unitarioAnterior.Value;
            }
            else
            {
                variacion = h.CostoAnterior == 0
                    ? null
                    : Math.Round((h.CostoNuevo - h.CostoAnterior) / h.CostoAnterior * 100, 2);
                diferencia = h.CostoAnterior == 0
                    ? null
                    : h.CostoNuevo - h.CostoAnterior;
            }

            var esSubida = unitarioNuevo.HasValue && unitarioAnterior.HasValue
                ? (unitarioNuevo.Value == unitarioAnterior.Value ? null : (bool?)(unitarioNuevo.Value > unitarioAnterior.Value))
                : (h.CostoAnterior == 0 || h.CostoNuevo == h.CostoAnterior
                    ? null
                    : (bool?)(h.CostoNuevo > h.CostoAnterior));

            return new HistorialPrecioGeneralDto
            {
                InsumoId = h.InsumoProveedor.InsumoId,
                NombreInsumo = h.InsumoProveedor.Insumo.Nombre,
                Fecha = h.FechaCambio,
                Precio = h.CostoNuevo,
                Proveedor = h.InsumoProveedor.Proveedor.Nombre,
                Cantidad = h.Cantidad,
                PrecioUnitario = unitarioNuevo,
                VariacionPorcentual = variacion,
                Diferencia = diferencia,
                EsSubida = esSubida
            };
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

    private static InsumoDto Mapear(Insumo insumo, int numeroRecetas, string? proveedorPrincipal = null)
    {
        return new InsumoDto
        {
            Id = insumo.Id,
            Nombre = insumo.Nombre,
            UnidadMedida = insumo.UnidadMedida,
            Categoria = insumo.Categoria,
            CostoUnitario = insumo.CostoUnitario,
            PorcentajeMerma = insumo.PorcentajeMerma,
            Proveedor = proveedorPrincipal,
            Cantidad = insumo.Cantidad,
            Activo = insumo.Activo,
            FechaActualizacion = insumo.FechaActualizacion,
            NumeroRecetas = numeroRecetas
        };
    }
}
