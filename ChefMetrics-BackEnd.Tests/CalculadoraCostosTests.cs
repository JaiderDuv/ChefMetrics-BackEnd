using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CalculadoraCostosTests
{
    [Fact]
    public void FijoProrrateado_suma_los_montos_y_divide_entre_el_divisor_global()
    {
        var resultado = CalculadoraCostos.FijoProrrateado(new[] { 100_000m, 7_588m }, 60m);

        Assert.Equal(1_793.13m, decimal.Round(resultado, 2));
    }

    [Fact]
    public void FijoProrrateado_devuelve_cero_si_no_hay_montos()
    {
        var resultado = CalculadoraCostos.FijoProrrateado(Array.Empty<decimal>(), 60m);

        Assert.Equal(0m, resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FijoProrrateado_devuelve_cero_si_el_divisor_no_es_positivo(int divisor)
    {
        var resultado = CalculadoraCostos.FijoProrrateado(new[] { 100_000m }, divisor);

        Assert.Equal(0m, resultado);
    }
}
