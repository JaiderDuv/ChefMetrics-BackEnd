using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class EmprendedorFormalizado
    {
        public bool Formalizado { get; set; }

        public string Nit { get; set; } = string.Empty;

        public string RazonSocial { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = string.Empty;

        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        public string NombreEmprendimiento { get; set; } = string.Empty;

        public string CategoriaNegocio { get; set; } = string.Empty;

        public string TelefonoContacto { get; set; } = string.Empty;

        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        public string HashContrasena { get; set; } = string.Empty;
    }

    public class Emprendedor
    {
        public bool Formalizado { get; set; }

        public string CorreoElectronico { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = string.Empty;

        public string DocumentoIdentidadUe { get; set; } = string.Empty;

        public string NombreEmprendimiento { get; set; } = string.Empty;

        public string CategoriaNegocio { get; set; } = string.Empty;

        public string TelefonoContacto { get; set; } = string.Empty;

        public string CorreoElectronicoContacto { get; set; } = string.Empty;

        public string HashContrasena { get; set; } = string.Empty;
    }
}
