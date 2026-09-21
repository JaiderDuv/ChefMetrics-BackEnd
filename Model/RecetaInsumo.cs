using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class RecetaInsumo
{
    public int Id { get; set; }

    public int RecetaId { get; set; }

    public int InsumoId { get; set; }

    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = null!;

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual Recetum Receta { get; set; } = null!;
}
