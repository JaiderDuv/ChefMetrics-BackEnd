using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

// Simulación mensual de ventas. No persiste nada: solo calcula el escenario
// con los precios y costos directos actuales del menú.
public sealed class SimularVentasDto
{
    [MinLength(1, ErrorMessage = "Agrega al menos un producto a la simulación.")]
    [MaxLength(200, ErrorMessage = "Máximo 200 productos por simulación.")]
    public List<SimulacionVentaItemDto> Productos { get; set; } = new();
}

public sealed class SimulacionVentaItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto.")]
    public int ProductoId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe estar entre 1 y 1.000.000.")]
    public int Cantidad { get; set; }
}

public sealed class SimulacionVentasDto
{
    public decimal CostosFijosMensuales { get; set; }
    public decimal VentasTotales { get; set; }
    public decimal CostoDirectoTotal { get; set; }
    public decimal MargenContribucion { get; set; }

    // Porcentaje sobre las ventas. Null si no hay ventas.
    public decimal? MargenContribucionPorcentaje { get; set; }

    // Margen de contribución menos costos fijos. Puede ser negativo.
    public decimal Utilidad { get; set; }

    // Facturación necesaria para cubrir los fijos con el mix simulado.
    // Null si el margen es cero o negativo.
    public decimal? PuntoEquilibrioMensual { get; set; }

    // Diferencia contra el punto de equilibrio. Solo uno de los dos tiene valor.
    public decimal? FaltanteParaEquilibrio { get; set; }
    public decimal? ExcedenteSobreEquilibrio { get; set; }

    // Margen de contribución ÷ costos fijos × 100. Null si no hay fijos.
    public decimal? CoberturaCostosFijosPorcentaje { get; set; }

    public int ProductosConsiderados { get; set; }
    public List<SimulacionVentaProductoDto> Productos { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public sealed class SimulacionVentaProductoDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public decimal CostoDirecto { get; set; }
    public int Cantidad { get; set; }
    public decimal Ventas { get; set; }
    public decimal CostoDirectoTotal { get; set; }
    public decimal MargenContribucion { get; set; }
    public decimal MargenContribucionPorcentaje { get; set; }
    public decimal? ParticipacionVentasPorcentaje { get; set; }
    public decimal? ParticipacionMargenPorcentaje { get; set; }
}
