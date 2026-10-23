namespace ChefMetrics_BackEnd.Dtos;

public sealed class SugerirPrecioDto
{
    public decimal MargenDeseado { get; set; }

    // Si se indica, el costo fijo prorrateado usa solo estos costos fijos;
    // null = todos los activos, lista vacía = ninguno (prorrateo 0).
    public List<int>? CostosFijosIds { get; set; }
}

public sealed class PrecioSugeridoDto
{
    public int RecetaId { get; set; }
    public string Receta { get; set; } = string.Empty;

    // Costo directo por porción (insumos, sin costos fijos).
    public decimal CostoDirectoPorPorcion { get; set; }

    // Costo completo por porción (insumos + cuota de costos fijos).
    public decimal CostoPorPorcion { get; set; }

    public decimal MargenDeseado { get; set; }
    public decimal? PrecioSugerido { get; set; }
    public bool MargenValido { get; set; }
    public List<string> Advertencias { get; set; } = new();
}