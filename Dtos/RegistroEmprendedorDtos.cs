using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class EmprendedorFormalizado
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden superar los 100 caracteres.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
        public string Apellidos { get; set; } = string.Empty;

        public bool Formalizado { get; set; }

        [Required(ErrorMessage = "El NIT es obligatorio para empresas formalizadas.")]
        [StringLength(20)]
        [RegularExpression(@"^\d{9,10}$", ErrorMessage = "El NIT debe ser numérico de 9 a 10 dígitos.")]
        public string Nit { get; set; } = string.Empty;

        [Required(ErrorMessage = "La Razón Social es obligatoria.")]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        [StringLength(5)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(@"^\d{5,12}$", ErrorMessage = "El documento debe ser numérico de 5 a 12 dígitos.")]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(150)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría de negocio es obligatoria.")]
        [StringLength(100)]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe contener 10 dígitos.")]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Correo de contacto inválido.")]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(128, MinimumLength = 8)]
        public string HashContrasena { get; set; } = string.Empty;
    }

    public class Emprendedor
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden superar los 100 caracteres.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
        public string Apellidos { get; set; } = string.Empty;

        public bool Formalizado { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        [StringLength(5)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(@"^\d{5,12}$", ErrorMessage = "El documento debe ser numérico de 5 a 12 dígitos.")]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(150)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría de negocio es obligatoria.")]
        [StringLength(100)]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe contener 10 dígitos.")]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Correo de contacto inválido.")]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(128, MinimumLength = 8)]
        public string HashContrasena { get; set; } = string.Empty;
    }
}