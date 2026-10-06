using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class InsumoProveedor
{
    public int Id { get; set; }

    public int InsumoId { get; set; }

    public int ProveedorId { get; set; }

    public decimal Costo { get; set; }

    public bool EsPrincipal { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual ICollection<HistorialPrecioInsumo> HistorialPrecioInsumos { get; set; } = new List<HistorialPrecioInsumo>();
}
