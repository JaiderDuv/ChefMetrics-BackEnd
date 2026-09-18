using ChefMetrics_BackEnd.Dtos;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ChefMetrics_BackEnd.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly IMemoryCache _loginAttempts;

        private const int MaximoIntentosLogin = 3;
        private static readonly TimeSpan VentanaIntentos = TimeSpan.FromMinutes(15);

        public AuthService(
            IUserRepository userRepository,
            IOptions<JwtSettings> jwtOptions,
            IMemoryCache loginAttempts)
        {
            _userRepository = userRepository;
            _jwtSettings = jwtOptions.Value;
            _loginAttempts = loginAttempts;
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

            var email = dto.CorreoElectronico.Trim().ToLowerInvariant();
            if (EstaBloqueado(email))
            {
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Se alcanzó el máximo de 3 intentos fallidos. Intente nuevamente más tarde."
                };
            }

            var usuario = await _userRepository.ObtenerPorEmailAsync(email);
            if (usuario == null)
            {
                RegistrarIntentoFallido(email);
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            bool passwordValida = PasswordHasher.VerifyPassword(dto.Password, usuario.HashContrasena);
            if (!passwordValida)
            {
                RegistrarIntentoFallido(email);
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            _loginAttempts.Remove(email);

            string accessToken = TokenGenerator.GenerarAccessToken(usuario, _jwtSettings);
            string refreshToken = TokenGenerator.GenerarTokenRefresh();

            await _userRepository.RegistrarRefreshTokenAsync(new RefreshToken
            {
                IdUsuarioEmprendedor = usuario.IdUsuarioEmprendedor,
                RefreshTokenHash = TokenGenerator.CalcularHash(refreshToken),
                RefreshTokenExpira = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                RefreshTokenRevocado = false
            });

            return new LoginResponseDto
            {
                Exito = true,
                Mensaje = "Inicio de sesión exitoso.",
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IdUsuario = usuario.IdUsuarioEmprendedor,
                CorreoElectronico = usuario.CorreoElectronico,
                NombreEmprendimiento = usuario.NombreEmprendimiento,
                OnboardingCompletado = usuario.OnboardingCompletado
            };
        }

        private bool EstaBloqueado(string email)
        {
            if (!_loginAttempts.TryGetValue<LoginAttemptState>(email, out var state) || state == null)
            {
                return false;
            }

            lock (state)
            {
                if (state.BloqueadoHasta.HasValue && state.BloqueadoHasta <= DateTime.UtcNow)
                {
                    _loginAttempts.Remove(email);
                    return false;
                }

                return state.IntentosFallidos >= MaximoIntentosLogin;
            }
        }

        private void RegistrarIntentoFallido(string email)
        {
            var state = _loginAttempts.GetOrCreate(email, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = VentanaIntentos;
                return new LoginAttemptState();
            }) ?? new LoginAttemptState();

            lock (state)
            {
                state.IntentosFallidos++;
                if (state.IntentosFallidos >= MaximoIntentosLogin)
                {
                    state.BloqueadoHasta = DateTime.UtcNow.Add(VentanaIntentos);
                    _loginAttempts.Set(email, state, state.BloqueadoHasta.Value);
                }
            }
        }

        private sealed class LoginAttemptState
        {
            public int IntentosFallidos { get; set; }
            public DateTime? BloqueadoHasta { get; set; }
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
                return false;
            }

            string tokenRecuperacion = TokenGenerator.GenerarTokenRecuperacion();
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
