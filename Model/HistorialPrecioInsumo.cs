using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class HistorialPrecioInsumo
{
    public int Id { get; set; }

    public int InsumoProveedorId { get; set; }

    public decimal CostoAnterior { get; set; }

    public decimal CostoNuevo { get; set; }

    public DateTime FechaCambio { get; set; }

    public virtual InsumoProveedor InsumoProveedor { get; set; } = null!;
}
