using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class InsumoServiceTests
{
    private readonly Mock<IInsumoRepository> repository = new();
    private readonly InsumoService service;
    private readonly Insumo insumo = new() { Id = 10, NegocioId = 2, Nombre = "Harina", Categoria = "materia_prima", UnidadMedida = "g", Cantidad = 100, CostoUnitario = 100, Activo = true };
    private readonly Proveedor proveedor = new() { Id = 3, Nombre = "Molino", NegocioId = 2 };

    public InsumoServiceTests()
    {
        service = new InsumoService(repository.Object, NullLogger<InsumoService>.Instance);
        repository.Setup(r => r.ObtenerNegocioIdPorCuentaAsync(7)).ReturnsAsync(2);
        repository.Setup(r => r.ObtenerPorIdAsync(2, 10)).ReturnsAsync(insumo);
        repository.Setup(r => r.EjecutarEnTransaccionAsync(It.IsAny<Func<Task<Insumo>>>())).Returns((Func<Task<Insumo>> action) => action());
        repository.Setup(r => r.CrearAsync(It.IsAny<Insumo>())).ReturnsAsync((Insumo value) => { value.Id = 10; return value; });
        repository.Setup(r => r.ObtenerOCrearProveedorAsync(2, It.IsAny<string>())).ReturnsAsync(proveedor);
        repository.Setup(r => r.CrearInsumoProveedorAsync(It.IsAny<InsumoProveedor>())).ReturnsAsync((InsumoProveedor value) => { value.Id = 4; return value; });
        repository.Setup(r => r.ObtenerNombreProveedorPrincipalAsync(10)).ReturnsAsync("Molino");
    }

    private static CrearInsumoDto Nuevo() => new() { Nombre = " Harina ", Categoria = "materia_prima", UnidadMedida = "g", CostoUnitario = 100, Cantidad = 100, Proveedor = " Molino " };

    [Fact]
    public async Task Crear_normaliza_y_registra_historial_del_proveedor()
    {
        var result = await service.CrearAsync(7, Nuevo());
        Assert.True(result.Exito);
        Assert.Equal("Harina", result.Insumo!.Nombre);
        Assert.Equal("Molino", result.Insumo.Proveedor);
        repository.Verify(r => r.CrearAsync(It.Is<Insumo>(i => i.NegocioId == 2 && i.Activo)), Times.Once);
        repository.Verify(r => r.RegistrarCambioPrecioAsync(It.Is<HistorialPrecioInsumo>(h => h.CostoNuevo == 100 && h.InsumoIdOriginal == 10)), Times.Once);
        repository.Verify(r => r.RevincularHistorialEliminadoAsync(2, "Harina", 10, 4), Times.Once);
    }

    [Theory]
    [InlineData("nombre_vacio")]
    [InlineData("nombre_largo")]
    [InlineData("categoria")]
    [InlineData("unidad")]
    [InlineData("costo_cero")]
    [InlineData("costo_excesivo")]
    [InlineData("proveedor_vacio")]
    [InlineData("proveedor_largo")]
    [InlineData("cantidad_negativa")]
    [InlineData("cantidad_excesiva")]
    [InlineData("densidad_negativa")]
    [InlineData("duplicado")]
    public async Task Creacion_invalida_no_escribe(string caso)
    {
        var dto = Nuevo();
        switch (caso)
        {
            case "nombre_vacio": dto.Nombre = " "; break;
            case "nombre_largo": dto.Nombre = new string('a', 151); break;
            case "categoria": dto.Categoria = "invalid"; break;
            case "unidad": dto.UnidadMedida = "invalid"; break;
            case "costo_cero": dto.CostoUnitario = 0; break;
            case "costo_excesivo": dto.CostoUnitario = 1000000; break;
            case "proveedor_vacio": dto.Proveedor = " "; break;
            case "proveedor_largo": dto.Proveedor = new string('a', 151); break;
            case "cantidad_negativa": dto.Cantidad = -1; break;
            case "cantidad_excesiva": dto.Cantidad = decimal.MaxValue; break;
            case "densidad_negativa": dto.DensidadGPorMl = -1; break;
            case "duplicado": repository.Setup(r => r.ExisteNombreAsync(2, "Harina", null)).ReturnsAsync(true); break;
        }
        Assert.False((await service.CrearAsync(7, dto)).Exito);
        repository.Verify(r => r.CrearAsync(It.IsAny<Insumo>()), Times.Never);
    }

    [Fact]
    public async Task Operaciones_sin_negocio_devuelven_no_encontrado()
    {
        Assert.True((await service.CrearAsync(99, Nuevo())).NoEncontrado);
        Assert.Null(await service.ListarAsync(99, new FiltrosInsumoDto()));
        Assert.True((await service.ObtenerPorIdAsync(99, 10)).NoEncontrado);
        Assert.True((await service.ActualizarParcialAsync(99, 10, new ActualizarInsumoDto())).NoEncontrado);
        Assert.True((await service.EliminarAsync(99, 10)).NoEncontrado);
        Assert.True((await service.RestaurarAsync(99, 10)).NoEncontrado);
        Assert.True((await service.ObtenerHistorialPreciosAsync(99, 10)).NoEncontrado);
        Assert.True((await service.ObtenerHistorialGeneralAsync(99, null, null)).NoEncontrado);
        repository.Verify(r => r.GuardarCambiosAsync(), Times.Never);
    }

    [Theory]
    [InlineData(0, 0, 50)]
    [InlineData(1, 500, 100)]
    public async Task Listado_limita_paginacion_y_agrega_conteos(int page, int size, int expected)
    {
        repository.Setup(r => r.ListarAsync(2, null, null, false, "nombre", false, 1, expected)).ReturnsAsync((new List<Insumo> { insumo }, 1));
        repository.Setup(r => r.ContarRecetasPorInsumosAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, int> { [10] = 2 });
        repository.Setup(r => r.ObtenerNombresProveedoresPrincipalesAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, string> { [10] = "Molino" });
        var result = await service.ListarAsync(7, new FiltrosInsumoDto { Pagina = page, TamanoPagina = size, OrdenarPor = "invalid" });
        Assert.Equal(expected, result!.TamanoPagina);
        Assert.Equal(1, result.Total);
        Assert.Equal("Molino", Assert.Single(result.Datos).Proveedor);
        Assert.True((await service.ObtenerPorIdAsync(7, 10)).Exito);
        Assert.True((await service.ObtenerPorIdAsync(7, 999)).NoEncontrado);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cambio_de_precio_actualiza_proveedor_y_auditoria(bool existing)
    {
        var association = new InsumoProveedor { Id = 4, InsumoId = 10, ProveedorId = 3, Proveedor = proveedor, Costo = 100, Cantidad = 100, UnidadMedida = "g", EsPrincipal = true };
        repository.Setup(r => r.ObtenerInsumoProveedorAsync(10, 3)).ReturnsAsync(existing ? association : null);
        repository.Setup(r => r.ObtenerInsumoProveedorPrincipalAsync(10)).ReturnsAsync(association);
        var result = await service.ActualizarParcialAsync(7, 10, new ActualizarInsumoDto { Nombre = "Harina fina", Categoria = "otro", UnidadMedida = "kg", Cantidad = 2, DensidadGPorMl = 0.5m, CostoUnitario = 200, Proveedor = "Molino", Activo = false });
        Assert.True(result.Exito);
        Assert.Equal(200, result.Insumo!.CostoUnitario);
        Assert.False(result.Insumo.Activo);
        Assert.Equal("kg", association.UnidadMedida);
        Assert.Equal(2, association.Cantidad);
        repository.Verify(r => r.RegistrarCambioPrecioAsync(It.Is<HistorialPrecioInsumo>(h => h.CostoNuevo == 200)), Times.Once);
        repository.Verify(r => r.ActualizarAsync(insumo), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Molino")]
    public async Task Cambiar_solo_proveedor_conserva_precio(string name)
    {
        insumo.Proveedor = "Anterior";
        var result = await service.ActualizarParcialAsync(7, 10, new ActualizarInsumoDto { Proveedor = name });
        Assert.True(result.Exito);
        Assert.Equal(100, insumo.CostoUnitario);
        repository.Verify(r => r.DesmarcarPrincipalesAsync(10, It.IsAny<int>()), Times.Once);
    }

    [Theory]
    [InlineData("vacio")]
    [InlineData("nombre")]
    [InlineData("largo")]
    [InlineData("unidad")]
    [InlineData("categoria")]
    [InlineData("cantidad")]
    [InlineData("tope")]
    [InlineData("costo")]
    [InlineData("costo_tope")]
    [InlineData("precio_sin_proveedor")]
    [InlineData("densidad")]
    public async Task Actualizacion_invalida_no_guarda(string caso)
    {
        var dto = new ActualizarInsumoDto();
        switch (caso)
        {
            case "nombre": dto.Nombre = " "; break;
            case "largo": dto.Nombre = new string('a', 151); break;
            case "unidad": dto.UnidadMedida = "invalid"; break;
            case "categoria": dto.Categoria = "invalid"; break;
            case "cantidad": dto.Cantidad = -1; break;
            case "tope": dto.Cantidad = decimal.MaxValue; break;
            case "costo": dto.CostoUnitario = 0; break;
            case "costo_tope": dto.CostoUnitario = 1000000; break;
            case "precio_sin_proveedor": dto.CostoUnitario = 200; break;
            case "densidad": dto.DensidadGPorMl = -1; break;
        }
        Assert.False((await service.ActualizarParcialAsync(7, 10, dto)).Exito);
        repository.Verify(r => r.ActualizarAsync(It.IsAny<Insumo>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Eliminacion_archiva_solo_si_no_hay_recetas(int recipes)
    {
        repository.Setup(r => r.ContarRecetasAsociadasAsync(10)).ReturnsAsync(recipes);
        var result = await service.EliminarAsync(7, 10);
        Assert.Equal(recipes == 0, result.Exito);
        Assert.Equal(recipes > 0, result.BloqueadoPorRecetas);
        repository.Verify(r => r.ArchivarHistorialAsync(2, 10), recipes == 0 ? Times.Once : Times.Never);
        Assert.True((await service.EliminarAsync(7, 999)).NoEncontrado);
    }

    [Theory]
    [InlineData(false, 100)]
    [InlineData(true, 100)]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    public async Task Historial_calcula_variaciones_y_conserva_datos_archivados(bool archived, int quantity)
    {
        var association = new InsumoProveedor { Id = 4, InsumoId = 10, Insumo = insumo, ProveedorId = 3, Proveedor = proveedor };
        var history = new List<HistorialPrecioInsumo>
        {
            new() { Id = 2, InsumoProveedorId = archived ? null : 4, InsumoProveedor = association, NegocioId = 2, InsumoIdOriginal = 10, NombreInsumo = "Harina", ProveedorNombre = "Molino", CostoAnterior = 100, CostoNuevo = 150, Cantidad = quantity, UnidadMedida = "g", FechaCambio = DateTime.UtcNow },
            new() { Id = 1, InsumoProveedorId = archived ? null : 4, InsumoProveedor = association, NegocioId = 2, InsumoIdOriginal = 10, NombreInsumo = "Harina", ProveedorNombre = "Molino", CostoAnterior = 100, CostoNuevo = 100, Cantidad = quantity, UnidadMedida = "g", FechaCambio = DateTime.UtcNow.AddDays(-1) }
        };
        repository.Setup(r => r.ListarHistorialPorInsumoAsync(10)).ReturnsAsync(history);
        repository.Setup(r => r.ListarHistorialGeneralAsync(2, null, null)).ReturnsAsync(archived ? new() : history);
        repository.Setup(r => r.ListarHistorialArchivadoGeneralAsync(2, null, null)).ReturnsAsync(archived ? history : new());
        repository.Setup(r => r.ListarHistorialArchivadoPorInsumoAsync(2, 10)).ReturnsAsync(history);
        if (archived) repository.Setup(r => r.ObtenerPorIdAsync(2, 10)).ReturnsAsync((Insumo?)null);
        var detail = await service.ObtenerHistorialPreciosAsync(7, 10);
        Assert.True(detail.Exito);
        Assert.Equal(archived, detail.Historial!.InsumoEliminado);
        Assert.Equal(50m, detail.Historial.Precios.First().VariacionPorcentual);
        var general = await service.ObtenerHistorialGeneralAsync(7, null, null);
        Assert.True(general.Exito);
        Assert.Equal(2, general.Historial!.Precios.Count);
        Assert.True(general.Historial.Precios.First().EsSubida);
        if (archived) Assert.True((await service.RestaurarAsync(7, 10)).Exito);
    }

    [Fact]
    public async Task Errores_de_persistencia_se_convierten_en_mensaje_publico()
    {
        repository.Setup(r => r.CrearAsync(It.IsAny<Insumo>())).ThrowsAsync(new DbUpdateException("detalle interno"));
        var result = await service.CrearAsync(7, Nuevo());
        Assert.False(result.Exito);
        Assert.DoesNotContain("detalle interno", result.Mensaje);
        repository.Setup(r => r.ActualizarAsync(insumo)).ThrowsAsync(new DbUpdateException("detalle interno"));
        Assert.False((await service.ActualizarParcialAsync(7, 10, new ActualizarInsumoDto { Activo = false })).Exito);
    }
}
