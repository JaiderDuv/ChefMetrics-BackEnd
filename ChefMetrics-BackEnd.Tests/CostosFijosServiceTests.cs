using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CostosFijosServiceTests
{
    private const int CuentaId = 7;
    private const int NegocioId = 3;

    [Fact]
    public async Task Guarda_el_divisor_global_y_devuelve_la_configuracion()
    {
        var (servicio, repo, _) = CrearServicio();
        repo.Setup(r => r.ActualizarPreparacionesBaseMesAsync(NegocioId, 60)).ReturnsAsync(true);
        repo.Setup(r => r.ObtenerPreparacionesBaseMesAsync(NegocioId)).ReturnsAsync(60);
        repo.Setup(r => r.ListarCostosFijosAsync(NegocioId, false)).ReturnsAsync(new List<CostoFijo>
        {
            new() { Id = 1, Nombre = "Arriendo", Categoria = "Arriendo", MontoMensual = 100_000m, Activo = true }
        });

        var resultado = await servicio.ActualizarPreparacionesBaseMesAsync(
            CuentaId, new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = 60 });

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Equal(60, resultado.Datos!.PreparacionesBaseMes);
        Assert.Single(resultado.Datos.Costos);
        Assert.Equal("Arriendo", resultado.Datos.Costos[0].Nombre);
        repo.Verify(r => r.ActualizarPreparacionesBaseMesAsync(NegocioId, 60), Times.Once);
    }

    [Fact]
    public async Task Permite_limpiar_el_divisor_global()
    {
        var (servicio, repo, _) = CrearServicio();
        repo.Setup(r => r.ActualizarPreparacionesBaseMesAsync(NegocioId, null)).ReturnsAsync(true);
        repo.Setup(r => r.ObtenerPreparacionesBaseMesAsync(NegocioId)).ReturnsAsync((int?)null);
        repo.Setup(r => r.ListarCostosFijosAsync(NegocioId, false)).ReturnsAsync(new List<CostoFijo>());

        var resultado = await servicio.ActualizarPreparacionesBaseMesAsync(
            CuentaId, new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = null });

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Datos);
        Assert.Null(resultado.Datos!.PreparacionesBaseMes);
        Assert.Empty(resultado.Datos.Costos);
        repo.Verify(r => r.ActualizarPreparacionesBaseMesAsync(NegocioId, null), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public async Task Rechaza_valores_fuera_del_rango_sin_guardar(int valor)
    {
        var (servicio, repo, _) = CrearServicio();

        var resultado = await servicio.ActualizarPreparacionesBaseMesAsync(
            CuentaId, new ActualizarPreparacionesBaseMesDto { PreparacionesBaseMes = valor });

        Assert.False(resultado.Exito);
        Assert.Contains("1.000.000", resultado.Mensaje);
        repo.VerifyNoOtherCalls();
    }

    private static (ICostosFijosService Servicio, Mock<ICosteoRepository> Repo, Mock<IInsumoRepository> Insumos) CrearServicio()
    {
        var repo = new Mock<ICosteoRepository>(MockBehavior.Strict);
        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);
        var servicio = new CostosFijosService(repo.Object, insumos.Object);
        return (servicio, repo, insumos);
    }
}
