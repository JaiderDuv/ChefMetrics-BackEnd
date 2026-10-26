using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

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

    [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string TelefonoContacto { get; set; } = string.Empty;

    [StringLength(20)]
    public string Nit { get; set; } = string.Empty;

    [StringLength(150)]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(50)]
    public string Rues { get; set; } = string.Empty;

    [StringLength(5)]
    public string? TipoDocumento { get; set; }

    [StringLength(20)]
    public string? NumeroDocumento { get; set; }

    [Required(ErrorMessage = "El correo de la empresa es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo de la empresa no es válido.")]
    [StringLength(255)]
    public string CorreoEmpresa { get; set; } = string.Empty;

    public bool Formalizado { get; set; }

    public bool TieneSitioFisico { get; set; }

    [StringLength(255)]
    public string Direccion { get; set; } = string.Empty;

    public List<EmpleadoDto> Empleados { get; set; } = new();

    public string? LogoUrl { get; set; }
}

public class EmpleadoDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El correo del empleado es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo del empleado no es válido.")]
    [StringLength(255)]
    public string CorreoInvitado { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar el rol del empleado.")]
    public int RolId { get; set; }

    public string? NombreRol { get; set; }
    public string? EstadoInvitacion { get; set; }
}

public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarPerfilResultadoDto
{
    public bool Exito { get; set; }
    public bool RequiereReautenticacion { get; set; }
    public bool RequiereConfirmacionCorreo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public class PerfilUsuarioDto
{
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

