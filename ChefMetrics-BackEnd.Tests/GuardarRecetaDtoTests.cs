using System.ComponentModel.DataAnnotations;
using ChefMetrics.Contracts;

namespace ChefMetrics_BackEnd.Tests;

public sealed class GuardarRecetaDtoTests
{
    [Fact]
    public void Acepta_categoria_y_porciones_validas()
    {
        var errores = Validar(new GuardarRecetaDto
        {
            Nombre = "Pan de masa madre",
            Categoria = "panaderia",
            PorcionesBase = 10,
            Insumos = [Insumo()]
        });

        Assert.Empty(errores);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Rechaza_porciones_fuera_del_rango(int porciones)
    {
        var errores = Validar(new GuardarRecetaDto
        {
            Nombre = "Pan de masa madre",
            Categoria = "panaderia",
            PorcionesBase = porciones,
            Insumos = [Insumo()]
        });

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(GuardarRecetaDto.PorcionesBase)));
    }

    [Fact]
    public void La_categoria_no_se_valida_como_numero_de_porciones()
    {
        var errores = Validar(new GuardarRecetaDto
        {
            Nombre = "Pan de masa madre",
            Categoria = "panaderia",
            PorcionesBase = 10,
            Insumos = [Insumo()]
        });

        Assert.DoesNotContain(errores, e => e.MemberNames.Contains(nameof(GuardarRecetaDto.Categoria)));
    }

    private static RecetaInsumoDto Insumo() => new()
    {
        InsumoId = 1,
        Cantidad = 1,
        UnidadMedida = "g"
    };

    private static List<ValidationResult> Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }
}
