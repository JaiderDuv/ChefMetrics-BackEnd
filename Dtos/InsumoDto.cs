using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public class CrearInsumoDto
{
    [Required(ErrorMessage = "El nombre del insumo es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(10, ErrorMessage = "La unidad de medida no puede superar los 10 caracteres.")]
    public string UnidadMedida { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "La categoría no puede superar los 20 caracteres.")]
    public string? Categoria { get; set; }

    [Required(ErrorMessage = "El costo unitario es obligatorio.")]
    public decimal CostoUnitario { get; set; }

    [Required(ErrorMessage = "El porcentaje de merma es obligatorio.")]
    public decimal PorcentajeMerma { get; set; }

    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    [StringLength(150, ErrorMessage = "El proveedor no puede superar los 150 caracteres.")]
    public string? Proveedor { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Cantidad { get; set; }

    public bool? Activo { get; set; }
}

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

    [Range(0, double.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Cantidad { get; set; }

    public bool? Activo { get; set; }
}

public class InsumoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal CostoUnitario { get; set; }
    public decimal PorcentajeMerma { get; set; }
    public string? Proveedor { get; set; }
    public decimal? Cantidad { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public int NumeroRecetas { get; set; }
}

public class FiltrosInsumoDto
{
    public string? Busqueda { get; set; }

    public string? Categoria { get; set; }

    public string? OrdenarPor { get; set; } = "nombre";

    public string? Direccion { get; set; } = "asc";

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

    public bool NoEncontrado { get; set; }
    public bool Conflicto { get; set; }
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

    public bool BloqueadoPorRecetas { get; set; }
    public int NumeroRecetas { get; set; }
}

public class HistorialPrecioDto
{
    public DateTime Fecha { get; set; }
    public decimal Precio { get; set; }
    public string? Proveedor { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }

    public decimal? VariacionPorcentual { get; set; }

    public decimal? Diferencia { get; set; }

    public bool? EsSubida { get; set; }
}

public class HistorialPreciosInsumoDto
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? ProveedorActual { get; set; }
    public decimal PrecioActual { get; set; }

    public bool HistorialCompletoDisponible { get; set; }

    public bool InsumoEliminado { get; set; }

    public List<HistorialPrecioDto> Precios { get; set; } = new();
}

public class ResultadoHistorialDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public bool NoEncontrado { get; set; }
    public HistorialPreciosInsumoDto? Historial { get; set; }
}

public class HistorialPrecioGeneralDto
{
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Precio { get; set; }
    public string? Proveedor { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }

    public decimal? VariacionPorcentual { get; set; }

   public decimal? Diferencia { get; set; }

    public bool? EsSubida { get; set; }

    public bool InsumoEliminado { get; set; }
}

public class HistorialGeneralDto
{
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


