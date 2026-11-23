using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChefMetrics_BackEnd.Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CredentialRequestAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request;
        if (!string.Equals(request.ContentType, "application/jose", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new { mensaje = "Actualiza la página para enviar tus datos de forma segura." })
                { StatusCode = StatusCodes.Status415UnsupportedMediaType };
            return;
        }

        const int limit = 32 * 1024;
        var buffer = new byte[limit + 1];
        var count = await request.Body.ReadAtLeastAsync(buffer, buffer.Length, false, http.RequestAborted);
        if (count > limit)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }

        var challenges = http.RequestServices.GetRequiredService<CredentialChallenges>();
        var json = challenges.Open(Encoding.ASCII.GetString(buffer, 0, count), $"{request.Method} {request.Path}");
        if (json is null)
        {
            context.Result = new BadRequestObjectResult(new { mensaje = "Solicitud inválida o vencida. Intenta de nuevo." });
            return;
        }

        var original = request.Body;
        var length = request.ContentLength;
        using var decrypted = new MemoryStream(json);
        try
        {
            request.Body = decrypted;
            request.ContentType = "application/json";
            request.ContentLength = json.Length;
            await next();
        }
        finally
        {
            request.Body = original;
            request.ContentType = "application/jose";
            request.ContentLength = length;
            CryptographicOperations.ZeroMemory(json);
        }
    }
}
