using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public sealed class UmbralMargenDto
{
    public decimal UmbralMargenMinimo { get; set; }
}

public sealed class ActualizarUmbralDto
{
    [Range(0, 99999.99, ErrorMessage = "El umbral no puede ser negativo.")]
    public decimal UmbralMargenMinimo { get; set; }
}

public sealed class ProductoEvaluacionDto
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CostoTotal { get; set; }
    public decimal? PrecioVenta { get; set; }
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