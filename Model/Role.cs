using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Role
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}
