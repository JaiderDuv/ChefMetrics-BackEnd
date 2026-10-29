using ChefMetrics.Contracts;
using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Services;
using Moq;

namespace ChefMetrics_BackEnd.Tests;

public sealed class ProveedorServiceSeguridadTests
{
    [Fact]
    public async Task No_permite_asociar_un_proveedor_de_otro_negocio()
    {
        const int cuentaId = 7;
        const int negocioId = 3;
        const int insumoId = 10;
        const int proveedorDeOtroNegocio = 99;

        var repositorio = new Mock<IProveedorRepository>(MockBehavior.Strict);
        repositorio
            .Setup(r => r.ObtenerNegocioIdPorCuentaAsync(cuentaId))
            .ReturnsAsync(negocioId);
        repositorio
            .Setup(r => r.ExisteInsumoEnNegocioAsync(negocioId, insumoId))
            .ReturnsAsync(true);
        repositorio
            .Setup(r => r.ObtenerPorIdDelNegocioAsync(negocioId, proveedorDeOtroNegocio))
            .ReturnsAsync((Proveedor?)null);

        var servicio = new ProveedorService(repositorio.Object);

        var resultado = await servicio.AsociarAsync(cuentaId, insumoId, new AsociarProveedorDto
        {
            ProveedorId = proveedorDeOtroNegocio,
            Costo = 1_000m,
            Cantidad = 1m,
            UnidadMedida = "unidad"
        });

        Assert.False(resultado.Exito);
        Assert.True(resultado.NoEncontrado);
        Assert.Contains("tu negocio", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        repositorio.Verify(
            r => r.ObtenerPorIdAsync(It.IsAny<int>()),
            Times.Never);
    }
}
