using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Tests;

public sealed class ConversorUnidadesTests
{
    [Theory]
    [InlineData(1, "kg", "g", 1000)]
    [InlineData(1, "lb", "g", 453.592)]
    [InlineData(1, "cda", "ml", 15)]
    [InlineData(1, "cdta", "ml", 5)]
    [InlineData(1, "taza", "ml", 240)]
    [InlineData(1, "gal", "l", 3.78541)]
    [InlineData(1, "docena", "unidad", 12)]
    public void Convierte_unidades_de_la_misma_familia(decimal cantidad, string desde, string hacia, decimal esperado)
    {
        var resultado = ConversorUnidades.Convertir(cantidad, desde, hacia);

        Assert.NotNull(resultado);
        Assert.Equal(esperado, resultado!.Value, 4);
    }

    [Fact]
    public void Convierte_masa_a_volumen_con_densidad()
    {
        // 1 taza de harina con densidad 0,53 g/ml = 240 ml * 0,53 = 127,2 g.
        var resultado = ConversorUnidades.Convertir(1m, "taza", "g", 0.53m);

        Assert.NotNull(resultado);
        Assert.Equal(127.2m, resultado!.Value, 4);
    }

    [Fact]
    public void Convierte_volumen_a_masa_con_densidad()
    {
        var resultado = ConversorUnidades.Convertir(100m, "g", "taza", 0.53m);

        Assert.NotNull(resultado);
        Assert.Equal(0.7862m, resultado!.Value, 4);
    }

    [Fact]
    public void No_convierte_entre_masa_y_volumen_sin_densidad()
    {
        Assert.Null(ConversorUnidades.Convertir(1m, "taza", "g"));
        Assert.False(ConversorUnidades.Compatibles("taza", "g"));
        Assert.False(ConversorUnidades.Compatibles("taza", "g", 0m));
    }

    [Fact]
    public void Compatibles_acepta_masa_y_volumen_si_hay_densidad()
    {
        Assert.True(ConversorUnidades.Compatibles("taza", "g", 0.53m));
        Assert.Contains("g", ConversorUnidades.UnidadesCompatibles("taza", 0.53m));
        Assert.DoesNotContain("g", ConversorUnidades.UnidadesCompatibles("taza", null));
    }

    [Fact]
    public void Precio_por_unidad_normaliza_el_paquete_a_la_unidad_del_insumo()
    {
        // Un paquete de 1.000 g por 2.500 cuesta 2.500 por kg (1 kg = 1.000 g).
        var precio = ConversorUnidades.PrecioPorUnidad(2_500m, 1_000m, "g", "kg");

        Assert.NotNull(precio);
        Assert.Equal(2_500m, precio!.Value, 4);
    }

    [Fact]
    public void Precio_por_unidad_falla_si_el_paquete_no_es_compatible()
    {
        Assert.Null(ConversorUnidades.PrecioPorUnidad(2_500m, 1m, "taza", "g"));
        Assert.NotNull(ConversorUnidades.PrecioPorUnidad(2_500m, 1m, "taza", "g", 0.53m));
    }

    [Fact]
    public void Costo_de_linea_aplica_conversion_y_merma()
    {
        // 1 taza de harina = 127,2 g; con 10 % de merma se compran 141,333 g.
        var costo = ConversorUnidades.CostoLinea(
            cantidad: 1m,
            unidadReceta: "taza",
            unidadInsumo: "g",
            costoUnitario: 0.005m,
            mermaPorcentaje: 10m,
            densidadGPorMl: 0.53m);

        Assert.Equal(0.71m, costo);
    }

    [Theory]
    [InlineData("taza", true)]
    [InlineData("cda", true)]
    [InlineData("cdta", true)]
    [InlineData("unidad", true)]
    [InlineData("docena", true)]
    [InlineData("kg", false)]
    [InlineData("gal", false)]
    public void Identifica_unidades_pequenas(string unidad, bool esperado)
    {
        Assert.Equal(esperado, ConversorUnidades.EsUnidadPequena(unidad));
    }

    [Fact]
    public void El_patron_de_validacion_incluye_todas_las_unidades_del_catalogo()
    {
        foreach (var unidad in ConversorUnidades.Unidades)
        {
            Assert.True(ConversorUnidades.EsValida(unidad));
        }

        Assert.Equal(ConversorUnidades.Unidades.Count, ConversorUnidades.Unidades.Distinct().Count());
    }
}
