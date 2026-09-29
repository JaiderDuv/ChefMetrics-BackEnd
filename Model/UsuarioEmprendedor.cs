using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class UsuarioEmprendedor
{
    public int Id { get; set; }

    public int CuentaId { get; set; }

    public string TipoFormalizacion { get; set; } = null!;

    public string? Nit { get; set; }

    public string? RazonSocial { get; set; }

    public string? TipoDocumento { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? NombreEmprendimiento { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public bool OnboardingCompletado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Cuenta Cuenta { get; set; } = null!;

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<SocioNegocio> SociosNegocio { get; set; } = new List<SocioNegocio>();

    public virtual Negocio? Negocio { get; set; }
}
