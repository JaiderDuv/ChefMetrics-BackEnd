using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChefMetrics.Contracts;

public sealed class InvitarEmpleadoDto
{
    [Required(ErrorMessage = "El correo del empleado es obligatorio.")]
    [StringLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingresa un correo válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,72}$",
        ErrorMessage = "Usa una mayúscula, una minúscula, un número y un carácter especial (@$!%*?&).")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol válido.")]
    public int RolId { get; set; }
}

public sealed class AceptarInvitacionDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public sealed class AceptarInvitacionRespuestaDto
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public sealed class ActualizarEmpleadoDto : IValidatableObject
{
    [Required(ErrorMessage = "El correo del empleado es obligatorio.")]
    [StringLength(255, ErrorMessage = "El correo no puede superar 255 caracteres.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingresa un correo válido.")]
    public string Correo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol válido.")]
    public int RolId { get; set; }

    public string? NuevaPassword { get; set; }
    public string? ConfirmarPassword { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var passwordInformada = !string.IsNullOrWhiteSpace(NuevaPassword);
        var confirmacionInformada = !string.IsNullOrWhiteSpace(ConfirmarPassword);
        if (!passwordInformada && !confirmacionInformada)
        {
            yield break;
        }

        if (!passwordInformada)
        {
            yield return new ValidationResult("Escribe la nueva contraseña.", [nameof(NuevaPassword)]);
            yield break;
        }

        if (NuevaPassword!.Length is < 8 or > 72 ||
            !Regex.IsMatch(
                NuevaPassword,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,72}$",
                RegexOptions.None,
                TimeSpan.FromMilliseconds(100)))
        {
            yield return new ValidationResult(
                "Usa entre 8 y 72 caracteres con mayúscula, minúscula, número y símbolo especial (@$!%*?&).",
                [nameof(NuevaPassword)]);
        }

        if (!confirmacionInformada)
        {
            yield return new ValidationResult("Confirma la nueva contraseña.", [nameof(ConfirmarPassword)]);
        }
        else if (!string.Equals(NuevaPassword, ConfirmarPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult("Las contraseñas no coinciden.", [nameof(ConfirmarPassword)]);
        }
    }
}

public sealed class ActualizarEstadoEmpleadoDto
{
    public bool Activo { get; set; }
}
