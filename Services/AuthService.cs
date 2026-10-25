using ChefMetrics.Contracts;
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
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly IMemoryCache _loginAttempts;

        private const int MaximoIntentosLogin = 3;
        private static readonly TimeSpan VentanaIntentos = TimeSpan.FromMinutes(15);

        public AuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            IOptions<JwtSettings> jwtOptions,
            IMemoryCache loginAttempts)
        {
            _userRepository = userRepository;
            _emailService = emailService;
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

            // 1. Verificar si la cuenta existe y está activa
            var cuenta = await _userRepository.ObtenerCuentaPorEmailAsync(email);
            if (cuenta == null || !cuenta.Activo)
            {
                RegistrarIntentoFallido(email);
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            // 2. Comprobar si la contraseña es correcta
            bool passwordValida = PasswordHasher.VerifyPassword(dto.Password, cuenta.PasswordHash);
            if (!passwordValida)
            {
                RegistrarIntentoFallido(email);
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas. Verifique su correo y contraseña."
                };
            }

            // 3. Comprobar verificación de email (únicamente tras validar la contraseña correcta)
            if (!cuenta.EmailVerificado)
            {
                return new LoginResponseDto
                {
                    Exito = false,
                    Mensaje = "Tu cuenta aún no ha sido verificada. Por favor, revisa tu correo electrónico para confirmar tu registro antes de iniciar sesión."
                };
            }

            // Si todo es correcto, reiniciamos intentos fallidos
            _loginAttempts.Remove(email);

            var usuario = cuenta.UsuarioEmprendedor;
            string accessToken = TokenGenerator.GenerarAccessToken(cuenta, usuario, _jwtSettings);
            string refreshToken = TokenGenerator.GenerarTokenRefresh();

            await _userRepository.RegistrarRefreshTokenAsync(new RefreshToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(refreshToken),
                FechaExpiracion = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                Revocado = false
            });

            return new LoginResponseDto
            {
                Exito = true,
                Mensaje = "Inicio de sesión exitoso.",
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IdUsuario = usuario?.Id ?? cuenta.Id,
                CorreoElectronico = cuenta.Correo,
                NombreEmprendimiento = usuario?.NombreEmprendimiento,
                OnboardingCompletado = usuario?.OnboardingCompletado ?? false
            };
        }

        public async Task<LoginResponseDto?> RenovarSesionAsync(RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return null;
            }

            var refreshToken = await _userRepository.ObtenerRefreshTokenPorHashAsync(
                TokenGenerator.CalcularHash(dto.Token));

            if (refreshToken == null ||
                refreshToken.Revocado ||
                refreshToken.FechaExpiracion <= DateTime.UtcNow ||
                !refreshToken.Cuenta.Activo ||
                !refreshToken.Cuenta.EmailVerificado)
            {
                return null;
            }

            var cuenta = refreshToken.Cuenta;
            var usuario = cuenta.UsuarioEmprendedor;
            var accessToken = TokenGenerator.GenerarAccessToken(cuenta, usuario, _jwtSettings);
            var nuevoRefreshToken = TokenGenerator.GenerarTokenRefresh();

            var rotado = await _userRepository.RotarRefreshTokenAsync(
                refreshToken,
                new RefreshToken
                {
                    CuentaId = cuenta.Id,
                    TokenHash = TokenGenerator.CalcularHash(nuevoRefreshToken),
                    FechaExpiracion = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                    Revocado = false
                });

            if (!rotado)
            {
                return null;
            }

            return new LoginResponseDto
            {
                Exito = true,
                Mensaje = "Sesión renovada.",
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = nuevoRefreshToken,
                IdUsuario = usuario?.Id ?? cuenta.Id,
                CorreoElectronico = cuenta.Correo,
                NombreEmprendimiento = usuario?.NombreEmprendimiento,
                OnboardingCompletado = usuario?.OnboardingCompletado ?? false
            };
        }

        public async Task<bool> CerrarSesionAsync(RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return false;
            }

            return await _userRepository.RevocarRefreshTokenAsync(
                TokenGenerator.CalcularHash(dto.Token));
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

            var email = dto.CorreoElectronico.Trim().ToLowerInvariant();
            var cuenta = await _userRepository.ObtenerCuentaPorEmailAsync(email);
            if (cuenta == null)
            {
                return false;
            }

            // 1. Extraer los nombres y apellidos del usuario
            var usuario = cuenta.UsuarioEmprendedor;
            string nombreCompleto = usuario != null
                ? $"{usuario.Nombres} {usuario.Apellidos}".Trim()
                : "Usuario";

            // 2. Generar y registrar el token
            string tokenRecuperacion = TokenGenerator.GenerarTokenRecuperacion();
            await _userRepository.RegistrarPasswordResetTokenAsync(new PasswordResetToken
            {
                CuentaId = cuenta.Id,
                TokenHash = TokenGenerator.CalcularHash(tokenRecuperacion),
                Tipo = TokenPurposes.PasswordReset,
                FechaExpiracion = DateTime.UtcNow.AddMinutes(15),
                Usado = false
            });

            // 3. Enviar el correo pasando el nombre completo obtenido
            await _emailService.EnviarCorreoRecuperacionAsync(email, nombreCompleto, tokenRecuperacion);

            return true;
        }

        public async Task<bool> RestablecerPasswordAsync(RestablecerPasswordDto dto)
        {
            if (dto.NuevaPassword != dto.ConfirmarPassword)
            {
                throw new ArgumentException("Las contraseñas no coinciden.");
            }

            var tokenRecuperacion = await _userRepository.ObtenerTokenPorHashAsync(
                TokenGenerator.CalcularHash(dto.Token),
                TokenPurposes.PasswordReset);

            if (tokenRecuperacion == null ||
                tokenRecuperacion.Usado ||
                tokenRecuperacion.FechaExpiracion <= DateTime.UtcNow)
            {
                throw new KeyNotFoundException("El token de recuperación es inválido o ha expirado.");
            }

            var cuenta = tokenRecuperacion.Cuenta;
            if (!string.Equals(cuenta.Correo, dto.CorreoElectronico,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new KeyNotFoundException("El token de recuperación no corresponde al usuario.");
            }

            string nuevoHash = PasswordHasher.HashPassword(dto.NuevaPassword);

            await _userRepository.RestablecerPasswordYRevocarSesionesAsync(
                cuenta, tokenRecuperacion, nuevoHash);
            return true;
        }
    }
}
