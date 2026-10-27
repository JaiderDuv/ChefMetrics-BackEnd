using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Insumo
{
    public int Id { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public decimal CostoUnitario { get; set; }

    public string? Proveedor { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public decimal? Cantidad { get; set; }

    // Densidad del insumo en g/ml. Permite convertir entre masa y volumen
    // (por ejemplo, taza de harina a gramos) cuando la unidad de la receta
    // no coincide con la unidad natural del insumo.
    public decimal? DensidadGPorMl { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<InsumoProveedor> InsumoProveedors { get; set; } = new List<InsumoProveedor>();

    public virtual ICollection<RecetaInsumo> RecetaInsumos { get; set; } = new List<RecetaInsumo>();
}
