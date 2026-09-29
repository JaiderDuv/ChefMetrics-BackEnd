using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Services;
using Microsoft.IdentityModel.Tokens;

namespace ChefMetrics_BackEnd.Utilities
{
    public static class TokenGenerator
    {
        public static string GenerarTokenRecuperacion()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static string GenerarTokenVerificacion()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static string GenerarTokenRefresh()
        {
            return GenerarTokenVerificacion();
        }

        public static string GenerarTokenEdicionPerfil()
        {
            return GenerarTokenVerificacion();
        }

        public static string GenerarAccessToken(Cuenta cuenta, UsuarioEmprendedor? usuario, JwtSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < 32)
            {
                throw new InvalidOperationException(
                    "La clave de firma JWT debe tener al menos 32 caracteres.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, cuenta.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, cuenta.Correo),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario?.NombreEmprendimiento ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string CalcularHash(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }
    }
}
