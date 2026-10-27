namespace ChefMetrics.Contracts;

// Única fuente de conversión de unidades para backend y frontend.
public static class ConversorUnidades
{
    // Patrón compartido por las validaciones de DTO (DataAnnotations exige const).
    public const string PatronUnidades =
        @"(?i)^(mg|g|kg|oz|lb|ml|l|cdta|cda|taza|floz|pt|qt|gal|unidad|docena)$";

    // Orden en el que se muestran las unidades en la interfaz.
    private static readonly string[] OrdenUnidades =
    {
        "mg", "g", "kg", "oz", "lb",
        "ml", "l", "cdta", "cda", "taza", "floz", "pt", "qt", "gal",
        "unidad", "docena"
    };

    // Factor respecto a la unidad base de cada familia (g, ml, unidad).
    private static readonly Dictionary<string, (string Familia, decimal Factor)> Tabla =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["mg"] = ("masa", 0.001m),
            ["g"] = ("masa", 1m),
            ["kg"] = ("masa", 1000m),
            ["oz"] = ("masa", 28.3495m),
            ["lb"] = ("masa", 453.592m),
            ["ml"] = ("volumen", 1m),
            ["l"] = ("volumen", 1000m),
            ["cdta"] = ("volumen", 5m),             // 1 cucharadita = 5 ml
            ["cda"] = ("volumen", 15m),             // 1 cucharada = 15 ml
            ["taza"] = ("volumen", 240m),           // 1 taza = 240 ml (ajustable)
            ["floz"] = ("volumen", 29.5735m),       // 1 onza líquida = 29,5735 ml
            ["pt"] = ("volumen", 473.176m),         // 1 pinta = 473,176 ml
            ["qt"] = ("volumen", 946.353m),         // 1 cuarto = 946,353 ml
            ["gal"] = ("volumen", 3785.41m),        // 1 galón = 3.785,41 ml
            ["unidad"] = ("conteo", 1m),
            ["docena"] = ("conteo", 12m)
        };

    public static IReadOnlyList<string> Unidades => OrdenUnidades;

    public static bool EsValida(string? u) => u is not null && Tabla.ContainsKey(u.Trim());

    public static bool EsUnidadPequena(string? u) =>
        u?.Trim().ToLowerInvariant() is "taza" or "unidad" or "docena" or "cda" or "cdta";

    // Dos unidades son compatibles si pertenecen a la misma familia. Masa y
    // volumen también lo son cuando el insumo tiene una densidad conocida.
    public static bool Compatibles(string a, string b, decimal? densidadGPorMl = null) =>
        Tabla.TryGetValue(a, out var x) && Tabla.TryGetValue(b, out var y) &&
        (x.Familia == y.Familia || (densidadGPorMl is > 0 && SonMasaYVolumen(x.Familia, y.Familia)));

    public static IEnumerable<string> UnidadesDeLaMismaFamilia(string unidad) =>
        Tabla.TryGetValue(unidad, out var x)
            ? Tabla.Where(t => t.Value.Familia == x.Familia).Select(t => t.Key)
            : Tabla.Keys;

    // Unidades que se pueden usar en una receta para un insumo. Incluye masa y
    // volumen cuando el insumo declara densidad, para permitir taza <-> gramos.
    public static IEnumerable<string> UnidadesCompatibles(string unidad, decimal? densidadGPorMl = null)
    {
        if (!Tabla.TryGetValue(unidad, out var x)) return Tabla.Keys;

        return Tabla
            .Where(t => t.Value.Familia == x.Familia ||
                        (densidadGPorMl is > 0 && SonMasaYVolumen(x.Familia, t.Value.Familia)))
            .Select(t => t.Key);
    }

    // Convierte una cantidad de una unidad a otra. La densidad (g/ml) solo es
    // necesaria cuando se cruza entre masa y volumen.
    public static decimal? Convertir(decimal cantidad, string deUnidad, string aUnidad,
        decimal? densidadGPorMl = null)
    {
        if (!Tabla.TryGetValue(deUnidad.Trim(), out var a) || !Tabla.TryGetValue(aUnidad.Trim(), out var b))
            return null;

        if (a.Familia == b.Familia)
            return cantidad * a.Factor / b.Factor;

        if (densidadGPorMl is not > 0) return null;

        if (a.Familia == "masa" && b.Familia == "volumen")
            return cantidad * a.Factor / densidadGPorMl.Value / b.Factor;

        if (a.Familia == "volumen" && b.Familia == "masa")
            return cantidad * a.Factor * densidadGPorMl.Value / b.Factor;

        return null;
    }

    // Precio por unidad del insumo para un paquete de proveedor.
    // Convierte la cantidad del paquete a la unidad base del insumo; null si la
    // familia del paquete no es compatible (los paquetes en familias distintas no
    // se comparan entre sí para no elegir un proveedor "barato" equivocado).
    public static decimal? PrecioPorUnidad(decimal costo, decimal cantidadPaquete, string unidadPaquete,
        string unidadInsumo, decimal? densidadGPorMl = null)
    {
        if (cantidadPaquete <= 0) return null;
        var cantidadBase = Convertir(cantidadPaquete, unidadPaquete, unidadInsumo, densidadGPorMl);
        if (cantidadBase is null or <= 0) return null;
        return costo / cantidadBase.Value;
    }

    // Costo de una línea de receta, considerando conversión de unidad y merma.
    public static decimal CostoLinea(decimal cantidad, string unidadReceta, string unidadInsumo,
        decimal costoUnitario, decimal mermaPorcentaje, decimal? densidadGPorMl = null)
    {
        var convertida = Convertir(cantidad, unidadReceta, unidadInsumo, densidadGPorMl);
        if (convertida is null) return 0m;
        return decimal.Round(
            CalculadoraCostos.CantidadParaComprar(convertida.Value, mermaPorcentaje) * costoUnitario, 2);
    }

    private static bool SonMasaYVolumen(string familiaA, string familiaB) =>
        (familiaA == "masa" && familiaB == "volumen") ||
        (familiaA == "volumen" && familiaB == "masa");
}
