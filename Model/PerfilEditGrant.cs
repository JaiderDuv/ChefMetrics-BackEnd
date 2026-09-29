namespace ChefMetrics_BackEnd.Model;

public partial class PerfilEditGrant
{
    public int Id { get; set; }

    public int CuentaId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Usado { get; set; }

    public virtual Cuenta Cuenta { get; set; } = null!;
}
