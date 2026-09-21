using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Empleado
{
    public int Id { get; set; }

    public int? CuentaId { get; set; }

    public int NegocioId { get; set; }

    public int RolId { get; set; }

    public string CorreoInvitado { get; set; } = null!;

    public string EstadoInvitacion { get; set; } = null!;

    public string? TokenInvitacion { get; set; }

    public int InvitadoPor { get; set; }

    public DateTime FechaInvitacion { get; set; }

    public DateTime? FechaAceptacion { get; set; }

    public virtual Cuentum? Cuenta { get; set; }

    public virtual UsuarioEmprendedor InvitadoPorNavigation { get; set; } = null!;

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual Role Rol { get; set; } = null!;
}
