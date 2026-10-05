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

    [Required(ErrorMessage = "El costo unitario es obligatorio.")]
    public decimal CostoUnitario { get; set; }

    [Required(ErrorMessage = "El porcentaje de merma es obligatorio.")]
    public decimal PorcentajeMerma { get; set; }

    [StringLength(150, ErrorMessage = "El proveedor no puede superar los 150 caracteres.")]
    public string? Proveedor { get; set; }
}

// PATCH /api/insumos/{id} — todos los campos opcionales; solo se actualiza lo enviado.
public class ActualizarInsumoDto
{
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(10, ErrorMessage = "La unidad de medida no puede superar los 10 caracteres.")]
    public string? UnidadMedida { get; set; }

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

    // Valores permitidos: "nombre", "costo", "merma", "fecha". Otro valor => "nombre".
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
