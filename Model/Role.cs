using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Role
{
    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<UsuarioEmprendedor> UsuarioEmprendedors { get; set; } = new List<UsuarioEmprendedor>();
}
