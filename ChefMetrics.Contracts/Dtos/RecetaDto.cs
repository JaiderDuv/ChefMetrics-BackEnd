using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

public sealed class RecetaInsumoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un insumo.")]
    public int InsumoId { get; set; }

    // Solo lectura
    public string? InsumoNombre { get; set; }
    public decimal CostoLinea { get; set; }

    [Range(0.001, 100000, ErrorMessage = "La cantidad debe estar entre 0.001 y 100000.")]
    public decimal Cantidad { get; set; }

    [Range(0, 100, ErrorMessage = "La merma debe estar entre 0 y 100 %.")]
    public decimal PorcentajeMerma { get; set; }

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [RegularExpression(ConversorUnidades.PatronUnidades, ErrorMessage = "Unidad de medida no válida.")]
    public string UnidadMedida { get; set; } = "g";
}

public sealed class GuardarRecetaDto

{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})[\p{L}\p{N} .,'\-()&]+$",
        ErrorMessage = "El nombre solo admite letras, números, espacios y . , ' - ( ) & (y debe incluir letras).")]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Las porciones deben estar entre 1 y 1000.")]

    [StringLength(30)]
    public string? Categoria { get; set; }

    public int PorcionesBase { get; set; } = 1;

    [StringLength(2000, ErrorMessage = "Las instrucciones no pueden superar 2000 caracteres.")]
    public string? Instrucciones { get; set; }

    [MinLength(1, ErrorMessage = "La receta necesita al menos un insumo.")]
    [MaxLength(50, ErrorMessage = "Máximo 50 insumos por receta.")]
    public List<RecetaInsumoDto> Insumos { get; set; } = new();
}

public sealed class RecetaDto
{
    public string? Categoria { get; set; }
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int PorcionesBase { get; set; }
    public string? Instrucciones { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime FechaCreacion { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal CostoPorPorcion { get; set; }
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
    public static ResultadoRecetaDto Error(string mensaje) => new() { Mensaje = mensaje };
    public static ResultadoRecetaDto NoHallado(string mensaje) => new() { NoEncontrado = true, Mensaje = mensaje };
}

public sealed class ResultadoEliminarRecetaDto
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public bool BloqueadaPorMenu { get; init; }
    public int NumeroProductos { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}