namespace ChefMetrics.Contracts;

// Única fuente de conversión de unidades para backend y frontend.
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

    public static IEnumerable<string> UnidadesDeLaMismaFamilia(string unidad) =>
        Tabla.TryGetValue(unidad, out var x)
            ? Tabla.Where(t => t.Value.Familia == x.Familia).Select(t => t.Key)
            : Tabla.Keys;

    // Convierte una cantidad de una unidad a otra de la misma familia. null si no son compatibles.
    public static decimal? Convertir(decimal cantidad, string deUnidad, string aUnidad)
    {
        if (!Tabla.TryGetValue(deUnidad.Trim(), out var a) || !Tabla.TryGetValue(aUnidad.Trim(), out var b)
            || a.Familia != b.Familia) return null;
        return cantidad * a.Factor / b.Factor;
    }

    // Precio por unidad del insumo para un paquete de proveedor.
    // Convierte la cantidad del paquete a la unidad base del insumo; null si la
    // familia del paquete no es compatible (los paquetes en familias distintas no
    // se comparan entre sí para no elegir un proveedor "barato" equivocado).
    public static decimal? PrecioPorUnidad(decimal costo, decimal cantidadPaquete, string unidadPaquete, string unidadInsumo)
    {
        if (cantidadPaquete <= 0) return null;
        var cantidadBase = Convertir(cantidadPaquete, unidadPaquete, unidadInsumo);
        if (cantidadBase is null or <= 0) return null;
        return costo / cantidadBase.Value;
    }

    // Costo de una línea de receta, considerando conversión de unidad y merma.
    public static decimal CostoLinea(decimal cantidad, string unidadReceta, string unidadInsumo,
        decimal costoUnitario, decimal mermaPorcentaje)
    {
        var convertida = Convertir(cantidad, unidadReceta, unidadInsumo);
        if (convertida is null) return 0m;
        return decimal.Round(
            CalculadoraCostos.CantidadParaComprar(convertida.Value, mermaPorcentaje) * costoUnitario, 2);
    }
}
