namespace ChefMetrics.Contracts;

// Punto de equilibrio mensual: cuánto hay que vender para cubrir los costos
// fijos, usando el margen de contribución de los productos (precio − costo
// directo, sin costos fijos). No es una proyección de ventas: es el mínimo.
public sealed class PuntoEquilibrioDto
{
    public decimal CostosFijosMensuales { get; set; }
    public int ProductosConsiderados { get; set; }
    public decimal? MargenContribucionPromedioPorcentaje { get; set; }
    public decimal? VentasEquilibrioMensuales { get; set; }
    public List<PuntoEquilibrioProductoDto> Productos { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public sealed class PuntoEquilibrioProductoDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public decimal CostoDirecto { get; set; }
    public decimal MargenContribucion { get; set; }
    public decimal MargenContribucionPorcentaje { get; set; }

    // Si este producto fuera el único que vendes.
    public decimal? UnidadesEquilibrio { get; set; }
    public decimal? VentasEquilibrio { get; set; }
}
