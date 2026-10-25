using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

// ---------- Ítems del producto ----------
// Un producto se compone de recetas (mínimo una) y opcionalmente de
// insumos directos (empaques) que no forman parte de ninguna receta.
// Los insumos directos SÍ se persisten en ProductoMenuInsumo y entran en
// el costo, el margen y la evaluación del menú.
public sealed class RecetaEnProductoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una receta.")]
    public int RecetaId { get; set; }

    // Cantidad = porciones de esta receta que entran en el producto:
    // el costo se calcula como (costo de insumos ÷ porciones base) × porciones.
    [Range(0.001, 10000, ErrorMessage = "La cantidad debe estar entre 0.001 y 10000.")]
    public decimal Cantidad { get; set; }
}

public sealed class InsumoDirectoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un insumo.")]
    public int InsumoId { get; set; }

    [Range(0.001, 100000, ErrorMessage = "La cantidad debe estar entre 0.001 y 100000.")]
    public decimal Cantidad { get; set; }

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [RegularExpression(@"(?i)^(g|kg|ml|l|oz|lb|unidad|taza)$", ErrorMessage = "Unidad de medida no válida.")]
    public string UnidadMedida { get; set; } = "unidad";
}

// ---------- Crear (POST) ----------
public sealed class CrearProductoMenuDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(75, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 75 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})[\p{L}\p{N} .,'\-()&]+$",
        ErrorMessage = "El nombre solo admite letras, números, espacios y . , ' - ( ) & (y debe incluir letras).")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0.01, 999999.99, ErrorMessage = "El precio de venta no puede exceder los 6 dígitos (máximo 999.999,99).")]
    public decimal? PrecioVenta { get; set; }

    public bool? Activo { get; set; }

    [MinLength(1, ErrorMessage = "El producto necesita al menos una receta.")]
    [MaxLength(50, ErrorMessage = "Máximo 50 recetas por producto.")]
    public List<RecetaEnProductoDto> Recetas { get; set; } = new();

    [MaxLength(50, ErrorMessage = "Máximo 50 insumos directos por producto.")]
    public List<InsumoDirectoDto> InsumosDirectos { get; set; } = new();
}

// ---------- Actualizar total (PUT estandarizado) ----------
public sealed class ActualizarProductoMenuDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(75, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 75 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})[\p{L}\p{N} .,'\-()&]+$",
        ErrorMessage = "El nombre solo admite letras, números, espacios y . , ' - ( ) & (y debe incluir letras).")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0.01, 999999.99, ErrorMessage = "El precio de venta no puede exceder los 6 dígitos (máximo 999.999,99).")]
    public decimal? PrecioVenta { get; set; }

    [Required(ErrorMessage = "Indica si el producto está activo.")]
    public bool Activo { get; set; } = true;

    [MinLength(1, ErrorMessage = "El producto necesita al menos una receta.")]
    [MaxLength(50, ErrorMessage = "Máximo 50 recetas por producto.")]
    public List<RecetaEnProductoDto> Recetas { get; set; } = new();

    [MaxLength(50, ErrorMessage = "Máximo 50 insumos directos por producto.")]
    public List<InsumoDirectoDto> InsumosDirectos { get; set; } = new();
}

// ---------- Actualizar parcial (PATCH: solo campos editados) ----------
public sealed class PatchProductoMenuDto
{
    [StringLength(75, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 75 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})[\p{L}\p{N} .,'\-()&]+$",
        ErrorMessage = "El nombre solo admite letras, números, espacios y . , ' - ( ) & (y debe incluir letras).")]
    public string? Nombre { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "El precio de venta no puede exceder los 6 dígitos (máximo 999.999,99).")]
    public decimal? PrecioVenta { get; set; }

    // Para quitar el precio con PATCH se usa QuitarPrecioVenta = true.
    // (decimal? solo no distingue "no enviado" de "quitar".)
    public bool? QuitarPrecioVenta { get; set; }

    public bool? Activo { get; set; }

    [MinLength(1, ErrorMessage = "El producto necesita al menos una receta.")]
    [MaxLength(50, ErrorMessage = "Máximo 50 recetas por producto.")]
    public List<RecetaEnProductoDto>? Recetas { get; set; }

    // null = no cambia los empaques guardados; lista (aunque vacía) = reemplaza.
    [MaxLength(50, ErrorMessage = "Máximo 50 insumos directos por producto.")]
    public List<InsumoDirectoDto>? InsumosDirectos { get; set; }
}

// ---------- Lectura ----------
public sealed class RecetaDetalleProductoDto
{
    public int RecetaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    // Porciones de esta receta que entran en el producto.
    public decimal Cantidad { get; set; }
    // Costo por porción de la receta (insumos ÷ porciones base).
    public decimal CostoUnitario { get; set; }
    public decimal CostoAportado { get; set; }
}

public sealed class InsumoDirectoDetalleDto
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadReceta { get; set; } = string.Empty;
    public string UnidadInsumo { get; set; } = string.Empty;
    public decimal CostoAportado { get; set; }
    public string FuenteCosto { get; set; } = "proveedor_principal";
}

public class ProductoMenuDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? PrecioVenta { get; set; }
    public decimal CostoTotal { get; set; }

    // Costo directo = recetas + empaques, sin costos fijos. Es el que se usa
    // para el punto de equilibrio y el food cost.
    public decimal CostoDirecto { get; set; }

    // Cuota de costos fijos prorrateada incluida en CostoTotal.
    public decimal CostoFijoProrrateado { get; set; }

    public decimal? MargenReal { get; set; }

    // Margen calculado solo con el costo directo (recetas + empaques).
    // Es la base del menú y de la sugerencia de precio por defecto.
    public decimal? MargenDirecto { get; set; }

    public int NumRecetas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class ProductoMenuDetalleDto : ProductoMenuDto
{
    public List<RecetaDetalleProductoDto> Recetas { get; set; } = new();
    public List<InsumoDirectoDetalleDto> InsumosDirectos { get; set; } = new();
    public decimal CostoRecetas { get; set; }
    public decimal CostoInsumosDirectos { get; set; }
    public List<string> Advertencias { get; set; } = new();
}

// ---------- Costeo del producto ----------
// costoTotalProducto = Σ (costoPorPorcionDeReceta × porciones) + Σ (costo insumo directo) + fijos prorrateados
// margenReal = (precioVenta − costoTotal) ÷ precioVenta × 100   (margen sobre el precio de venta)
public sealed class CostoProductoDto
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public decimal CostoRecetas { get; set; }
    public decimal CostoInsumosDirectos { get; set; }

    // Costo directo = recetas + empaques (sin fijos). Es el costo real de producción.
    public decimal CostoDirecto { get; set; }

    // Cuota mensual de costos fijos prorrateada que se suma al costo completo.
    public decimal CostoFijoProrrateado { get; set; }

    // Preparaciones base que consume el producto:
    // Σ (porciones usadas ÷ porciones base de cada receta).
    public decimal PreparacionesEquivalentes { get; set; }

    public decimal CostoTotal { get; set; }
    public decimal? PrecioVenta { get; set; }
    public decimal? MargenReal { get; set; }

    // Margen sobre el precio de venta usando solo el costo directo (sin fijos).
    public decimal? MargenDirecto { get; set; }

    // SinCostosFijos = no se aplicó ningún fijo al cálculo.
    // HayCostosFijosConfigurados = el negocio tiene fijos activos configurados.
    public bool SinCostosFijos { get; set; }
    public bool HayCostosFijosConfigurados { get; set; }
    public List<RecetaDetalleProductoDto> Recetas { get; set; } = new();
    public List<InsumoDirectoDetalleDto> InsumosDirectos { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public sealed class PreviewCostoProductoDto
{
    public List<RecetaEnProductoDto> Recetas { get; set; } = new();
    public List<InsumoDirectoDto> InsumosDirectos { get; set; } = new();
    public List<int>? CostosFijosIds { get; set; }
}

// ---------- Precio sugerido del menú ----------
public sealed class SugerirPrecioProductoDto
{
    public decimal MargenDeseado { get; set; }

    // null = todos los activos, lista vacía = ninguno (costo directo),
    // lista con IDs = solo esos costos fijos. La interfaz envía una
    // selección explícita para no cargar todos los fijos por accidente.
    public List<int>? CostosFijosIds { get; set; }
}

public sealed class PreviewPrecioProductoDto
{
    public List<RecetaEnProductoDto> Recetas { get; set; } = new();
    public List<InsumoDirectoDto> InsumosDirectos { get; set; } = new();
    public decimal MargenDeseado { get; set; }
    public List<int>? CostosFijosIds { get; set; }
}

public sealed class PrecioSugeridoProductoDto
{
    public int? ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;

    // Costo base usado para calcular el precio sugerido:
    // es el directo si no se seleccionaron fijos, o el completo si se incluyeron.
    public decimal CostoTotal { get; set; }

    public decimal CostoDirecto { get; set; }
    public decimal CostoFijoProrrateado { get; set; }
    public bool IncluyeCostosFijos { get; set; }

    public decimal MargenDeseado { get; set; }
    public decimal? PrecioSugerido { get; set; }
    public bool MargenValido { get; set; }
    public List<string> Advertencias { get; set; } = new();
}

public sealed class ResultadoProductoMenuDto
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public ProductoMenuDetalleDto? Producto { get; init; }

    public static ResultadoProductoMenuDto Ok(ProductoMenuDetalleDto p, string mensaje) =>
        new() { Exito = true, Producto = p, Mensaje = mensaje };
    public static ResultadoProductoMenuDto Error(string mensaje) => new() { Mensaje = mensaje };
    public static ResultadoProductoMenuDto NoHallado(string mensaje) => new() { NoEncontrado = true, Mensaje = mensaje };
}
