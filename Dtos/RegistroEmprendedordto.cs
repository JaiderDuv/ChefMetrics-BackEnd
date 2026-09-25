using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class EmprendedorFormalizado
    {
        public bool Formalizado { get; set; }

        [Required]
        [StringLength(20)]
        public string Nit { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(150)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Phone]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,128}$")]
        public string HashContrasena { get; set; } = string.Empty;
    }

    public class Emprendedor
    {
        public bool Formalizado { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        [StringLength(150)]
        public string NombreEmprendimiento { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoriaNegocio { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Phone]
        public string TelefonoContacto { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,128}$")]
        public string HashContrasena { get; set; } = string.Empty;
    }
}
