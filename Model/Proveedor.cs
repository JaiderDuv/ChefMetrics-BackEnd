using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Proveedor
{
    public int Id { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<InsumoProveedor> InsumoProveedors { get; set; } = new List<InsumoProveedor>();

    public virtual Negocio Negocio { get; set; } = null!;
}
