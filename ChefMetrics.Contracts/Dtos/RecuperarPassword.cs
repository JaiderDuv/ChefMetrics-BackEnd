using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts
{
    public class RecuperarPasswordDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [StringLength(255)]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
