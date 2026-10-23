using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;


public sealed class MaximoDecimalesAttribute : ValidationAttribute
{
    private readonly int _decimales;

    public MaximoDecimalesAttribute(int decimales)
    {
        _decimales = decimales;
        ErrorMessage = $"Se permiten máximo {decimales} decimales.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not decimal d) return true;
        var factor = (decimal)Math.Pow(10, _decimales);
        return Math.Round(d * factor) / factor == d;
    }
}

internal static class LimitesProveedor
{
    private static readonly HashSet<string> UnidadesPequenas =
        new(StringComparer.OrdinalIgnoreCase) { "unidad", "taza" };

    public const decimal CostoMaximo = 999999.99m;

    public static decimal CantidadMaxima(string? unidadMedida) =>
        !string.IsNullOrWhiteSpace(unidadMedida) && UnidadesPequenas.Contains(unidadMedida.Trim())
            ? 999.99m
            : 999999.99m;
}


public class CrearProveedorDto
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(75, ErrorMessage = "El nombre no puede superar los 75 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

    [StringLength(255, ErrorMessage = "El correo no puede superar los 255 caracteres.")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }
}

public class ActualizarProveedorDto
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(75, ErrorMessage = "El nombre no puede superar los 75 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

    [StringLength(255, ErrorMessage = "El correo no puede superar los 255 caracteres.")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }
}

public class ProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}


public class AsociarProveedorDto : IValidatableObject
{
    [Required(ErrorMessage = "Debe indicar el proveedor a asociar.")]
    public int ProveedorId { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El costo debe estar entre 0,01 y 999.999,99.")]
    [MaximoDecimales(2)]
    public decimal Costo { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    public decimal Cantidad { get; set; }

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [StringLength(10)]
    public string UnidadMedida { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var maximo = LimitesProveedor.CantidadMaxima(UnidadMedida);

        if (Cantidad <= 0)
            yield return new ValidationResult("La cantidad debe ser mayor a 0.", new[] { nameof(Cantidad) });
        else if (Cantidad > maximo)
            yield return new ValidationResult($"La cantidad no puede superar {maximo:N2}.", new[] { nameof(Cantidad) });
        else if (Math.Round(Cantidad * 100m) / 100m != Cantidad)
            yield return new ValidationResult("La cantidad admite máximo 2 decimales.", new[] { nameof(Cantidad) });
    }
}

public class CrearYAsociarProveedorDto : IValidatableObject
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(75, ErrorMessage = "El nombre no puede superar los 75 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

    [StringLength(255, ErrorMessage = "El correo no puede superar los 255 caracteres.")]
    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El costo debe estar entre 0,01 y 999.999,99.")]
    [MaximoDecimales(2)]
    public decimal Costo { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    public decimal Cantidad { get; set; }

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [StringLength(10)]
    public string UnidadMedida { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var maximo = LimitesProveedor.CantidadMaxima(UnidadMedida);

        if (Cantidad <= 0)
            yield return new ValidationResult("La cantidad debe ser mayor a 0.", new[] { nameof(Cantidad) });
        else if (Cantidad > maximo)
            yield return new ValidationResult($"La cantidad no puede superar {maximo:N2}.", new[] { nameof(Cantidad) });
        else if (Math.Round(Cantidad * 100m) / 100m != Cantidad)
            yield return new ValidationResult("La cantidad admite máximo 2 decimales.", new[] { nameof(Cantidad) });
    }
}

public class ActualizarCostoProveedorDto : IValidatableObject
{
    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, 999999.99, ErrorMessage = "El costo debe estar entre 0,01 y 999.999,99.")]
    [MaximoDecimales(2)]
    public decimal Costo { get; set; }

    public decimal? Cantidad { get; set; }

    [StringLength(10)]
    public string? UnidadMedida { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Cantidad.HasValue) yield break;

        var maximo = LimitesProveedor.CantidadMaxima(UnidadMedida);

        if (Cantidad.Value <= 0)
            yield return new ValidationResult("La cantidad debe ser mayor a 0.", new[] { nameof(Cantidad) });
        else if (Cantidad.Value > maximo)
            yield return new ValidationResult($"La cantidad no puede superar {maximo:N2}.", new[] { nameof(Cantidad) });
        else if (Math.Round(Cantidad.Value * 100m) / 100m != Cantidad.Value)
            yield return new ValidationResult("La cantidad admite máximo 2 decimales.", new[] { nameof(Cantidad) });
    }
}


public class InsumoProveedorDto
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public decimal Costo { get; set; }
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    // Precio por unidad base del insumo (convertido desde el paquete).
    // null si la familia del paquete no es compatible con la del insumo.
    public decimal? PrecioUnitario { get; set; }
    public bool EsPrincipal { get; set; }
    public bool EsMasBarato { get; set; }
}

public class ResultadoProveedorDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }
    public object? Datos { get; set; }

    public static ResultadoProveedorDto Ok(object datos, string mensaje) =>
        new() { Exito = true, Mensaje = mensaje, Datos = datos };

    public static ResultadoProveedorDto Error(string mensaje) =>
        new() { Exito = false, Mensaje = mensaje };

    public static ResultadoProveedorDto NoHallado(string mensaje) =>
        new() { Exito = false, NoEncontrado = true, Mensaje = mensaje };
}

public class ChequeoEliminarProveedorDto
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneInsumos { get; set; }
    public List<string> NombresInsumos { get; set; } = new();
}

public class ResultadoChequeoEliminarDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }
    public ChequeoEliminarProveedorDto? Datos { get; set; }

    public static ResultadoChequeoEliminarDto Ok(ChequeoEliminarProveedorDto datos) =>
        new() { Exito = true, Datos = datos };
    public static ResultadoChequeoEliminarDto NoHallado(string mensaje) =>
        new() { Exito = false, NoEncontrado = true, Mensaje = mensaje };
}