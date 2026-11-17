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

    [Theory]
    [InlineData("confirmar-cuenta")]
    [InlineData("aceptar-invitacion")]
    public async Task Otros_correos_transaccionales_incluyen_su_ruta_y_token(string route)
    {
        var handler = new BrevoHandler(route);
        using var http = new HttpClient(handler);
        var service = new SmtpEmailService(Options.Create(new EmailSettings
        {
            BrevoApiKey = "test-key",
            From = "remitente@example.com",
            VerificationUrl = "https://pwgapg-frontend.pages.dev/confirmar-cuenta",
            InvitationUrl = "https://pwgapg-frontend.pages.dev/aceptar-invitacion"
        }), http);

        if (route == "confirmar-cuenta")
            await service.EnviarCorreoVerificacionAsync("destino@example.com", "token-prueba");
        else
            await service.EnviarInvitacionEmpleadoAsync("destino@example.com", "<Cocina>", "token-prueba");
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class BrevoHandler(string route = "nueva-contrasena") : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.brevo.com/v3/smtp/email", request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("api-key")));
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var message = payload.RootElement;
            Assert.Equal("remitente@example.com", message.GetProperty("sender").GetProperty("email").GetString());
            Assert.Equal("destino@example.com", message.GetProperty("to")[0].GetProperty("email").GetString());
            var html = WebUtility.HtmlDecode(message.GetProperty("htmlContent").GetString());
            var enlace = Regex.Match(html!, $"""href="(https://pwgapg-frontend\.pages\.dev/{Regex.Escape(route)}\?[^"]+)["]""");
            Assert.True(enlace.Success);
            var query = QueryHelpers.ParseQuery(new Uri(enlace.Groups[1].Value).Query);
            Assert.Equal("token-prueba", query["token"].ToString());
            if (route == "nueva-contrasena")
                Assert.Equal("destino@example.com", query["correoElectronico"].ToString());
            else
                Assert.False(query.ContainsKey("correoElectronico"));
            if (route == "aceptar-invitacion")
                Assert.Contains("&lt;Cocina&gt;", message.GetProperty("htmlContent").GetString());
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
