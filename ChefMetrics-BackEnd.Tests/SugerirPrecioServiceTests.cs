using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class SugerirPrecioServiceTests
{
    private const int CuentaId = 7;
    private const int RecetaId = 10;

    [Fact]
    public async Task Sugiere_precio_con_el_margen_deseado_sobre_el_costo_por_porcion()
    {
        var costos = new Mock<ICostoRecetaService>(MockBehavior.Strict);
        costos.Setup(c => c.CalcularAsync(CuentaId, RecetaId, It.IsAny<List<int>?>()))
            .ReturnsAsync(ResultadoCosteo<CostoRecetaDto>.Ok(new CostoRecetaDto
            {
                RecetaId = RecetaId,
                Receta = "Pastel",
                PorcionesBase = 4,
                CostoInsumos = 1_000m,
                CostoPorPorcion = 500m
            }));

        var servicio = new SugerirPrecioService(costos.Object);

        var resultado = await servicio.CalcularAsync(CuentaId, RecetaId, 20m);

        Assert.True(resultado.Exito);
        var dto = resultado.Datos!;
        Assert.True(dto.MargenValido);
        Assert.Equal(250m, dto.CostoDirectoPorPorcion);
        Assert.Equal(500m, dto.CostoPorPorcion);
        Assert.Equal(625m, dto.PrecioSugerido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100)]
    [InlineData(120)]
    public async Task No_sugiere_precio_con_un_margen_invalido(decimal margen)
    {
        var costos = new Mock<ICostoRecetaService>(MockBehavior.Strict);
        costos.Setup(c => c.CalcularAsync(CuentaId, RecetaId, It.IsAny<List<int>?>()))
            .ReturnsAsync(ResultadoCosteo<CostoRecetaDto>.Ok(new CostoRecetaDto
            {
                RecetaId = RecetaId,
                Receta = "Pastel",
                PorcionesBase = 4,
                CostoInsumos = 1_000m,
                CostoPorPorcion = 500m
            }));

        var servicio = new SugerirPrecioService(costos.Object);

        var resultado = await servicio.CalcularAsync(CuentaId, RecetaId, margen);

        Assert.True(resultado.Exito);
        Assert.False(resultado.Datos!.MargenValido);
        Assert.Null(resultado.Datos.PrecioSugerido);
        Assert.NotEmpty(resultado.Datos.Advertencias);
    }
}
