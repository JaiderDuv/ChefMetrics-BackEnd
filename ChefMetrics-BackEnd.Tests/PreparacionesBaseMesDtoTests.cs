using System.ComponentModel.DataAnnotations;
using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Tests;

public sealed class PreparacionesBaseMesDtoTests
{
    [Fact]
    public void Permite_null_para_limpiar_el_divisor()
    {
        var errores = Validar(new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = null });

        Assert.Empty(errores);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(1_000_000)]
    public void Acepta_valores_dentro_del_rango(int valor)
    {
        var errores = Validar(new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = valor });

        Assert.Empty(errores);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Rechaza_valores_fuera_del_rango(int valor)
    {
        var errores = Validar(new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = valor });

        Assert.NotEmpty(errores);
        Assert.Contains(errores, e => e.ErrorMessage?.Contains("1.000.000") == true);
    }

    private static List<ValidationResult> Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }
}
