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

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 0.9)]
    [InlineData(50, 0.5)]
    [InlineData(100, 0.01)]
    public void Rendimiento_aplica_la_merma_y_evita_division_por_cero(decimal merma, decimal esperado)
    {
        Assert.Equal(esperado, CalculadoraCostos.Rendimiento(merma));
    }

    [Fact]
    public void Cantidad_para_comprar_incluye_la_merma()
    {
        Assert.Equal(555.5556m, decimal.Round(CalculadoraCostos.CantidadParaComprar(500m, 10m), 4));
    }

    [Fact]
    public void Merma_extra_es_el_sobrecosto_de_comprar_por_la_merma()
    {
        Assert.Equal(55.56m, decimal.Round(CalculadoraCostos.MermaExtra(500m, 1m, 10m), 2));
    }

    [Fact]
    public void Linea_sin_densidad_no_cruza_masa_y_volumen()
    {
        var linea = CalculadoraCostos.Linea(
            insumoId: 1,
            nombre: "Harina",
            cantidad: 1m,
            unidadReceta: "taza",
            unidadInsumo: "g",
            merma: 0m,
            costo: 1m,
            fuente: "costo_unitario_respaldo");

        Assert.Equal(0m, linea.CostoInsumo);
        Assert.Equal(0m, linea.CantidadConvertida);
    }
}
