using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class EmprendedorFormalizado
    {
        public bool Formalizado { get; set; }

        [Required(ErrorMessage = "El NIT es obligatorio para empresas formalizadas.")]
        [RegularExpression(@"^\d{9,10}$", ErrorMessage = "El NIT debe ser numérico de 9 a 10 dígitos.")]
        public string Nit { get; set; } = string.Empty;

        [Required(ErrorMessage = "La Razón Social es obligatoria.")]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^\d{5,12}$", ErrorMessage = "El documento debe ser numérico de 5 a 12 dígitos.")]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(100)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría de negocio es obligatoria.")]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe contener 10 dígitos.")]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo de contacto inválido.")]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string HashContrasena { get; set; } = string.Empty;
    }

    public class Emprendedor
    {
        public bool Formalizado { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [RegularExpression(@"^\d{5,12}$", ErrorMessage = "El documento debe ser numérico de 5 a 12 dígitos.")]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(100)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría de negocio es obligatoria.")]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe contener 10 dígitos.")]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo de contacto inválido.")]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string HashContrasena { get; set; } = string.Empty;
    }
}