using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class ProductoMenuRecetum
{
    public int Id { get; set; }

    public int ProductoMenuId { get; set; }

    public int RecetaId { get; set; }

    public decimal Cantidad { get; set; }

    public virtual ProductoMenu ProductoMenu { get; set; } = null!;

    public virtual Recetum Receta { get; set; } = null!;
}
