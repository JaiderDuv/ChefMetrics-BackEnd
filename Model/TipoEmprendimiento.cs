using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class TipoEmprendimiento
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Negocio> Negocios { get; set; } = new List<Negocio>();
}
