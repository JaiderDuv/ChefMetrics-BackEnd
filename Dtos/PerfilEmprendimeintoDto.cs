using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public class ConfirmarEdicionPerfilDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class PermisoEdicionPerfilDto
{
    public string EditToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class PerfilEmprendedorDto
{
    [Required(ErrorMessage = "El correo de la cuenta es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de la cuenta no es válido.")]
    [StringLength(255)]
    public string CorreoCuenta { get; set; } = string.Empty;

    [StringLength(150)]
    public string NombreEmprendimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría del negocio es obligatoria.")]
    [StringLength(100)]
    public string CategoriaNegocio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de contacto no es válido.")]
    [StringLength(255)]
    public string CorreoContacto { get; set; } = string.Empty;

    [StringLength(20)]
    public string Nit { get; set; } = string.Empty;

    [StringLength(150)]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(50)]
    public string Rues { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo de la empresa es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de la empresa no es válido.")]
    [StringLength(255)]
    public string CorreoEmpresa { get; set; } = string.Empty;

    public bool Formalizado { get; set; }

    public bool TieneSitioFisico { get; set; }

    [StringLength(255)]
    public string Direccion { get; set; } = string.Empty;

    public List<SocioNegocioDto> Socios { get; set; } = new();
}

public class SocioNegocioDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Los nombres del socio son obligatorios.")]
    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos del socio son obligatorios.")]
    [StringLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento del socio es obligatorio.")]
    [StringLength(5)]
    public string TipoDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento del socio es obligatorio.")]
    [StringLength(20)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono del socio es obligatorio.")]
    [Phone(ErrorMessage = "El teléfono del socio no es válido.")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del socio es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo del socio no es válido.")]
    [StringLength(255)]
    public string CorreoElectronico { get; set; } = string.Empty;
}

public class ActualizarPerfilResultadoDto
{
    public bool Exito { get; set; }
    public bool RequiereReautenticacion { get; set; }
    public bool RequiereConfirmacionCorreo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

// Perfil de usuario: edición libre, sin validaciones de seguridad.
// Todos los campos son opcionales; el frontend los autocompleta al abrir.
public class PerfilUsuarioDto
{
    // No persistido: la BD no tiene columna de foto. Se edita/previsualiza en cliente.
    public string? FotoPerfilUrl { get; set; }

    [StringLength(5)]
    public string TipoDocumento { get; set; } = string.Empty;

    [StringLength(20)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [StringLength(20)]
    public string TelefonoContacto { get; set; } = string.Empty;

    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [StringLength(100)]
    public string Apellidos { get; set; } = string.Empty;
}
