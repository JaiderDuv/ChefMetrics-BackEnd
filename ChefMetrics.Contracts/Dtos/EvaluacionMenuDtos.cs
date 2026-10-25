using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

public sealed class UmbralMargenDto
{
    public decimal UmbralMargenMinimo { get; set; }
}

public sealed class ActualizarUmbralDto
{
    [Range(0, 100, ErrorMessage = "El umbral debe estar entre 0 y 100 %.")]
    public decimal UmbralMargenMinimo { get; set; }
}

public sealed class ProductoEvaluacionDto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Costo directo (recetas + empaques): base del margen y del precio.
    public decimal CostoDirecto { get; set; }

    // Costo completo con la cuota de fijos, para análisis.
    public decimal CostoTotal { get; set; }

    public decimal? PrecioVenta { get; set; }

    // Margen directo: el indicador con el que se evalúa el riesgo.
    public decimal? MargenDirecto { get; set; }

    // Margen con costo completo, solo como referencia para análisis.
    public decimal? MargenReal { get; set; }

    public bool MargenNoCalculable { get; set; }
    public string? MotivoNoCalculable { get; set; }
    public bool EnRiesgo { get; set; }
}

public sealed class EvaluacionMenuDto
{
    public decimal UmbralMargenMinimo { get; set; }
    public List<ProductoEvaluacionDto> Productos { get; set; } = new();
}

public sealed class ResultadoEvaluacionDto
{
    public bool Exito { get; init; }
    public bool NoEncontrado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public object? Datos { get; init; }

    public static ResultadoEvaluacionDto Ok(object datos, string mensaje = "") =>
        new() { Exito = true, Datos = datos, Mensaje = mensaje };
    public static ResultadoEvaluacionDto Error(string mensaje) => new() { Mensaje = mensaje };
    public static ResultadoEvaluacionDto NoHallado(string mensaje) => new() { NoEncontrado = true, Mensaje = mensaje };
}