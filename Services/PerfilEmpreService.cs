using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services;

public sealed class PerfilEmpreService : IPerfilEmpreService
{
    private static readonly TimeSpan DuracionPermisoEdicion = TimeSpan.FromMinutes(5);

    private readonly IUserRepository _userRepository;
    private readonly IPerfilEmprendimientoRepository _perfilRepository;
    private readonly IEmailService _emailService;

    public PerfilEmpreService(
        IUserRepository userRepository,
        IPerfilEmprendimientoRepository perfilRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _perfilRepository = perfilRepository;
        _emailService = emailService;
    }

    public async Task<PermisoEdicionPerfilDto?> ConfirmarIdentidadAsync(
        int cuentaId,
        ConfirmarEdicionPerfilDto dto)
    {
        var cuenta = await _userRepository.ObtenerCuentaPorIdAsync(cuentaId);
        if (cuenta is null || !cuenta.Activo || !cuenta.EmailVerificado ||
            !PasswordHasher.VerifyPassword(dto.Password, cuenta.PasswordHash))
        {
            return null;
        }

        await _perfilRepository.RevocarPermisosEdicionAsync(cuentaId);

        var token = TokenGenerator.GenerarTokenEdicionPerfil();
        var expira = DateTime.UtcNow.Add(DuracionPermisoEdicion);
        await _perfilRepository.RegistrarPermisoEdicionAsync(new PerfilEditGrant
        {
            CuentaId = cuentaId,
            TokenHash = TokenGenerator.CalcularHash(token),
            FechaExpiracion = expira,
            Usado = false
        });

        return new PermisoEdicionPerfilDto
        {
            EditToken = token,
            ExpiresAt = expira
        };
    }

    public async Task<PerfilEmprendedorDto?> ObtenerPerfilAsync(
        int cuentaId,
        string editToken)
    {
        var permiso = await ObtenerPermisoValidoAsync(cuentaId, editToken);
        if (permiso is null)
        {
            return null;
        }

        var cuenta = await _perfilRepository.ObtenerPerfilCompletoAsync(cuentaId);
        return cuenta is null ? null : MapearPerfil(cuenta);
    }

    public async Task<ActualizarPerfilResultadoDto> ActualizarPerfilAsync(
        int cuentaId,
        string editToken,
        PerfilEmprendedorDto dto)
    {
        var permiso = await ObtenerPermisoValidoAsync(cuentaId, editToken);
        if (permiso is null)
        {
            return ErrorAutorizacion("La autorización de edición es inválida o ha expirado.");
        }

        var cuenta = await _perfilRepository.ObtenerPerfilCompletoAsync(cuentaId);
        if (cuenta?.UsuarioEmprendedor?.Negocio is null)
        {
            return Error("El perfil del emprendimiento todavía no está completo.");
        }

        var usuario = cuenta.UsuarioEmprendedor;
        var negocio = usuario.Negocio;
        var correo = dto.CorreoCuenta.Trim().ToLowerInvariant();
        var nit = dto.Formalizado ? NullIfWhiteSpace(dto.Nit) : null;
        var razonSocial = dto.Formalizado ? NullIfWhiteSpace(dto.RazonSocial) : null;
        var rues = dto.Formalizado ? NullIfWhiteSpace(dto.Rues) : null;
        var emailCambio = !string.Equals(cuenta.Correo, correo, StringComparison.OrdinalIgnoreCase);

        if (emailCambio && await _perfilRepository.ExisteCorreoAsync(correo, cuentaId))
        {
            return Error("El correo de la cuenta ya está registrado.");
        }

        if (dto.Formalizado && nit is not null && await _perfilRepository.ExisteNitAsync(nit, cuentaId))
        {
            return Error("El NIT ya está registrado.");
        }

        cuenta.Correo = correo;
        if (emailCambio)
        {
            cuenta.EmailVerificado = false;
            cuenta.Activo = false;
        }

        usuario.TipoFormalizacion = dto.Formalizado ? "formalizado" : "no_formalizado";
        usuario.NombreEmprendimiento = NullIfWhiteSpace(dto.NombreEmprendimiento);
        usuario.Nit = nit;
        usuario.RazonSocial = razonSocial;

        var tipoEmprendimiento = await _userRepository
            .ObtenerOCrearTipoEmprendimientoAsync(dto.CategoriaNegocio.Trim());

        negocio.TipoEmprendimientoId = tipoEmprendimiento.Id;
        negocio.CorreoContacto = dto.CorreoContacto.Trim().ToLowerInvariant();
        negocio.Rues = rues;
        negocio.CorreoEmpresa = dto.CorreoEmpresa.Trim().ToLowerInvariant();
        negocio.TieneSitioFisico = dto.TieneSitioFisico;
        negocio.Direccion = dto.TieneSitioFisico
            ? NullIfWhiteSpace(dto.Direccion)
            : null;
        negocio.FechaActualizacion = DateTime.UtcNow;

        var empleadosExistentes = negocio.Empleados.ToList();
        var idsRecibidos = dto.Empleados
            .Where(e => e.Id.HasValue && e.Id.Value > 0)
            .Select(e => e.Id!.Value)
            .ToHashSet();

        if (idsRecibidos.Count != dto.Empleados.Count(e => e.Id.HasValue && e.Id.Value > 0))
        {
            return Error("La lista de empleados contiene registros repetidos.");
        }

        var empleadoIdsInvalidos = idsRecibidos.Except(empleadosExistentes.Select(e => e.Id)).Any();
        if (empleadoIdsInvalidos)
        {
            return Error("Uno de los empleados no pertenece a la cuenta actual.");
        }

        var correosRepetidos = dto.Empleados
            .Select(e => e.CorreoInvitado.Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .ToList();
        if (correosRepetidos.Count != correosRepetidos.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return Error("No puedes registrar dos empleados con el mismo correo.");
        }

        var empleadosEliminados = empleadosExistentes
            .Where(e => !idsRecibidos.Contains(e.Id))
            .ToList();
        var empleadosNuevos = new List<Empleado>();

        foreach (var empleadoDto in dto.Empleados)
        {
            var correoEmpleado = empleadoDto.CorreoInvitado.Trim().ToLowerInvariant();

            if (!await _perfilRepository.ExisteRolAsync(empleadoDto.RolId))
            {
                return Error($"El rol seleccionado para {correoEmpleado} no es válido.");
            }

            if (empleadoDto.Id.HasValue && empleadoDto.Id.Value > 0)
            {
                var existente = empleadosExistentes.Single(e => e.Id == empleadoDto.Id.Value);
                if (await _perfilRepository.ExisteEmpleadoCorreoAsync(
                    negocio.Id, correoEmpleado, existente.Id))
                {
                    return Error($"El correo {correoEmpleado} ya está registrado en tu negocio.");
                }

                existente.CorreoInvitado = correoEmpleado;
                existente.RolId = empleadoDto.RolId;
            }
            else
            {
                if (await _perfilRepository.ExisteEmpleadoCorreoAsync(negocio.Id, correoEmpleado))
                {
                    return Error($"El correo {correoEmpleado} ya está registrado en tu negocio.");
                }

                empleadosNuevos.Add(new Empleado
                {
                    NegocioId = negocio.Id,
                    RolId = empleadoDto.RolId,
                    CorreoInvitado = correoEmpleado,
                    EstadoInvitacion = "pendiente",
                    InvitadoPor = usuario.Id,
                    FechaInvitacion = DateTime.UtcNow
                });
            }
        }

        await _perfilRepository.GuardarEmpleadosAsync(
            permiso,
            empleadosNuevos,
            empleadosEliminados);

        if (emailCambio)
        {
            await EnviarConfirmacionNuevoCorreoAsync(cuenta);

            return new ActualizarPerfilResultadoDto
            {
                Exito = true,
                RequiereConfirmacionCorreo = true,
                Mensaje = "El correo cambió. Debes confirmarlo antes de volver a iniciar sesión."
            };
        }

        return new ActualizarPerfilResultadoDto
        {
            Exito = true,
            Mensaje = "El perfil fue actualizado correctamente."
        };
    }

    public async Task<PerfilUsuarioDto?> ObtenerPerfilUsuarioAsync(int cuentaId)
    {
        var cuenta = await _perfilRepository.ObtenerPerfilCompletoAsync(cuentaId);
        if (cuenta?.UsuarioEmprendedor is null)
        {
            return null;
        }

        var usuario = cuenta.UsuarioEmprendedor;
        return new PerfilUsuarioDto
        {
            FotoPerfilUrl = usuario.FotoPerfilUrl,
            TipoDocumento = usuario.TipoDocumento ?? string.Empty,
            NumeroDocumento = usuario.NumeroDocumento ?? string.Empty,
            TelefonoContacto = usuario.Negocio?.TelefonoContacto ?? string.Empty,
            Nombres = usuario.Nombres ?? string.Empty,
            Apellidos = usuario.Apellidos ?? string.Empty
        };
    }

    public async Task<ActualizarPerfilResultadoDto> ActualizarPerfilUsuarioAsync(
        int cuentaId,
        PerfilUsuarioDto dto)
    {
        var cuenta = await _perfilRepository.ObtenerPerfilCompletoAsync(cuentaId);
        if (cuenta?.UsuarioEmprendedor?.Negocio is null)
        {
            return Error("El perfil del usuario todavía no está completo.");
        }

        var usuario = cuenta.UsuarioEmprendedor;
        var negocio = usuario.Negocio;

        usuario.TipoDocumento = NullIfWhiteSpace(dto.TipoDocumento)?.ToUpperInvariant();
        usuario.NumeroDocumento = NullIfWhiteSpace(dto.NumeroDocumento);
        usuario.Nombres = NullIfWhiteSpace(dto.Nombres);
        usuario.Apellidos = NullIfWhiteSpace(dto.Apellidos);
        usuario.FotoPerfilUrl = NullIfWhiteSpace(dto.FotoPerfilUrl);
        negocio.TelefonoContacto = NullIfWhiteSpace(dto.TelefonoContacto) ?? string.Empty;
        negocio.FechaActualizacion = DateTime.UtcNow;

        try
        {
            await _perfilRepository.GuardarCambiosUsuarioAsync();
        }
        catch (DbUpdateException)
        {
            return Error("El documento de identidad ya está registrado por otro usuario.");
        }

        return new ActualizarPerfilResultadoDto
        {
            Exito = true,
            Mensaje = "El perfil fue actualizado correctamente."
        };
    }

    private async Task<PerfilEditGrant?> ObtenerPermisoValidoAsync(
        int cuentaId,
        string editToken)
    {
        if (string.IsNullOrWhiteSpace(editToken))
        {
            return null;
        }

        var permiso = await _perfilRepository.ObtenerPermisoEdicionAsync(
            TokenGenerator.CalcularHash(editToken));

        return permiso is not null &&
            permiso.CuentaId == cuentaId &&
            !permiso.Usado &&
            permiso.FechaExpiracion > DateTime.UtcNow
            ? permiso
            : null;
    }

    private async Task EnviarConfirmacionNuevoCorreoAsync(Cuenta cuenta)
    {
        var token = TokenGenerator.GenerarTokenVerificacion();
        await _userRepository.RegistrarPasswordResetTokenAsync(new PasswordResetToken
        {
            CuentaId = cuenta.Id,
            TokenHash = TokenGenerator.CalcularHash(token),
            Tipo = TokenPurposes.EmailVerification,
            FechaExpiracion = DateTime.UtcNow.AddHours(24),
            Usado = false
        });
        await _emailService.EnviarCorreoVerificacionAsync(cuenta.Correo, token);
    }

    private static PerfilEmprendedorDto MapearPerfil(Cuenta cuenta)
    {
        var usuario = cuenta.UsuarioEmprendedor!;
        var negocio = usuario.Negocio!;

        return new PerfilEmprendedorDto
        {
            CorreoCuenta = cuenta.Correo,
            Formalizado = string.Equals(usuario.TipoFormalizacion, "formalizado", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(usuario.TipoFormalizacion, "si", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(usuario.TipoFormalizacion, "true", StringComparison.OrdinalIgnoreCase),
            NombreEmprendimiento = usuario.NombreEmprendimiento ?? string.Empty,
            CategoriaNegocio = negocio.TipoEmprendimiento?.Nombre ?? string.Empty,
            CorreoContacto = negocio.CorreoContacto,
            Nit = usuario.Nit ?? string.Empty,
            RazonSocial = usuario.RazonSocial ?? string.Empty,
            Rues = negocio.Rues ?? string.Empty,
            CorreoEmpresa = negocio.CorreoEmpresa ?? negocio.CorreoContacto,
            TieneSitioFisico = negocio.TieneSitioFisico,
            Direccion = negocio.Direccion ?? string.Empty,
            Empleados = negocio.Empleados
                .OrderBy(e => e.CorreoInvitado)
                .Select(e => new EmpleadoDto
                {
                    Id = e.Id,
                    CorreoInvitado = e.CorreoInvitado,
                    RolId = e.RolId,
                    NombreRol = e.Rol?.Nombre ?? string.Empty,
                    EstadoInvitacion = e.EstadoInvitacion
                }).ToList()
        };
    }

    public async Task<List<RolDto>> ObtenerRolesAsync()
    {
        var roles = await _perfilRepository.ObtenerRolesAsync();
        return roles.Select(r => new RolDto { Id = r.Id, Nombre = r.Nombre }).ToList();
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ActualizarPerfilResultadoDto Error(string mensaje)
    {
        return new ActualizarPerfilResultadoDto
        {
            Exito = false,
            Mensaje = mensaje
        };
    }

    private static ActualizarPerfilResultadoDto ErrorAutorizacion(string mensaje)
    {
        return new ActualizarPerfilResultadoDto
        {
            Exito = false,
            RequiereReautenticacion = true,
            Mensaje = mensaje
        };
    }

}
