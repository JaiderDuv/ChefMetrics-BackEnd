namespace ChefMetrics_BackEnd.Costeo;

public sealed class SugerirPrecioDto
{
    public decimal MargenDeseado { get; set; }
}

public sealed class PrecioSugeridoDto
{
    public int RecetaId { get; set; }
    public string Receta { get; set; } = string.Empty;
    public decimal CostoPorPorcion { get; set; }
    public decimal MargenDeseado { get; set; }
    public decimal? PrecioSugerido { get; set; }
    public bool MargenValido { get; set; }
    public List<string> Advertencias { get; set; } = new();
}