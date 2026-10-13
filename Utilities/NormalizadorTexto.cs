using System.Globalization;
using System.Text;

namespace ChefMetrics_BackEnd.Utilities;

public static class NormalizadorTexto
{
    
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var sinEspaciosExtra = string.Join(' ', texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var formaD = sinEspaciosExtra.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in formaD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}