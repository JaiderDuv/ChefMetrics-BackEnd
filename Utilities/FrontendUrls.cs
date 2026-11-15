namespace ChefMetrics_BackEnd.Utilities;

public static class FrontendUrls
{
    public static Uri ResolveBaseUrl(string? configuredUrl, bool isDevelopment)
    {
        var url = string.IsNullOrWhiteSpace(configuredUrl)
            ? (isDevelopment ? "https://localhost:7158/" : "https://pwgapg-frontend.pages.dev/")
            : configuredUrl.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            (!isDevelopment && (uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttps)) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Frontend:BaseUrl debe ser una URL válida, pública y HTTPS en producción.");
        }

        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }

    public static string ResolveEmailUrl(string? configuredUrl, Uri frontendBaseUrl, string route, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(configuredUrl))
            return new Uri(frontendBaseUrl, route).AbsoluteUri;

        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException($"La URL de correo para {route} debe ser una URL HTTP o HTTPS válida.");
        }

        // Una configuración local copiada al despliegue debe apuntar al frontend público.
        if (!isDevelopment && uri.IsLoopback)
            return new Uri(frontendBaseUrl, route).AbsoluteUri;

        if (!isDevelopment && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"La URL de correo para {route} debe usar HTTPS en producción.");

        return uri.AbsoluteUri;
    }
}
