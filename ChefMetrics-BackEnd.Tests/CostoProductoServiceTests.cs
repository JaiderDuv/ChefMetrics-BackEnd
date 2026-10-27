using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class CostoProductoServiceTests
{
    private const int CuentaId = 7;
    private const int NegocioId = 3;

    [Fact]
    public async Task Suma_recetas_y_empaques_y_calcula_el_margen_directo()
    {
        var harina = new Insumo
        {
            Id = 1,
            NegocioId = NegocioId,
            Nombre = "Harina",
            UnidadMedida = "g",
            CostoUnitario = 1_000m,
            Cantidad = 1_000m,
            Activo = true
        };
        var caja = new Insumo
        {
            Id = 2,
            NegocioId = NegocioId,
            Nombre = "Caja",
            UnidadMedida = "unidad",
            CostoUnitario = 500m,
            Cantidad = 1m,
            Activo = true
        };
        var receta = new Receta
        {
            Id = 10,
            NegocioId = NegocioId,
            Nombre = "Masa base",
            PorcionesBase = 4
        };
        receta.RecetaInsumos.Add(new RecetaInsumo
        {
            Id = 1,
            RecetaId = receta.Id,
            InsumoId = harina.Id,
            Insumo = harina,
            Cantidad = 500m,
            UnidadMedida = "g",
            PorcentajeMerma = 0m
        });

        var productos = new Mock<IProductoMenuRepository>(MockBehavior.Strict);
        productos.Setup(p => p.ObtenerRecetasConInsumosAsync(NegocioId, It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Receta> { receta });
        productos.Setup(p => p.ObtenerInsumosActivosAsync(NegocioId, It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Insumo> { caja });

        var costeo = new Mock<ICosteoRepository>(MockBehavior.Strict);
        costeo.Setup(c => c.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, PrecioProveedorPrincipal>());
        costeo.Setup(c => c.ListarCostosFijosAsync(NegocioId, true))
            .ReturnsAsync(new List<CostoFijo>());
        costeo.Setup(c => c.ObtenerPreparacionesBaseMesAsync(NegocioId))
            .ReturnsAsync((int?)null);

        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);

        var servicio = new CostoProductoService(productos.Object, costeo.Object, insumos.Object);

        var resultado = await servicio.CalcularPreviewAsync(CuentaId, new PreviewCostoProductoDto
        {
            Recetas = { new RecetaEnProductoDto { RecetaId = receta.Id, Cantidad = 4 } },
            InsumosDirectos = { new InsumoDirectoDto { InsumoId = caja.Id, Cantidad = 1, UnidadMedida = "unidad" } },
            CostosFijosIds = new List<int>()
        });

        Assert.True(resultado.Exito);
        var dto = resultado.Datos!;
        Assert.Equal(500m, dto.CostoRecetas);
        Assert.Equal(500m, dto.CostoInsumosDirectos);
        Assert.Equal(1_000m, dto.CostoDirecto);
        Assert.Equal(1_000m, dto.CostoTotal);
    }

    [Fact]
    public async Task Costea_empaque_en_tazas_si_el_insumo_tiene_densidad()
    {
        var harina = new Insumo
        {
            Id = 1,
            NegocioId = NegocioId,
            Nombre = "Harina",
            UnidadMedida = "g",
            DensidadGPorMl = 0.53m,
            CostoUnitario = 1_000m,
            Cantidad = 1_000m,
            Activo = true
        };
        var receta = new Receta
        {
            Id = 10,
            NegocioId = NegocioId,
            Nombre = "Masa base",
            PorcionesBase = 1
        };
        receta.RecetaInsumos.Add(new RecetaInsumo
        {
            Id = 1,
            RecetaId = receta.Id,
            InsumoId = harina.Id,
            Insumo = harina,
            Cantidad = 1m,
            UnidadMedida = "taza",
            PorcentajeMerma = 0m
        });

        var productos = new Mock<IProductoMenuRepository>(MockBehavior.Strict);
        productos.Setup(p => p.ObtenerRecetasConInsumosAsync(NegocioId, It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<Receta> { receta });

        var costeo = new Mock<ICosteoRepository>(MockBehavior.Strict);
        costeo.Setup(c => c.ObtenerCostosProveedorPrincipalAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, PrecioProveedorPrincipal>());
        costeo.Setup(c => c.ListarCostosFijosAsync(NegocioId, true))
            .ReturnsAsync(new List<CostoFijo>());
        costeo.Setup(c => c.ObtenerPreparacionesBaseMesAsync(NegocioId))
            .ReturnsAsync((int?)null);

        var insumos = new Mock<IInsumoRepository>(MockBehavior.Strict);
        insumos.Setup(i => i.ObtenerNegocioIdPorCuentaAsync(CuentaId)).ReturnsAsync(NegocioId);

        var servicio = new CostoProductoService(productos.Object, costeo.Object, insumos.Object);

        var resultado = await servicio.CalcularPreviewAsync(CuentaId, new PreviewCostoProductoDto
        {
            Recetas = { new RecetaEnProductoDto { RecetaId = receta.Id, Cantidad = 1 } },
            CostosFijosIds = new List<int>()
        });

        Assert.True(resultado.Exito);
        Assert.Equal(127.2m, resultado.Datos!.CostoDirecto);
    }
}
