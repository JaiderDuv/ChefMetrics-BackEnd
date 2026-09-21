using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class PasswordResetToken
{
    public int Id { get; set; }

    public int CuentaId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Usado { get; set; }

    public virtual Cuentum Cuenta { get; set; } = null!;
}
