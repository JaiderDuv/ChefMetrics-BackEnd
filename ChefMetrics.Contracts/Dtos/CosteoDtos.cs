using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

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
// Cotización del proveedor principal: Costo es el TOTAL del paquete
// (Cantidad + UnidadMedida). El precio por unidad se calcula dividiendo.
public sealed record PrecioProveedorPrincipal(decimal Costo, decimal Cantidad, string UnidadMedida);
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
    // SinCostosFijos describe el cálculo: true si no se aplicó ningún fijo.
    // HayCostosFijosConfigurados distingue "no hay fijos configurados" de
    // "hay fijos, pero no se seleccionó ninguno".
    public bool SinCostosFijos { get; set; }
    public bool HayCostosFijosConfigurados { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<LineaCostoDto> Lineas { get; set; } = new();
}

public sealed class EscaladoRecetaDto
{
    public int RecetaId { get; set; }
    public string Receta { get; set; } = string.Empty;
    public int PorcionesBase { get; set; }
    public int PorcionesDeseadas { get; set; }
    public decimal FactorEscala { get; set; }
    public decimal CostoInsumos { get; set; }
    public decimal CostoMermaAplicada { get; set; }
    public decimal CostoFijoProrrateado { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal CostoPorPorcion { get; set; }
    public bool SinCostosFijos { get; set; }
    public bool HayCostosFijosConfigurados { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<LineaCostoDto> Lineas { get; set; } = new();
}

// ---------- Costos fijos ----------
public sealed class CostoFijoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Otros";
    public decimal MontoMensual { get; set; }
    public bool Activo { get; set; }
}

public sealed class CrearCostoFijoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    // Si no se envía, el servicio lo guarda como "Otros" (compatibilidad).
    [StringLength(50, MinimumLength = 2, ErrorMessage = "La categoría debe tener entre 2 y 50 caracteres.")]
    public string Categoria { get; set; } = "Otros";

    [Range(0.01, 9999999999, ErrorMessage = "El monto mensual debe ser mayor a 0.")]
    public decimal MontoMensual { get; set; }

    public bool? Activo { get; set; }
}

public sealed class ActualizarCostoFijoDto
{
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(50, MinimumLength = 2, ErrorMessage = "La categoría debe tener entre 2 y 50 caracteres.")]
    public string? Categoria { get; set; }

    [Range(0.01, 9999999999, ErrorMessage = "El monto mensual debe ser mayor a 0.")]
    public decimal? MontoMensual { get; set; }

    public bool? Activo { get; set; }
}

// El divisor es único por negocio: todas las filas de CostoFijo usan el mismo
// número de preparaciones base al mes.
public sealed class ConfiguracionCostosFijosDto
{
    public int? PreparacionesBaseMes { get; set; }
    public List<CostoFijoDto> Costos { get; set; } = new();
}

public sealed class ActualizarPreparacionesBaseMesDto
{
    // Null permite limpiar el valor; si se envía, debe ser un entero positivo.
    [Range(1, 1000000, ErrorMessage = "Las preparaciones base al mes deben estar entre 1 y 1.000.000.")]
    public int? PreparacionesBaseMes { get; set; }
}