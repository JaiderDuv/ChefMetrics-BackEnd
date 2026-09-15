using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<LoginResponseDto> IniciarSesionAsync(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CorreoElectronico) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "El correo electrónico y la contraseña son obligatorios."
                };
            }

            var usuario = await _userRepository.ObtenerPorEmailAsync(dto.CorreoElectronico);
            if (usuario == null)
            {
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            bool passwordValida = PasswordHasher.VerifyPassword(dto.Password, usuario.HashContrasena);
            if (!passwordValida)
            {
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            string tokenSesion = TokenGenerator.GenerarTokenRecuperacion();

            return new LoginResponseDto
            {
                Exito = true,
                Mensaje = "Inicio de sesión exitoso.",
                Token = tokenSesion,
                IdUsuario = usuario.IdUsuarioEmprendedor,
                CorreoElectronico = usuario.CorreoElectronico,
                NombreEmprendimiento = usuario.NombreEmprendimiento
            };
        }

        public async Task<bool> SolicitarRecuperacionPasswordAsync(RecuperarPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CorreoElectronico))
            {
                throw new ArgumentException("El correo electrónico es obligatorio.");
            }

            var usuario = await _userRepository.ObtenerPorEmailAsync(dto.CorreoElectronico);
            if (usuario == null)
            {
                // Por seguridad no revelamos si el correo existe o no, o indicamos false si se prefiere
                return false;
            }

            string tokenRecuperacion = TokenGenerator.GenerarTokenRecuperacion();
            // En un flujo de producción aquí se enviaría el correo electrónico con el link de recuperación
            return true;
        }

        public async Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto)
        {
            if (dto.NuevaPassword != dto.ConfirmarPassword)
            {
                throw new ArgumentException("Las contraseñas no coinciden.");
            }

            var usuario = await _userRepository.ObtenerPorEmailAsync(dto.CorreoElectronico);
            if (usuario == null)
            {
                throw new KeyNotFoundException("No se encontró un usuario asociado al correo especificado.");
            }

            string nuevoHash = PasswordHasher.HashPassword(dto.NuevaPassword);
            usuario.HashContrasena = nuevoHash;

            await _userRepository.ActualizarAsync(usuario);
            return true;
        }
    }
}
