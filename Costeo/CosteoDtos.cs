using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Costeo;

public sealed class ResultadoCosteo<T>
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public T? Datos { get; init; }

    public static ResultadoCosteo<T> Ok(T datos, string mensaje = "") => new() { Exito = true, Datos = datos, Mensaje = mensaje };
    public static ResultadoCosteo<T> Error(string mensaje) => new() { Mensaje = mensaje };
    public static ResultadoCosteo<T> NoHallado(string mensaje) => new() { NoEncontrado = true, Mensaje = mensaje };
}

// ---------- Costo de receta (CONTRATO que consume HU8.2) ----------
public sealed class LineaCostoDto
{
    public int InsumoId { get; set; }
    public string Insumo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadReceta { get; set; } = string.Empty;
    public string UnidadInsumo { get; set; } = string.Empty;
    public decimal CantidadConvertida { get; set; }
    public decimal CantidadParaComprar { get; set; }
    public decimal CostoProveedor { get; set; }
    public string FuenteCosto { get; set; } = "proveedor_principal";   // o "costo_unitario_respaldo"
    public decimal CostoInsumo { get; set; }
    public decimal MermaExtra { get; set; }
}

public sealed class CostoRecetaDto
{
    public int RecetaId { get; set; }
    public string Receta { get; set; } = string.Empty;
    public int PorcionesBase { get; set; }
    public decimal CostoInsumos { get; set; }
    public decimal CostoMermaAplicada { get; set; }
    public decimal CostoFijoProrrateado { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal CostoPorPorcion { get; set; }
    public bool SinCostosFijos { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<LineaCostoDto> Lineas { get; set; } = new();
}

// ---------- Costos fijos ----------
public sealed class CostoFijoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal MontoMensual { get; set; }
    public string UnidadProrrateo { get; set; } = string.Empty;
    public decimal ValorDivisor { get; set; }
    public bool Activo { get; set; }
}

public sealed class CrearCostoFijoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0.01, 9999999999, ErrorMessage = "El monto mensual debe ser mayor a 0.")]
    public decimal MontoMensual { get; set; }

    [Required(ErrorMessage = "La unidad de prorrateo es obligatoria.")]
    [StringLength(30)]
    public string UnidadProrrateo { get; set; } = "unidades_producidas";

    [Range(0.01, 1000000, ErrorMessage = "El valor divisor debe estar entre 0,01 y 1.000.000.")]
    public decimal ValorDivisor { get; set; } = 1;

    public bool? Activo { get; set; }
}

public sealed class ActualizarCostoFijoDto
{
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string? Nombre { get; set; }

    [Range(0.01, 9999999999, ErrorMessage = "El monto mensual debe ser mayor a 0.")]
    public decimal? MontoMensual { get; set; }

    [StringLength(30)]
    public string? UnidadProrrateo { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "El valor divisor debe estar entre 0,01 y 1.000.000.")]
    public decimal? ValorDivisor { get; set; }

    public bool? Activo { get; set; }
}