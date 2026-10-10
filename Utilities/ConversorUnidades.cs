namespace ChefMetrics_BackEnd.Utilities;

public static class ConversorUnidades
{
    // Factor respecto a la unidad base de cada familia (g, ml, unidad).
    private static readonly Dictionary<string, (string Familia, decimal Factor)> Tabla =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = ("masa", 1m),
            ["kg"] = ("masa", 1000m),
            ["oz"] = ("masa", 28.3495m),
            ["lb"] = ("masa", 453.592m),
            ["ml"] = ("volumen", 1m),
            ["l"] = ("volumen", 1000m),
            ["taza"] = ("volumen", 240m),          // 1 taza = 240 ml (ajustable)
            ["unidad"] = ("conteo", 1m)
        };

    public static bool EsValida(string? u) => u is not null && Tabla.ContainsKey(u.Trim());

    public static bool Compatibles(string a, string b) =>
        Tabla.TryGetValue(a, out var x) && Tabla.TryGetValue(b, out var y) && x.Familia == y.Familia;

    // Costo de una línea de receta, considerando conversión de unidad y merma.
    public static decimal CostoLinea(decimal cantidad, string unidadReceta, string unidadInsumo,
                                     decimal costoUnitario, decimal mermaPorcentaje)
    {
        if (!Compatibles(unidadReceta, unidadInsumo)) return 0m;
        var enUnidadInsumo = cantidad * Tabla[unidadReceta].Factor / Tabla[unidadInsumo].Factor;
        var paraComprar = enUnidadInsumo * (1m + mermaPorcentaje / 100m);
        return decimal.Round(paraComprar * costoUnitario, 2);
    }
    // Convierte una cantidad de una unidad a otra de la misma familia. null si no son compatibles.
    public static decimal? Convertir(decimal cantidad, string deUnidad, string aUnidad)
    {
        if (!Tabla.TryGetValue(deUnidad.Trim(), out var a) || !Tabla.TryGetValue(aUnidad.Trim(), out var b)
            || a.Familia != b.Familia) return null;
        return cantidad * a.Factor / b.Factor;
    }
}