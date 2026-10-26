using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CostoRecetaServiceTests
{
    private const int CuentaId = 7;
    private const int NegocioId = 3;
    private const int RecetaId = 10;

    [Fact]
    public async Task Sin_divisor_global_no_prorratea_fijos_y_advierte()
    {
        var (servicio, _) = CrearServicio(preparacionesBaseMes: null);

        var resultado = await servicio.CalcularAsync(CuentaId, RecetaId);

        Assert.True(resultado.Exito);
        var dto = resultado.Datos!;
        Assert.Equal(500m, dto.CostoInsumos);
        Assert.Equal(0m, dto.CostoFijoProrrateado);
        Assert.Equal(500m, dto.CostoTotal);
        Assert.False(dto.SinCostosFijos);
        Assert.Contains(dto.Advertencias, a => a.Contains("No has configurado las preparaciones base al mes"));
    }

    [Fact]
    public async Task Con_divisor_global_prorratea_los_fijos_sobre_la_preparacion()
    {
        var (servicio, _) = CrearServicio(preparacionesBaseMes: 60);

        var resultado = await servicio.CalcularAsync(CuentaId, RecetaId);

        Assert.True(resultado.Exito);
        var dto = resultado.Datos!;
        Assert.Equal(1_793.13m, dto.CostoFijoProrrateado);
        Assert.Equal(2_293.13m, dto.CostoTotal);
        Assert.Equal(573.28m, dto.CostoPorPorcion);
        Assert.DoesNotContain(dto.Advertencias, a => a.Contains("No has configurado las preparaciones base al mes"));
    }

    [Fact]
    public async Task Sin_costos_fijos_seleccionados_el_prorrateo_es_cero()
    {
        var (servicio, _) = CrearServicio(preparacionesBaseMes: 60);

        var resultado = await servicio.CalcularAsync(CuentaId, RecetaId, new List<int>());

        Assert.True(resultado.Exito);
        var dto = resultado.Datos!;
        Assert.True(dto.SinCostosFijos);
        Assert.Equal(0m, dto.CostoFijoProrrateado);
        Assert.Contains(dto.Advertencias, a => a.Contains("No seleccionaste ningún costo fijo"));
        Assert.DoesNotContain(dto.Advertencias, a => a.Contains("No has configurado las preparaciones base al mes"));
    }

    private static (ICostoRecetaService Servicio, Mock<ICosteoRepository> Repo) CrearServicio(int? preparacionesBaseMes)
    {
        var insumo = new Insumo
        {
            Id = 1,
            NegocioId = NegocioId,
            Nombre = "Harina",
            UnidadMedida = "g",
            Categoria = "Insumos",
            CostoUnitario = 1_000m,
            Cantidad = 1_000m,
            Activo = true
        };
        var receta = new Receta
        {
            Id = RecetaId,
            NegocioId = NegocioId,
            Nombre = "Masa base",
            PorcionesBase = 4
        };
        receta.RecetaInsumos.Add(new RecetaInsumo
        {
            Id = 1,
            RecetaId = RecetaId,
            InsumoId = insumo.Id,
            Insumo = insumo,
            Cantidad = 500m,
            UnidadMedida = "g",
            PorcentajeMerma = 0m
        });

        var repo = new Mock<ICosteoRepository>(MockBehavior.Strict);
        repo.Setup(r => r.ObtenerRecetaConInsumosAsync(NegocioId, RecetaId)).ReturnsAsync(receta);
        repo.Setup(r => r.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, PrecioProveedorPrincipal>());
        repo.Setup(r => r.ListarCostosFijosAsync(NegocioId, true))
            .ReturnsAsync(new List<CostoFijo>
            {
                new() { Id = 1, Nombre = "Arriendo", Categoria = "Arriendo", MontoMensual = 107_588m, Activo = true }
            });
        repo.Setup(r => r.ObtenerPreparacionesBaseMesAsync(NegocioId)).ReturnsAsync(preparacionesBaseMes);

        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);

        return (new CostoRecetaService(repo.Object, insumos.Object), repo);
    }
}
