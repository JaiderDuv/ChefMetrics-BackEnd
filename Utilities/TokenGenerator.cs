using System.Security.Cryptography;

namespace ChefMetrics_BackEnd.Utilities
{
    public static class TokenGenerator
    {
        public static string GenerarTokenRecuperacion()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            // Convierte a Base64 y elimina caracteres especiales para URLs
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}