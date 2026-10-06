using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

// POST /api/insumos
public class CrearInsumoDto
{
    [Required(ErrorMessage = "El nombre del insumo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(10, ErrorMessage = "La unidad de medida no puede superar los 10 caracteres.")]
    public string UnidadMedida { get; set; } = string.Empty;

    // Categoría del insumo (varchar(20)). Vacío => "otro".
    [StringLength(20, ErrorMessage = "La categoría no puede superar los 20 caracteres.")]
    public string? Categoria { get; set; }

    [Required(ErrorMessage = "El costo unitario es obligatorio.")]
    public decimal CostoUnitario { get; set; }

    [Required(ErrorMessage = "El porcentaje de merma es obligatorio.")]
    public decimal PorcentajeMerma { get; set; }

    [StringLength(150, ErrorMessage = "El proveedor no puede superar los 150 caracteres.")]
    public string? Proveedor { get; set; }

    // Opcional: si no se envía, el insumo se crea activo.
    public bool? Activo { get; set; }
}

// PATCH /api/insumos/{id} — todos los campos opcionales; solo se actualiza lo enviado.
public class ActualizarInsumoDto
{
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(10, ErrorMessage = "La unidad de medida no puede superar los 10 caracteres.")]
    public string? UnidadMedida { get; set; }

    [StringLength(20, ErrorMessage = "La categoría no puede superar los 20 caracteres.")]
    public string? Categoria { get; set; }

    public decimal? CostoUnitario { get; set; }

    public decimal? PorcentajeMerma { get; set; }

    [StringLength(150, ErrorMessage = "El proveedor no puede superar los 150 caracteres.")]
    public string? Proveedor { get; set; }

    public bool? Activo { get; set; }
}

// Respuesta: detalle de un insumo (GET, GET {id}, POST, PATCH).
public class InsumoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal CostoUnitario { get; set; }
    public decimal PorcentajeMerma { get; set; }
    public string? Proveedor { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaActualizacion { get; set; }

    // Cantidad de recetas que usan este insumo. Sirve al frontend
    // para saber si el insumo se puede eliminar (si > 0, no se puede).
    public int NumeroRecetas { get; set; }
}

// Query params del GET /api/insumos.
public class FiltrosInsumoDto
{
    // Busca por nombre o proveedor (contiene, sin distinción de mayúsculas).
    public string? Busqueda { get; set; }

    // Filtro exacto por categoría (varchar(20)).
    public string? Categoria { get; set; }

    // Valores permitidos: "nombre", "costo", "merma". Otro valor => "nombre".
    public string? OrdenarPor { get; set; } = "nombre";

    // Valores permitidos: "asc", "desc". Otro valor => "asc".
    public string? Direccion { get; set; } = "asc";

    // Por defecto solo activos.
    public bool IncluirInactivos { get; set; } = false;

    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 50;
}

public class ListaPaginadaDto<T>
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public List<T> Datos { get; set; } = new();
}

public class ResultadoInsumoDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    // true => el controlador responde 404; false + !Exito => 400.
    public bool NoEncontrado { get; set; }
    public InsumoDto? Insumo { get; set; }

    public static ResultadoInsumoDto Ok(InsumoDto insumo, string mensaje)
    {
        return new ResultadoInsumoDto { Exito = true, Mensaje = mensaje, Insumo = insumo };
    }

    public static ResultadoInsumoDto Error(string mensaje)
    {
        return new ResultadoInsumoDto { Exito = false, Mensaje = mensaje };
    }

    public static ResultadoInsumoDto NoHallado(string mensaje)
    {
        return new ResultadoInsumoDto { Exito = false, NoEncontrado = true, Mensaje = mensaje };
    }
}

public class ResultadoEliminarInsumoDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }

    // true => el controlador responde 409 Conflict.
    public bool BloqueadoPorRecetas { get; set; }
    public int NumeroRecetas { get; set; }
}

// GET /api/insumos/{id}/historial-precios — del más reciente al más viejo.
public class HistorialPrecioDto
{
    public DateTime Fecha { get; set; }
    public decimal Precio { get; set; }
    public string? Proveedor { get; set; }

    // % de variación frente al precio anterior del MISMO proveedor.
    // Null en el registro más antiguo de cada proveedor (no hay con qué comparar).
    public decimal? VariacionPorcentual { get; set; }

    // Diferencia en dinero frente al precio anterior (CostoNuevo - CostoAnterior).
    // Null si no hay precio anterior.
    public decimal? Diferencia { get; set; }

    // true = subió (rojo), false = bajó (verde), null = sin variación o sin previo.
    public bool? EsSubida { get; set; }
}

public class HistorialPreciosInsumoDto
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ProveedorActual { get; set; }
    public decimal PrecioActual { get; set; }

    // false => solo se conoce el precio vigente. El historial completo
    // requiere la tabla HistorialPrecioInsumo (ver InsumoService).
    public bool HistorialCompletoDisponible { get; set; }

    public List<HistorialPrecioDto> Precios { get; set; } = new();
}

public class ResultadoHistorialDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }
    public HistorialPreciosInsumoDto? Historial { get; set; }
}

// GET /api/insumos/historial-precios — global del negocio, del más reciente al más viejo.
public class HistorialPrecioGeneralDto
{
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Precio { get; set; }
    public string? Proveedor { get; set; }

    // % de variación frente al precio anterior del MISMO insumo y proveedor.
    public decimal? VariacionPorcentual { get; set; }

    // Diferencia en dinero frente al precio anterior (CostoNuevo - CostoAnterior).
    public decimal? Diferencia { get; set; }

    // true = subió (rojo), false = bajó (verde), null = sin variación o sin previo.
    public bool? EsSubida { get; set; }
}

public class HistorialGeneralDto
{
    // false => solo se conocen los precios vigentes (sin tabla de historial en BD).
    public bool HistorialCompletoDisponible { get; set; }

    public List<HistorialPrecioGeneralDto> Precios { get; set; } = new();
}

public class ResultadoHistorialGeneralDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }
    public HistorialGeneralDto? Historial { get; set; }
}
