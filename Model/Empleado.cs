using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public int IdRol { get; set; }

    public string NombreEmpleado { get; set; } = null!;

    public string DocIdentidadEmpleado { get; set; } = null!;

    public string CorreoElectronicoEmpleado { get; set; } = null!;

    public string? NumeroTel { get; set; }

    public string HashContrasena { get; set; } = null!;

    public virtual Role IdRolNavigation { get; set; } = null!;
}
