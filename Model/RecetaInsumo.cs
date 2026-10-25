using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class RecetaInsumo
{
    public int Id { get; set; }

    public int RecetaId { get; set; }

    public int InsumoId { get; set; }

    public decimal Cantidad { get; set; }

    // Porcentaje de merma propio de esta preparación (0-100).
    public decimal PorcentajeMerma { get; set; }

    public string UnidadMedida { get; set; } = null!;

    public virtual Insumo Insumo { get; set; } = null!;

    public virtual Receta Receta { get; set; } = null!;
}
