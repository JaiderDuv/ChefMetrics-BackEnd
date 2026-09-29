namespace ChefMetrics_BackEnd.Model;

public partial class SocioNegocio
{
    public int Id { get; set; }

    public int UsuarioEmprendedorId { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string TipoDocumento { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string CorreoElectronico { get; set; } = null!;

    public virtual UsuarioEmprendedor UsuarioEmprendedor { get; set; } = null!;
}
