using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUserRepository _usuarioRepository;
        private readonly IEmailService _emailService;

        public UsuarioService(IUserRepository usuarioRepository, IEmailService emailService)
        {
            _usuarioRepository = usuarioRepository;
            _emailService = emailService;
        }

        public async Task<bool> RegistrarEmprendedorFormalizadoAsync(EmprendedorFormalizado dto)
        {
            await RegistrarEmprendedorInternoAsync(
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
            string correo,
            string passwordPlano,
            string tipoFormalizacion,
            string? nit,
            string? razonSocial,
            string tipoDocumento,
            string numeroDocumento,
            string nombreEmprendimiento,
            string categoriaNegocio,
            string telefonoContacto,
            string correoContacto)
        {
            if (await _usuarioRepository.ExisteCorreoAsync(correo))
            {
                throw new Exception("El correo electrónico ya se encuentra registrado.");
            }

            var cuenta = await _usuarioRepository.RegistrarCuentaAsync(new Cuenta
            {
                Correo = correo,
                PasswordHash = PasswordHasher.HashPassword(passwordPlano),
                TipoCuenta = "emprendedor",
                EmailVerificado = false,
                Activo = false
            });

            var usuario = await _usuarioRepository.RegistrarUsuarioAsync(new UsuarioEmprendedor
            {
                CuentaId = cuenta.Id,
                TipoFormalizacion = tipoFormalizacion,
                Nit = nit,
                RazonSocial = razonSocial,
                TipoDocumento = tipoDocumento,
                NumeroDocumento = numeroDocumento,
                NombreEmprendimiento = nombreEmprendimiento,
                OnboardingCompletado = false
            });

            var tipoEmprendimiento = await _usuarioRepository
                .ObtenerOCrearTipoEmprendimientoAsync(categoriaNegocio);

            await _usuarioRepository.RegistrarNegocioAsync(new Negocio
            {
                UsuarioId = usuario.Id,
                TipoEmprendimientoId = tipoEmprendimiento.Id,
                TelefonoContacto = telefonoContacto,
                CorreoContacto = correoContacto,
                FechaActualizacion = DateTime.UtcNow
            });

            string token = TokenGenerator.GenerarTokenVerificacion();
            await _usuarioRepository.RegistrarPasswordResetTokenAsync(new PasswordResetToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(token),
                FechaExpiracion = DateTime.UtcNow.AddHours(24),
                Usado = false
            });
            await _emailService.EnviarCorreoVerificacionAsync(cuenta.Correo, token);
        }

        public async Task<bool> ValidarTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var registro = await _usuarioRepository.ObtenerPasswordResetPorHashAsync(
                TokenGenerator.CalcularHash(token));

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
