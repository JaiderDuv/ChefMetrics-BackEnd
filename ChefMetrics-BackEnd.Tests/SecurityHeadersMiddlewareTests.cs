using ChefMetrics_BackEnd.Middleware;
using Microsoft.AspNetCore.Http;

namespace ChefMetrics_BackEnd.Tests;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Las_respuestas_de_la_api_incluyen_cabeceras_de_seguridad()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/productos-menu";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"].ToString());
        Assert.Contains("no-store", context.Response.Headers["Cache-Control"].ToString());
        Assert.Contains("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task Las_respuestas_que_no_son_de_la_api_no_reciben_csp_de_api()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal(string.Empty, context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.Equal(string.Empty, context.Response.Headers["Cache-Control"].ToString());
    }

    [Fact]
    public async Task El_middleware_no_interrumpe_la_cadena_si_no_hay_error()
    {
        var ejecutado = false;
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            ejecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(ejecutado);
    }
}
