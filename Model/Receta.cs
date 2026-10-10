using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;


public partial class Receta
{
    public string? ImagenUrl { get; set; }

    public int Id { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public int PorcionesBase { get; set; }

    public string? Instrucciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ProductoMenuRecetum> ProductoMenuReceta { get; set; } = new List<ProductoMenuRecetum>();

    public virtual ICollection<RecetaInsumo> RecetaInsumos { get; set; } = new List<RecetaInsumo>();
}
