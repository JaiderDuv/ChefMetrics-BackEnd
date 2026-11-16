using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ChefMetrics_BackEnd.Tests;

public sealed class EnvioCorreoHttpsTests
{
    [Fact]
    public async Task RecuperacionEnviaElEnlacePorHttpsSinCredencialesSmtp()
    {
        using var http = new HttpClient(new BrevoHandler());
        var service = new SmtpEmailService(Options.Create(new EmailSettings
        {
            BrevoApiKey = "test-key",
            From = "remitente@example.com",
            PasswordResetUrl = "https://pwgapg-frontend.pages.dev/nueva-contrasena"
        }), http);

        await service.EnviarCorreoRecuperacionAsync("destino@example.com", "Usuario", "token-prueba");
    }

    private sealed class BrevoHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.brevo.com/v3/smtp/email", request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("api-key")));
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var message = payload.RootElement;
            Assert.Equal("remitente@example.com", message.GetProperty("sender").GetProperty("email").GetString());
            Assert.Equal("destino@example.com", message.GetProperty("to")[0].GetProperty("email").GetString());
            var html = WebUtility.HtmlDecode(message.GetProperty("htmlContent").GetString());
            var enlace = Regex.Match(html!, """href="(https://pwgapg-frontend\.pages\.dev/nueva-contrasena\?[^"]+)["]""");
            Assert.True(enlace.Success);
            var query = QueryHelpers.ParseQuery(new Uri(enlace.Groups[1].Value).Query);
            Assert.Equal("token-prueba", query["token"].ToString());
            Assert.Equal("destino@example.com", query["correoElectronico"].ToString());
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
