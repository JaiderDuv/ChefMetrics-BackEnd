using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos
{
    public class RestablecerPasswordDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(255)]
        public string CorreoElectronico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El token de recuperación es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(128)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Debe contener al menos una mayúscula, una minúscula, un número y un carácter especial.")]
        public string NuevaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes confirmar la nueva contraseña.")]
        [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
