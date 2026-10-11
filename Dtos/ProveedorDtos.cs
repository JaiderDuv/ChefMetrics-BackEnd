using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

// POST /api/negocios/mio/proveedores — proveedor general del negocio
public class CrearProveedorDto
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

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

public class AsociarProveedorDto
{
    [Required(ErrorMessage = "Debe indicar el proveedor a asociar.")]
    public int ProveedorId { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0.")]
    public decimal Costo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal? Cantidad { get; set; }
}

public class CrearYAsociarProveedorDto
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0.")]
    public decimal Costo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal? Cantidad { get; set; }
}

public class ActualizarCostoProveedorDto
{
    [Required(ErrorMessage = "El costo es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El costo debe ser mayor a 0.")]
    public decimal Costo { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal? Cantidad { get; set; }
}

public class InsumoProveedorDto
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public decimal Costo { get; set; }
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
public class ActualizarProveedorDto
{
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [RegularExpression(@"^$|^3\d{9}$", ErrorMessage = "El teléfono debe tener 10 dígitos y empezar por 3.")]
    public string? Telefono { get; set; }

    [RegularExpression(@"^$|^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }
}