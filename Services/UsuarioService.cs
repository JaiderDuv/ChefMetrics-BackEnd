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
            if (await _usuarioRepository.ExisteCorreoAsync(dto.CorreoElectronico))
            {
                throw new Exception("El correo electrónico ya se encuentra registrado.");
            }

            string hashPassword = PasswordHasher.HashPassword(dto.HashContrasena);
            string token = TokenGenerator.GenerarTokenVerificacion();

            var nuevoUsuario = new UsuarioEmprendedor
            {
                Formalizado = dto.Formalizado,
                Nit = dto.Nit,
                RazonSocial = dto.RazonSocial,
                CorreoElectronico = dto.CorreoElectronico,
                TipoDocumento = dto.TipoDocumento,
                DocumentoIdentidadUe = dto.DocumentoIdentidadUe,
                NombreEmprendimiento = dto.NombreEmprendimiento,
                CategoriaNegocio = dto.CategoriaNegocio,
                TelefonoContacto = dto.TelefonoContacto,
                CorreoElectronicoContacto = dto.CorreoElectronicoContacto,
                HashContrasena = hashPassword
            };

            // d. Persistir en base de datos
            var resultado = await _usuarioRepository.RegistrarUsuarioAsync(nuevoUsuario);
            await _usuarioRepository.RegistrarRefreshTokenAsync(new RefreshToken
            {
                IdUsuarioEmprendedor = nuevoUsuario.IdUsuarioEmprendedor,
                TokenVerificacionHash = TokenGenerator.CalcularHash(token),
                TokenVerificacionExpira = DateTime.UtcNow.AddHours(24),
                CorreoVerificado = false
            });
            await _emailService.EnviarCorreoVerificacionAsync(nuevoUsuario.CorreoElectronico, token);
            return resultado != null;
        }

        public async Task<bool> RegistrarEmprendedorAsync(Emprendedor dto)
        {
            if (await _usuarioRepository.ExisteCorreoAsync(dto.CorreoElectronico))
            {
                throw new Exception("El correo electrónico ya se encuentra registrado.");
            }

            string hashPassword = PasswordHasher.HashPassword(dto.HashContrasena);
            string token = TokenGenerator.GenerarTokenVerificacion();

            var nuevoUsuario = new UsuarioEmprendedor
            {
                Formalizado = dto.Formalizado,
                Nit = null,
                RazonSocial = null,
                CorreoElectronico = dto.CorreoElectronico,
                TipoDocumento = dto.TipoDocumento,
                DocumentoIdentidadUe = dto.DocumentoIdentidadUe,
                NombreEmprendimiento = dto.NombreEmprendimiento,
                CategoriaNegocio = dto.CategoriaNegocio,
                TelefonoContacto = dto.TelefonoContacto,
                CorreoElectronicoContacto = dto.CorreoElectronicoContacto,
                HashContrasena = hashPassword
            };

            var resultado = await _usuarioRepository.RegistrarUsuarioAsync(nuevoUsuario);
            await _usuarioRepository.RegistrarRefreshTokenAsync(new RefreshToken
            {
                IdUsuarioEmprendedor = nuevoUsuario.IdUsuarioEmprendedor,
                TokenVerificacionHash = TokenGenerator.CalcularHash(token),
                TokenVerificacionExpira = DateTime.UtcNow.AddHours(24),
                CorreoVerificado = false
            });
            await _emailService.EnviarCorreoVerificacionAsync(nuevoUsuario.CorreoElectronico, token);
            return resultado != null;
        }

        public async Task<bool> ValidarTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var refreshToken = await _usuarioRepository.ObtenerPorTokenVerificacionHashAsync(
                TokenGenerator.CalcularHash(token));

            if (refreshToken == null || refreshToken.CorreoVerificado ||
                refreshToken.TokenVerificacionExpira <= DateTime.UtcNow)
            {
                return false;
            }

            refreshToken.CorreoVerificado = true;
            refreshToken.TokenVerificacionHash = null;
            refreshToken.TokenVerificacionExpira = null;
            await _usuarioRepository.ActualizarRefreshTokenAsync(refreshToken);

            return true;
        }
    }
}
