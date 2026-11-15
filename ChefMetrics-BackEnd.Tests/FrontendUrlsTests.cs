using ChefMetrics_BackEnd.Utilities;

namespace ChefMetrics_BackEnd.Tests;

public sealed class FrontendUrlsTests
{
    [Fact]
    public void Produccion_usa_el_frontend_publico_por_defecto()
    {
        Assert.Equal("https://pwgapg-frontend.pages.dev/", FrontendUrls.ResolveBaseUrl(null, false).AbsoluteUri);
    }

    [Theory]
    [InlineData("https://localhost:7158/")]
    [InlineData("http://frontend.example/")]
    [InlineData("https://frontend.example/?token=test")]
    [InlineData("https://frontend.example/#app")]
    [InlineData("/ruta-relativa")]
    public void Produccion_rechaza_una_base_invalida(string url)
    {
        Assert.Throws<InvalidOperationException>(() => FrontendUrls.ResolveBaseUrl(url, false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://localhost:7158/nueva-contrasena")]
    [InlineData("http://127.0.0.1:5196/nueva-contrasena")]
    public void Los_correos_de_produccion_usan_la_base_publica(string? configuredUrl)
    {
        var frontend = FrontendUrls.ResolveBaseUrl("https://frontend.example/app", false);
        Assert.Equal("https://frontend.example/app/nueva-contrasena",
            FrontendUrls.ResolveEmailUrl(configuredUrl, frontend, "nueva-contrasena", false));
    }

    [Fact]
    public void Conserva_el_enlace_explicito_de_correo()
    {
        var url = "https://cuentas.example/confirmar?idioma=es";
        Assert.Equal(url, FrontendUrls.ResolveEmailUrl(url,
            FrontendUrls.ResolveBaseUrl(null, false), "confirmar-cuenta", false));
    }

    [Fact]
    public void Desarrollo_conserva_los_enlaces_locales()
    {
        var frontend = FrontendUrls.ResolveBaseUrl(null, true);
        Assert.Equal("https://localhost:7158/aceptar-invitacion",
            FrontendUrls.ResolveEmailUrl(null, frontend, "aceptar-invitacion", true));
    }
}
