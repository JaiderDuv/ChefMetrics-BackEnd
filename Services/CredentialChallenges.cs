using System.Security.Cryptography;
using System.Text.Json;
using Jose;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace ChefMetrics_BackEnd.Services;

public sealed class CredentialChallenges : IDisposable
{
    private readonly RSA _key = RSA.Create(3072);
    private readonly MemoryCache _pending = new(new MemoryCacheOptions { SizeLimit = 8192 });
    private readonly object _gate = new();
    private readonly string _publicKey;

    public CredentialChallenges() => _publicKey = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());

    public sealed record Challenge(string Header, string PublicKey);

    public Challenge Issue()
    {
        var header = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "RSA-OAEP-256", enc = "A256GCM", jti = Guid.NewGuid().ToString("N")
        }));
        _pending.Set(header, true, new MemoryCacheEntryOptions()
            .SetSize(1).SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));
        return new(header, _publicKey);
    }

    public byte[]? Open(string token, string target)
    {
        var separator = token.IndexOf('.');
        if (separator is <= 0 or > 256) return null;
        var header = token[..separator];
        lock (_gate)
        {
            // Only headers issued here are accepted, exactly once, including concurrent submissions.
            if (!_pending.TryGetValue(header, out _)) return null;
            _pending.Remove(header);
        }

        try
        {
            var payload = JWT.DecodeBytes(token, _key, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM);
            try
            {
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("target", out var route) || route.ValueKind != JsonValueKind.String ||
                    route.GetString() != target || !root.TryGetProperty("body", out var body) ||
                    body.ValueKind != JsonValueKind.Object) return null;
                return JsonSerializer.SerializeToUtf8Bytes(body);
            }
            finally { CryptographicOperations.ZeroMemory(payload); }
        }
        catch (Exception ex) when (ex is JoseException or CryptographicException or JsonException
            or ArgumentException or FormatException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _pending.Dispose();
        _key.Dispose();
    }
}
