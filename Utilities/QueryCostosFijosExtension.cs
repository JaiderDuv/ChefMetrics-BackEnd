using Microsoft.AspNetCore.Http;

namespace ChefMetrics_BackEnd.Utilities;

public static class QueryCostosFijosExtension
{
    // Semántica única de costosFijosIds en query:
    // - parámetro ausente: null => todos los costos fijos activos.
    // - parámetro presente sin IDs (?costosFijosIds=): lista vacía => ninguno.
    // - parámetro con IDs (repetidos o separados por coma): solo esos costos fijos.
    // Se recibe como string[] para que un valor vacío no active la validación
    // del binder de listas de enteros; la conversión se hace aquí.
    public static List<int>? LeerCostosFijosIds(this HttpRequest request, string[]? valores)
    {
        if (!request.Query.ContainsKey("costosFijosIds")) return null;

        var ids = new List<int>();
        foreach (var valor in valores ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(valor)) continue;
            foreach (var parte in valor.Split(',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(parte, out var id) && id > 0) ids.Add(id);
            }
        }

        return ids;
    }
}
