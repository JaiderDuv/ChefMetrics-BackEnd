using ChefMetrics_BackEnd.Dtos;

namespace ChefMetrics_BackEnd.Utilities;

public static class CalculadoraCostos
{
    // cantidadParaComprar = cantidadConvertida × (1 + merma/100)
    // costoInsumo         = cantidadParaComprar × costo del proveedor principal
    // mermaExtra          = cantidadConvertida × costo × merma/100
    public static LineaCostoDto Linea(int insumoId, string nombre, decimal cantidad, string unidadReceta,
        string unidadInsumo, decimal merma, decimal costo, string fuente)
    {
        var convertida = ConversorUnidades.Convertir(cantidad, unidadReceta, unidadInsumo) ?? 0m;
        var paraComprar = convertida * (1m + merma / 100m);
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
            CostoInsumo = paraComprar * costo,
            MermaExtra = convertida * costo * merma / 100m
        };
    }

    // Σ (monto mensual ÷ valor divisor) de los costos fijos activos.
    public static decimal FijoProrrateado(IEnumerable<(decimal Monto, decimal Divisor)> activos) =>
        activos.Where(c => c.Divisor > 0).Sum(c => c.Monto / c.Divisor);
}