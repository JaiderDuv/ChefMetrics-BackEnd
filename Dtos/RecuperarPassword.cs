using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class RecuperarPasswordDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
