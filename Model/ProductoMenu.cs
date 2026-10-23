using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class ProductoMenu
{
    public int Id { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal? PrecioVenta { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ProductoMenuRecetum> ProductoMenuReceta { get; set; } = new List<ProductoMenuRecetum>();

    public virtual ICollection<ProductoMenuInsumo> ProductoMenuInsumos { get; set; } = new List<ProductoMenuInsumo>();
}
