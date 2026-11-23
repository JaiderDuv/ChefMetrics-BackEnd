using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Controllers;
using ChefMetrics_BackEnd.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CredentialRequestTests
{
    [Fact]
    public async Task Login_exige_cifrado_y_entrega_las_credenciales_una_sola_vez()
    {
        const string ruta = "/api/auth/login";
        var login = new LoginDto { CorreoElectronico = "test@example.com", Password = "Prueba🔐" };
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        auth.Setup(s => s.IniciarSesionAsync(It.Is<LoginDto>(x =>
                x.CorreoElectronico == login.CorreoElectronico && x.Password == login.Password)))
            .ReturnsAsync(new LoginResponseDto { Exito = true });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddSingleton<CredentialChallenges>();
        builder.Services.AddSingleton(auth.Object);
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var challenge = await http.GetFromJsonAsync<CredentialChallenges.Challenge>("/api/auth/desafio");
        var token = Encrypt(challenge!, $"POST {ruta}", login);

        using var cleartext = await http.PostAsJsonAsync(ruta, login);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, cleartext.StatusCode);
        using var first = await http.PostAsync(ruta, JoseContent(token));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using var repeated = await http.PostAsync(ruta, JoseContent(token));
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        auth.VerifyAll();
        auth.Verify(s => s.IniciarSesionAsync(It.IsAny<LoginDto>()), Times.Once);
    }

    [Fact]
    public void El_servidor_rechaza_cambios_de_destino_o_de_contenido()
    {
        using var desafios = new CredentialChallenges();
        var login = new LoginDto();
        const string target = "POST /api/auth/login";
        Assert.Null(desafios.Open(Encrypt(desafios.Issue(), target, login), "POST /api/perfil/confirmar-identidad"));
        var parts = Encrypt(desafios.Issue(), target, login).Split('.');
        parts[4] = WebEncoders.Base64UrlEncode(new byte[16]);
        Assert.Null(desafios.Open(string.Join('.', parts), target));
    }

    private static ByteArrayContent JoseContent(string token)
    {
        var content = new ByteArrayContent(Encoding.ASCII.GetBytes(token));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/jose");
        return content;
    }

    private static string Encrypt(CredentialChallenges.Challenge challenge, string target, LoginDto login)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(challenge.PublicKey), out _);
        var key = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(12);
        var data = JsonSerializer.SerializeToUtf8Bytes(new { target, body = login });
        var cipher = new byte[data.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(iv, data, cipher, tag, Encoding.ASCII.GetBytes(challenge.Header));
        return string.Join('.', challenge.Header, WebEncoders.Base64UrlEncode(rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256)),
            WebEncoders.Base64UrlEncode(iv), WebEncoders.Base64UrlEncode(cipher), WebEncoders.Base64UrlEncode(tag));
    }
}
