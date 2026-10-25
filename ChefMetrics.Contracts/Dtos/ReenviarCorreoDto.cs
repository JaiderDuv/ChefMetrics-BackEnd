using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts
{
    public class ReenviarCorreoDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        public string Correo { get; set; } = string.Empty;
    }
}