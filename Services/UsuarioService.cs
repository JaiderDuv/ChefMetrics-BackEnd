using System;
using System.Threading.Tasks;
using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model; // Contiene la clase ChefMetricsContext
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUserRepository _usuarioRepository;
        private readonly IEmailService _emailService;
        private readonly ChefMetricsContext _context; // Nombre exacto del DbContext

        public UsuarioService(
            ChefMetricsContext context,
            IUserRepository usuarioRepository,
            IEmailService emailService)
        {
            _context = context;
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
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
            // 1. Crear la Cuenta (Autenticación)
            var cuenta = new Cuenta
            {
                Correo = correoElectronico.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
                TipoCuenta = "Emprendedor",
                EmailVerificado = false,
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
                TipoDocumento = tipoDocumento,
                NumeroDocumento = numeroDocumento,
                NombreEmprendimiento = string.IsNullOrWhiteSpace(nombreEmprendimiento) ? null : nombreEmprendimiento,
                OnboardingCompletado = false,
                FechaCreacion = DateTime.UtcNow
            };

            _context.UsuarioEmprendedors.Add(emprendedor); // En el DbContext la propiedad se llama "UsuarioEmprendedors"
            await _context.SaveChangesAsync();

            // 3. Generar token y enviar correo de verificación
            string token = Guid.NewGuid().ToString();
            string nombreCompleto = $"{emprendedor.Nombres} {emprendedor.Apellidos}";

            // Llama al servicio SMTP para enviar el correo
            await _emailService.EnviarCorreoVerificacionAsync(cuenta.Correo, nombreCompleto, token);
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

            string nuevoToken = Guid.NewGuid().ToString();

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
            string tokenRecuperacion = Guid.NewGuid().ToString();

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