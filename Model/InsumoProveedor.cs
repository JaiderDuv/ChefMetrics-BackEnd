using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class InsumoProveedor
{
    public int Id { get; set; }

    public int InsumoId { get; set; }

    public int ProveedorId { get; set; }

    public decimal Costo { get; set; }
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public virtual ICollection<HistorialPrecioInsumo> HistorialPrecioInsumos { get; set; } = new List<HistorialPrecioInsumo>();

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}
