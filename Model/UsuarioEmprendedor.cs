using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class UsuarioEmprendedor
{
    public int IdUsuarioEmprendedor { get; set; }

    public int IdRol { get; set; }

    public bool Formalizado { get; set; }

    public string? Nit { get; set; }

    public string? RazonSocial { get; set; }

    public string CorreoElectronico { get; set; } = null!;

    public string? TipoDocumento { get; set; }

    public string? DocumentoIdentidadUe { get; set; }

    public string? NombreEmprendimiento { get; set; }

    public bool OnboardingCompletado { get; set; }

    public string CategoriaNegocio { get; set; } = null!;

    public string TelefonoContacto { get; set; } = null!;

    public string CorreoElectronicoContacto { get; set; } = null!;

    public string HashContrasena { get; set; } = null!;

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
