using System.Globalization;
using System.Text;

namespace ChefMetrics_BackEnd.Utilities;

public static class Normalizador
{
    public const string CollationInsensible = "Latin1_General_CI_AI";

    public static string? NormalizarProveedor(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return string.Join(" ", valor.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public static string SinTildes(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string ClaveProveedor(string? valor)
    {
        var normalizado = NormalizarProveedor(valor) ?? string.Empty;
        return SinTildes(normalizado).ToLowerInvariant();
    }

    public static string? NormalizarCategoria(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "otro";
        }

        var partes = valor.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("_", partes);
    }
}
