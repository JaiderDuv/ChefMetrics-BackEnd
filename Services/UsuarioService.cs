using System;
using System.Threading.Tasks;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model; // Contiene la clase ChefMetricsContext
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChefMetrics_BackEnd.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUserRepository _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ChefMetricsContext _context; // Nombre exacto del DbContext
        private readonly IWebHostEnvironment _entorno;
        private readonly EmailSettings _emailSettings;

        // CK_UsuarioEmprendedor_TipoDocumento: solo CC, CE, TI o PA.
        private static readonly HashSet<string> TiposDocumentoValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "CC", "CE", "TI", "PA"
        };

        public UsuarioService(
            ChefMetricsContext context,
            IUserRepository usuarioRepository,
            IEmailService emailService,
            IWebHostEnvironment entorno,
            IOptions<EmailSettings> emailOptions)
        {
            _context = context;
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
            _entorno = entorno;
            _emailSettings = emailOptions.Value;
        }

        public async Task<bool> RegistrarEmprendedorFormalizadoAsync(EmprendedorFormalizado dto)
        {
            await RegistrarEmprendedorInternoAsync(
                dto.Nombres,
                dto.Apellidos,
                dto.CorreoElectronico,
                dto.HashContrasena,
                "formalizado",
                dto.Nit,
                dto.RazonSocial,
                dto.TipoDocumento,
                dto.DocumentoIdentidadUe,
                dto.NombreEmprendimiento,
                dto.CategoriaNegocio,
                dto.TelefonoContacto,
                dto.CorreoElectronicoContacto);

            return true;
        }

        public async Task<bool> RegistrarEmprendedorAsync(Emprendedor dto)
        {
            await RegistrarEmprendedorInternoAsync(
                dto.Nombres,
                dto.Apellidos,
                dto.CorreoElectronico,
                dto.HashContrasena,
                "no_formalizado",
                null,
                null,
                dto.TipoDocumento,
                dto.DocumentoIdentidadUe,
                dto.NombreEmprendimiento,
                dto.CategoriaNegocio,
                dto.TelefonoContacto,
                dto.CorreoElectronicoContacto);

            return true;
        }

        private async Task RegistrarEmprendedorInternoAsync(
            string nombres,
            string apellidos,
            string correoElectronico,
            string contrasena,
            string tipoFormalizacion,
            string? nit,
            string? razonSocial,
            string tipoDocumento,
            string numeroDocumento,
            string? nombreEmprendimiento,
            string categoriaNegocio,
            string telefonoContacto,
            string correoContacto)
        {
            var correoNormalizado = correoElectronico.ToLower().Trim();

            // En desarrollo, si no hay SMTP configurado la cuenta se verifica
            // automáticamente: así un clon nuevo puede registrarse y probar la
            // aplicación sin configurar una contraseña de Gmail.
            var smtpConfigurado =
                !string.IsNullOrWhiteSpace(_emailSettings.Host) &&
                !string.IsNullOrWhiteSpace(_emailSettings.Username) &&
                !string.IsNullOrWhiteSpace(_emailSettings.Password) &&
                !string.IsNullOrWhiteSpace(_emailSettings.From);
            var verificarAutomaticamente = _entorno.IsDevelopment() && !smtpConfigurado;

            // Evita exponer el error crudo de BD (DbUpdateException) cuando el
            // correo ya existe por un intento anterior.
            if (await _usuarioRepository.ExisteCorreoAsync(correoNormalizado))
            {
                throw new InvalidOperationException(
                    "Este correo electrónico ya está registrado. Revisa tu bandeja de entrada " +
                    "o solicita el reenvío del correo de verificación.");
            }

            // CK_UsuarioEmprendedor_Formalizacion: formalizado exige NIT y razón
            // social SIN documento; no formalizado exige documento SIN NIT/razón.
            var esFormalizado = string.Equals(
                tipoFormalizacion, "formalizado", StringComparison.OrdinalIgnoreCase);

            if (esFormalizado &&
                (string.IsNullOrWhiteSpace(nit) || string.IsNullOrWhiteSpace(razonSocial)))
            {
                throw new InvalidOperationException(
                    "El NIT y la razón social son obligatorios para el registro formalizado.");
            }

            string? tipoDocFinal = esFormalizado ? null : tipoDocumento;
            string? numeroDocFinal = esFormalizado ? null : numeroDocumento;

            if (!esFormalizado)
            {
                if (string.IsNullOrWhiteSpace(tipoDocFinal) ||
                    !TiposDocumentoValidos.Contains(tipoDocFinal.Trim()))
                {
                    throw new InvalidOperationException(
                        "El tipo de documento no es válido. Valores permitidos: CC, CE, TI, PA.");
                }
                if (string.IsNullOrWhiteSpace(numeroDocFinal))
                {
                    throw new InvalidOperationException("El número de documento es obligatorio.");
                }

                tipoDocFinal = tipoDocFinal.Trim().ToUpperInvariant();
                numeroDocFinal = numeroDocFinal.Trim();
            }

            var telefonoLimpio = telefonoContacto.Trim();
            if (telefonoLimpio.Length < 7 || telefonoLimpio.Length > 20)
            {
                throw new InvalidOperationException(
                    "El teléfono de contacto debe tener entre 7 y 20 caracteres.");
            }

            var correoContactoLimpio = correoContacto.Trim().ToLowerInvariant();

            // Todo el registro va en una transacción: si algo falla no quedan
            // Cuenta/Usuario/Negocio huérfanos. El correo se envía tras el commit.
            await using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
            // 1. Crear la Cuenta (Autenticación)
            var cuenta = new Cuenta
            {
                Correo = correoNormalizado,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
                TipoCuenta = "Emprendedor",
                EmailVerificado = verificarAutomaticamente,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Cuenta.Add(cuenta); // En el DbContext la propiedad se llama "Cuenta"
            await _context.SaveChangesAsync();

            // 2. Crear el UsuarioEmprendedor (Perfil y Datos de Negocio)
            var emprendedor = new UsuarioEmprendedor
            {
                CuentaId = cuenta.Id,
                Nombres = nombres.Trim(),
                Apellidos = apellidos.Trim(),
                TipoFormalizacion = tipoFormalizacion,
                Nit = nit,
                RazonSocial = razonSocial,
                TipoDocumento = tipoDocFinal,
                NumeroDocumento = numeroDocFinal,
                NombreEmprendimiento = string.IsNullOrWhiteSpace(nombreEmprendimiento) ? null : nombreEmprendimiento,
                OnboardingCompletado = false,
                FechaCreacion = DateTime.UtcNow
            };

                _context.UsuarioEmprendedors.Add(emprendedor); // En el DbContext la propiedad se llama "UsuarioEmprendedors"
                await _context.SaveChangesAsync();

                // 2b. Crear el Negocio con los datos del negocio capturados en el registro
                // (TelefonoContacto, CorreoContacto y CategoriaNegocio via TipoEmprendimiento)
                var tipoEmprendimiento = await _usuarioRepository
                    .ObtenerOCrearTipoEmprendimientoAsync(categoriaNegocio);

                await _usuarioRepository.RegistrarNegocioAsync(new Negocio
                {
                    UsuarioId = emprendedor.Id,
                    TipoEmprendimientoId = tipoEmprendimiento.Id,
                    TelefonoContacto = telefonoLimpio,
                    CorreoContacto = correoContactoLimpio,
                    FechaActualizacion = DateTime.UtcNow
                });

                await transaccion.CommitAsync();

                if (verificarAutomaticamente)
                {
                    return;
                }

                // 3. Generar token, persistirlo y enviar correo de verificación
                // (fuera de la transacción; sin esta fila el enlace nunca validaría)
            string nombreCompleto = $"{emprendedor.Nombres} {emprendedor.Apellidos}";

            string token = TokenGenerator.GenerarTokenVerificacion();
            await _usuarioRepository.RegistrarPasswordResetTokenAsync(new PasswordResetToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(token),
                Tipo = TokenPurposes.EmailVerification,
                Usado = false,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(15)
            });

            // Llama al servicio SMTP para enviar el correo
            await _emailService.EnviarCorreoVerificacionAsync(cuenta.Correo, nombreCompleto, token);
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ReenviarCorreoVerificacionAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            var cuenta = await _usuarioRepository.ObtenerCuentaPorEmailAsync(correo.Trim().ToLower());

            // Si la cuenta no existe o ya está verificada, omitimos el reenvío
            if (cuenta == null || cuenta.EmailVerificado)
            {
                return false;
            }

            var usuario = cuenta.UsuarioEmprendedor;
            string nombreCompleto = usuario != null
                ? $"{usuario.Nombres} {usuario.Apellidos}".Trim()
                : "Usuario";

            string nuevoToken = TokenGenerator.GenerarTokenVerificacion();

            var tokenModel = new PasswordResetToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(nuevoToken),
                Tipo = TokenPurposes.EmailVerification,
                Usado = false,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(15)
            };

            await _usuarioRepository.RegistrarPasswordResetTokenAsync(tokenModel);
            await _emailService.EnviarCorreoVerificacionAsync(cuenta.Correo, nombreCompleto, nuevoToken);

            return true;
        }
        public async Task<bool> SolicitarRecuperacionPasswordAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            // 1. Obtener la cuenta vinculada con el perfil UsuarioEmprendedor
            var cuenta = await _usuarioRepository.ObtenerCuentaPorEmailAsync(correo.Trim().ToLower());

            if (cuenta == null)
            {
                // Retornamos false internamente
                return false;
            }

            // 2. Extraer Nombres y Apellidos
            var usuario = cuenta.UsuarioEmprendedor;
            string nombreCompleto = usuario != null
                ? $"{usuario.Nombres} {usuario.Apellidos}".Trim()
                : "Usuario";

            // 3. Generar token de recuperación con 15 minutos de expiración
            string tokenRecuperacion = TokenGenerator.GenerarTokenRecuperacion();

            var tokenModel = new PasswordResetToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(tokenRecuperacion),
                Tipo = TokenPurposes.PasswordReset,
                Usado = false,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(15)
            };

            await _usuarioRepository.RegistrarPasswordResetTokenAsync(tokenModel);

            // 4. Enviar correo usando la firma que incluye el nombre completo
            await _emailService.EnviarCorreoRecuperacionAsync(cuenta.Correo, nombreCompleto, tokenRecuperacion);

            return true;
        }

        public async Task<bool> ValidarTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var registro = await _usuarioRepository.ObtenerTokenPorHashAsync(
                TokenGenerator.CalcularHash(token),
                TokenPurposes.EmailVerification);

            if (registro == null || registro.Usado ||
                registro.FechaExpiracion <= DateTime.UtcNow)
            {
                return false;
            }

            await _usuarioRepository.ConfirmarEmailPorTokenAsync(registro.Cuenta, registro);

            return true;
        }
    }
}