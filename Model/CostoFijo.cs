using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class CostoFijo
{
    public int Id { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal MontoMensual { get; set; }

    public string UnidadProrrateo { get; set; } = null!;

    public bool Activo { get; set; }

    public decimal ValorDivisor { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;
}
