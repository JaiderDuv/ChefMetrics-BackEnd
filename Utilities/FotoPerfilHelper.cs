using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Utilities;

/// <summary>
/// Valida y normaliza la foto de perfil que envía el frontend como data URL.
/// Se guarda en UsuarioEmprendedor.FotoPerfilUrl (nvarchar(max)).
/// </summary>
public static class FotoPerfilHelper
{
    private const int MaxBytes = 2 * 1024 * 1024;
    private const int MaxLongitudDataUrl = 4_000_000;

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif",
        "image/bmp"
    };

    public static bool TryNormalizar(string? valor, out string? normalizado, out string? error)
    {
        normalizado = null;
        error = null;

        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        var texto = valor.Trim();
        if (texto.Length > MaxLongitudDataUrl)
        {
            error = "La foto de perfil no puede superar los 2 MB.";
            return false;
        }

        if (!texto.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            error = "La foto de perfil debe enviarse como una imagen data URL.";
            return false;
        }

        var coma = texto.IndexOf(',');
        if (coma <= 5)
        {
            error = "La foto de perfil no tiene un formato válido.";
            return false;
        }

        var cabecera = texto[5..coma];
        var partes = cabecera.Split(';');
        if (partes.Length != 2 ||
            !partes[1].Equals("base64", StringComparison.OrdinalIgnoreCase) ||
            !TiposPermitidos.Contains(partes[0]))
        {
            error = "El formato de la foto no es válido. Usa JPG, PNG, WEBP, GIF o BMP.";
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(texto[(coma + 1)..]);
            if (bytes.Length == 0)
            {
                error = "La foto de perfil está vacía.";
                return false;
            }

            if (bytes.Length > MaxBytes)
            {
                error = "La foto de perfil no puede superar los 2 MB.";
                return false;
            }
        }
        catch (FormatException)
        {
            error = "La foto de perfil no contiene datos Base64 válidos.";
            return false;
        }

        normalizado = texto;
        return true;
    }
}
