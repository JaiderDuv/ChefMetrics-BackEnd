using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public sealed class RecetaInsumoDto
{
    [Required]
    public int InsumoId { get; set; }

    // Solo lectura: se llena al consultar, se ignora al guardar.
    public string? InsumoNombre { get; set; }

    [Range(0.001, 1000000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal Cantidad { get; set; }
    
    [Required]
    public string UnidadMedida { get; set; } = "g";
}

// Sirve para crear (POST) y modificar (PUT).
public sealed class GuardarRecetaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Las porciones deben estar entre 1 y 1000.")]
    public int PorcionesBase { get; set; } = 1;

    public string? Instrucciones { get; set; }

    [MinLength(1, ErrorMessage = "La receta necesita al menos un insumo.")]
    public List<RecetaInsumoDto> Insumos { get; set; } = new();
}

public sealed class RecetaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int PorcionesBase { get; set; }
    public string? Instrucciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<RecetaInsumoDto> Insumos { get; set; } = new();
}

public sealed class ResultadoRecetaDto
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public RecetaDto? Receta { get; init; }

    public static ResultadoRecetaDto Ok(RecetaDto receta, string mensaje) =>
        new() { Exito = true, Receta = receta, Mensaje = mensaje };
    public static ResultadoRecetaDto Error(string mensaje) =>
        new() { Mensaje = mensaje };
    public static ResultadoRecetaDto NoHallado(string mensaje) =>
        new() { NoEncontrado = true, Mensaje = mensaje };
}

public sealed class ResultadoEliminarRecetaDto
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public bool BloqueadaPorMenu { get; init; }
    public int NumeroProductos { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}       