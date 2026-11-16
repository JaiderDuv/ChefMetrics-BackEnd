using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class ProveedorServiceTests
{
    private readonly Mock<IProveedorRepository> repo = new();
    private readonly Proveedor provider = new() { Id = 3, NegocioId = 2, Nombre = "Molino", Correo = "ventas@example.com", Telefono = "3001234567" };
    private readonly ProveedorService service;
    public ProveedorServiceTests()
    {
        repo.Setup(x => x.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        repo.Setup(x => x.ExisteInsumoEnNegocioAsync(2, 10)).ReturnsAsync(true);
        repo.Setup(x => x.ObtenerPorIdDelNegocioAsync(2, 3)).ReturnsAsync(provider);
        repo.Setup(x => x.ListarPorNegocioAsync(2)).ReturnsAsync(new List<Proveedor> { provider });
        repo.Setup(x => x.ListarPorInsumoAsync(10)).ReturnsAsync(new List<InsumoProveedor>());
        repo.Setup(x => x.ObtenerUnidadInsumoAsync(2, 10)).ReturnsAsync("g");
        repo.Setup(x => x.CrearAsync(It.IsAny<Proveedor>())).ReturnsAsync((Proveedor p) => { p.Id = 3; return p; });
        repo.Setup(x => x.AsociarAsync(10, 3, 100, 1000, "g")).ReturnsAsync(new InsumoProveedor { InsumoId = 10, ProveedorId = 3, Costo = 100, Cantidad = 1000, UnidadMedida = "g", EsPrincipal = true });
        service = new(repo.Object);
    }

    [Theory]
    [InlineData(0, 8)][InlineData(5, 5)][InlineData(100, 20)]
    public async Task Busqueda_limita_resultados_y_mapea_contactos(int limit, int expected)
    {
        repo.Setup(x => x.BuscarPorNombreAsync(2, "Mol", expected)).ReturnsAsync(new List<Proveedor> { provider });
        var result = await service.BuscarProveedoresAsync(7, "Mol", limit);
        var item = Assert.Single(Assert.IsType<List<ProveedorDto>>(result.Datos));
        Assert.Equal(provider.Correo, item.Correo);
        Assert.Empty(Assert.IsType<List<ProveedorDto>>((await service.BuscarProveedoresAsync(7, " ", limit)).Datos));
        Assert.Equal(3, Assert.Single(Assert.IsType<List<ProveedorDto>>((await service.ListarProveedoresDelNegocioAsync(7)).Datos)).Id);
    }

    [Fact]
    public async Task Compara_precio_por_unidad_y_excluye_proveedores_ya_asociados()
    {
        repo.Setup(x => x.ListarPorInsumoAsync(10)).ReturnsAsync(new List<InsumoProveedor> {
            new() { ProveedorId = 3, Proveedor = provider, Costo = 100, Cantidad = 1, UnidadMedida = "kg", EsPrincipal = true },
            new() { ProveedorId = 4, Proveedor = new Proveedor { Nombre = "Otro" }, Costo = 20, Cantidad = 100, UnidadMedida = "g" },
            new() { ProveedorId = 5, Proveedor = new Proveedor { Nombre = "Incompatible" }, Costo = 1, Cantidad = 1, UnidadMedida = "l" } });
        var rows = Assert.IsType<List<InsumoProveedorDto>>((await service.ListarPorInsumoAsync(7, 10)).Datos);
        Assert.Equal(0.1m, rows[0].PrecioUnitario);
        Assert.True(rows[0].EsMasBarato);
        Assert.False(rows[1].EsMasBarato);
        Assert.Null(rows[2].PrecioUnitario);
        Assert.Empty(Assert.IsType<List<ProveedorDto>>((await service.ListarDisponiblesParaInsumoAsync(7, 10)).Datos));
    }

    [Fact]
    public async Task Crear_asociar_y_crear_con_asociacion_persisten_datos_correctos()
    {
        var created = await service.CrearProveedorAsync(7, new() { Nombre = " Nuevo ", Correo = "nuevo@example.com" });
        Assert.Equal("Nuevo", Assert.IsType<ProveedorDto>(created.Datos).Nombre);
        repo.Verify(x => x.CrearAsync(It.Is<Proveedor>(p => p.NegocioId == 2 && p.Nombre == "Nuevo")), Times.Once);
        var linked = await service.AsociarAsync(7, 10, new() { ProveedorId = 3, Costo = 100, Cantidad = 1000, UnidadMedida = "g" });
        Assert.Equal(0.1m, Assert.IsType<InsumoProveedorDto>(linked.Datos).PrecioUnitario);
        var both = await service.CrearYAsociarAsync(7, 10, new() { Nombre = " Nuevo ", Costo = 100, Cantidad = 1000, UnidadMedida = "g" });
        Assert.Equal("Nuevo", Assert.IsType<InsumoProveedorDto>(both.Datos).Nombre);
        repo.Verify(x => x.SincronizarInsumoConPrincipalAsync(10), Times.Exactly(2));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Cambios_de_asociacion_sincronizan_solo_si_existe(bool exists)
    {
        repo.Setup(x => x.DesasociarAsync(10, 3)).ReturnsAsync(exists);
        repo.Setup(x => x.MarcarPrincipalAsync(10, 3)).ReturnsAsync(exists);
        repo.Setup(x => x.ActualizarCostoAsync(10, 3, 120, 1000, "g")).ReturnsAsync(exists);
        var results = new[] { await service.DesasociarAsync(7, 10, 3), await service.MarcarPrincipalAsync(7, 10, 3), await service.ActualizarCostoAsync(7, 10, 3, 120, 1000, "g") };
        Assert.All(results, r => { Assert.Equal(exists, r.Exito); Assert.Equal(!exists, r.NoEncontrado); });
        repo.Verify(x => x.SincronizarInsumoConPrincipalAsync(10), Times.Exactly(exists ? 3 : 0));
    }

    [Fact]
    public async Task Renombrar_propaga_nombre_y_eliminar_borra_asociaciones_primero()
    {
        repo.Setup(x => x.PropagarNombreAInsumosAsync(2, 3, "Nuevo")).ReturnsAsync(2);
        var updated = await service.ActualizarProveedorAsync(7, 3, new() { Nombre = " Nuevo ", Correo = " ", Telefono = " 3001234567 " });
        Assert.True(updated.Exito);
        Assert.Contains("2 insumo", updated.Mensaje);
        Assert.Null(provider.Correo);
        Assert.Equal("3001234567", provider.Telefono);
        repo.Setup(x => x.ObtenerNombresInsumosAsociadosAsync(3)).ReturnsAsync(new List<string> { "Harina" });
        Assert.True((await service.ChequearEliminarProveedorAsync(7, 3)).Datos!.TieneInsumos);
        var sequence = new MockSequence();
        repo.InSequence(sequence).Setup(x => x.EliminarAsociacionesDeProveedorAsync(3)).Returns(Task.CompletedTask);
        repo.InSequence(sequence).Setup(x => x.Eliminar(provider));
        Assert.True((await service.EliminarProveedorAsync(7, 3)).Exito);
        repo.Verify(x => x.EliminarAsociacionesDeProveedorAsync(3), Times.Once);
        repo.Verify(x => x.Eliminar(provider), Times.Once);
        repo.Verify(x => x.GuardarCambiosAsync(), Times.Once);
    }

    [Theory]
    [InlineData("sin-negocio")][InlineData("sin-insumo")][InlineData("sin-proveedor")][InlineData("duplicado")][InlineData("costo")]
    public async Task Asociacion_invalida_no_escribe(string failure)
    {
        if (failure == "sin-negocio") repo.Setup(x => x.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync((int?)null);
        if (failure == "sin-insumo") repo.Setup(x => x.ExisteInsumoEnNegocioAsync(2, 10)).ReturnsAsync(false);
        if (failure == "sin-proveedor") repo.Setup(x => x.ObtenerPorIdDelNegocioAsync(2, 3)).ReturnsAsync((Proveedor?)null);
        if (failure == "duplicado") repo.Setup(x => x.ExisteAsociacionAsync(10, 3)).ReturnsAsync(true);
        var result = await service.AsociarAsync(7, 10, new() { ProveedorId = 3, Costo = failure == "costo" ? 1000000 : 100, Cantidad = 1000, UnidadMedida = "g" });
        Assert.False(result.Exito);
        repo.Verify(x => x.AsociarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Duplicados_y_costos_fuera_del_limite_se_rechazan()
    {
        repo.Setup(x => x.ExisteNombreAsync(2, "Molino", null)).ReturnsAsync(true);
        repo.Setup(x => x.ExisteNombreAsync(2, "Molino", 3)).ReturnsAsync(true);
        Assert.False((await service.CrearProveedorAsync(7, new() { Nombre = "Molino" })).Exito);
        Assert.False((await service.CrearYAsociarAsync(7, 10, new() { Nombre = "Molino" })).Exito);
        Assert.False((await service.ActualizarProveedorAsync(7, 3, new() { Nombre = "Molino" })).Exito);
        Assert.False((await service.CrearYAsociarAsync(7, 10, new() { Nombre = "Otro", Costo = 1000000 })).Exito);
        Assert.False((await service.ActualizarCostoAsync(7, 10, 3, 1000000)).Exito);
        repo.Verify(x => x.CrearAsync(It.IsAny<Proveedor>()), Times.Never);
    }

    [Fact]
    public async Task Cuenta_sin_negocio_no_consulta_ni_modifica_proveedores()
    {
        var results = new[] { await service.ListarProveedoresDelNegocioAsync(99), await service.BuscarProveedoresAsync(99, "a", 5),
            await service.CrearProveedorAsync(99, new()), await service.ListarPorInsumoAsync(99, 10), await service.ListarDisponiblesParaInsumoAsync(99, 10),
            await service.CrearYAsociarAsync(99, 10, new()), await service.DesasociarAsync(99, 10, 3), await service.MarcarPrincipalAsync(99, 10, 3),
            await service.ActualizarCostoAsync(99, 10, 3, 100), await service.ActualizarProveedorAsync(99, 3, new()), await service.EliminarProveedorAsync(99, 3) };
        Assert.All(results, r => { Assert.False(r.Exito); Assert.True(r.NoEncontrado); });
        Assert.True((await service.ChequearEliminarProveedorAsync(99, 3)).NoEncontrado);
        repo.Verify(x => x.ListarPorNegocioAsync(It.IsAny<int>()), Times.Never);
    }
}
