using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Cuenta
{
    public int Id { get; set; }

    public string Correo { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string TipoCuenta { get; set; } = null!;

    public bool EmailVerificado { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Empleado? Empleado { get; set; }

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<PerfilEditGrant> PerfilEditGrants { get; set; } = new List<PerfilEditGrant>();

    public virtual UsuarioEmprendedor? UsuarioEmprendedor { get; set; }
}