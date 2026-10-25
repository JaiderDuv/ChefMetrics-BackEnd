namespace ChefMetrics.Contracts;

// Única fuente del cálculo de merma para backend y frontend.
public static class CalculadoraCostos
{
    // La merma es la fracción del insumo comprado que se pierde al prepararlo.
    // Si se necesitan 500 g netos y la merma es 10 %, se compran 500 / (1 - 0.10).
    // El rendimiento mínimo evita una división por cero si la merma llega a 100 %.
    public const decimal RendimientoMinimo = 0.01m;

    public static decimal Rendimiento(decimal mermaPorcentaje) =>
        Math.Max(1m - mermaPorcentaje / 100m, RendimientoMinimo);

    public static decimal CantidadParaComprar(decimal cantidadConvertida, decimal mermaPorcentaje) =>
        cantidadConvertida / Rendimiento(mermaPorcentaje);

    // Costo adicional causado por la merma, sin incluir el costo base del insumo.
    public static decimal MermaExtra(decimal cantidadConvertida, decimal costoUnitario, decimal mermaPorcentaje) =>
        cantidadConvertida * costoUnitario * (1m / Rendimiento(mermaPorcentaje) - 1m);

    // cantidadParaComprar = cantidadConvertida ÷ (1 - merma/100)
    // costoInsumo         = cantidadParaComprar × costo del proveedor principal
    // mermaExtra          = costoInsumo - cantidadConvertida × costo
    public static LineaCostoDto Linea(int insumoId, string nombre, decimal cantidad, string unidadReceta,
        string unidadInsumo, decimal merma, decimal costo, string fuente)
    {
        var convertida = ConversorUnidades.Convertir(cantidad, unidadReceta, unidadInsumo) ?? 0m;
        var paraComprar = CantidadParaComprar(convertida, merma);
        return new LineaCostoDto
        {
            InsumoId = insumoId,
            Insumo = nombre,
            Cantidad = cantidad,
            UnidadReceta = unidadReceta,
            UnidadInsumo = unidadInsumo,
            CantidadConvertida = decimal.Round(convertida, 4),
            CantidadParaComprar = decimal.Round(paraComprar, 4),
            CostoProveedor = costo,
            FuenteCosto = fuente,
            CostoInsumo = decimal.Round(paraComprar * costo, 2),
            MermaExtra = decimal.Round(MermaExtra(convertida, costo, merma), 2)
        };
    }

    // Σ (monto mensual ÷ valor divisor) de los costos fijos activos.
    public static decimal FijoProrrateado(IEnumerable<(decimal Monto, decimal Divisor)> activos) =>
        activos.Where(c => c.Divisor > 0).Sum(c => c.Monto / c.Divisor);
}
