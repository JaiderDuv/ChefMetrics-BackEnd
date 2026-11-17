using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class ProveedorRepositoryRelationalTests
{
    private static async Task<Proveedor> AddProvider(TestDatabase database, string name)
    {
        var supplier = new Proveedor { NegocioId = database.Business.Id, Nombre = name };
        return await new ProveedorRepository(database.Context).CrearAsync(supplier);
    }

    [Fact]
    public async Task Asociar_y_cambiar_precio_conserva_el_historial_y_sincroniza_el_principal()
    {
        using var database = new TestDatabase(relational: true);
        var other = await AddProvider(database, "Alternativo");
        var main = await AddProvider(database, "Principal");
        var repo = new ProveedorRepository(database.Context);
        var id = database.Ingredient.Id;
        var first = await repo.AsociarAsync(id, main.Id, 100m, 2m, "kg");
        var second = await repo.AsociarAsync(id, other.Id, 200m, 10m, "kg");

        Assert.True(first.EsPrincipal);
        Assert.False(second.EsPrincipal);
        Assert.True(await repo.ActualizarCostoAsync(id, main.Id, 110m, 3m, " KG "));
        Assert.True(await repo.ActualizarCostoAsync(id, main.Id, 110m));
        Assert.True(await repo.ActualizarCostoAsync(id, other.Id, 220m));
        Assert.False(await repo.ActualizarCostoAsync(id, 999, 220m));

        var prices = await database.Context.HistorialPrecioInsumos
            .Where(h => h.InsumoProveedorId == first.Id).OrderBy(h => h.Id).ToListAsync();
        Assert.Collection(prices,
            entry => Assert.Equal(100m, entry.CostoNuevo),
            entry =>
            {
                Assert.Equal(100m, entry.CostoAnterior);
                Assert.Equal(110m, entry.CostoNuevo);
                Assert.Equal("Harina", entry.NombreInsumo);
                Assert.Equal("Principal", entry.ProveedorNombre);
                Assert.Equal(database.Business.Id, entry.NegocioId);
            });

        await repo.SincronizarInsumoConPrincipalAsync(id);
        await database.Context.Entry(database.Ingredient).ReloadAsync();
        Assert.Equal(3000m, database.Ingredient.Cantidad);
        Assert.Equal("g", database.Ingredient.UnidadMedida);
        Assert.Equal(110m, database.Ingredient.CostoUnitario);
        Assert.Equal("Principal", database.Ingredient.Proveedor);
        Assert.Equal(1, await repo.ContarAsociacionesAsync(main.Id));
        Assert.Equal(new[] { "Harina" }, await repo.ObtenerNombresInsumosAsociadosAsync(main.Id));
    }

    [Fact]
    public async Task Desasociar_principal_promueve_el_siguiente_y_deja_su_cantidad()
    {
        using var database = new TestDatabase(relational: true);
        var main = await AddProvider(database, "Primero");
        var next = await AddProvider(database, "Segundo");
        var last = await AddProvider(database, "Tercero");
        var repo = new ProveedorRepository(database.Context);
        var id = database.Ingredient.Id;
        await repo.AsociarAsync(id, main.Id, 100, 2, "g");
        var nextLink = await repo.AsociarAsync(id, next.Id, 200, 9, "g");
        await repo.AsociarAsync(id, last.Id, 220, 12, "g");
        nextLink.FechaActualizacion = DateTime.UtcNow.AddDays(-1);
        await database.Context.SaveChangesAsync();

        Assert.False(await repo.DesasociarAsync(id, 999));
        Assert.True(await repo.DesasociarAsync(id, main.Id));
        database.Context.ChangeTracker.Clear();
        Assert.True((await repo.ObtenerAsociacionAsync(id, next.Id))!.EsPrincipal);
        Assert.Equal(9m, await repo.ObtenerCantidadInsumoAsync(id));
        Assert.True(await repo.MarcarPrincipalAsync(id, last.Id));
        Assert.True(await repo.MarcarPrincipalAsync(id, last.Id));
        Assert.False(await repo.MarcarPrincipalAsync(id, 999));
        Assert.False((await repo.ObtenerAsociacionAsync(id, next.Id))!.EsPrincipal);
    }

    [Theory]
    [InlineData("g", true)]
    [InlineData("ml", false)]
    public async Task Sin_principal_elige_paquete_comparable_o_conserva_el_costo_base(string unit, bool comparable)
    {
        using var database = new TestDatabase(relational: true);
        var expensive = await AddProvider(database, "Primero");
        var economical = await AddProvider(database, "Económico");
        var repo = new ProveedorRepository(database.Context);
        var id = database.Ingredient.Id;
        var first = await repo.AsociarAsync(id, expensive.Id, 200, 10, unit);
        await repo.AsociarAsync(id, economical.Id, 120, 10, unit);
        first.EsPrincipal = false;
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await repo.SincronizarInsumoConPrincipalAsync(id);
        var ingredient = await database.Context.Insumos.FindAsync(id);
        if (comparable) Assert.Equal(economical.Nombre, ingredient!.Proveedor);
        else Assert.Contains(ingredient!.Proveedor, new[] { expensive.Nombre, economical.Nombre });
        Assert.Equal(comparable ? 120m : 100m, ingredient.CostoUnitario);
        Assert.Equal(10m, ingredient.Cantidad);
        await repo.SincronizarInsumoConPrincipalAsync(999);
    }

    [Fact]
    public async Task Eliminar_asociaciones_limpia_solo_el_historial_del_proveedor()
    {
        using var database = new TestDatabase(relational: true);
        var main = await AddProvider(database, "Principal");
        var other = await AddProvider(database, "Alternativo");
        var repo = new ProveedorRepository(database.Context);
        var id = database.Ingredient.Id;
        var first = await repo.AsociarAsync(id, main.Id, 100, 2, "g");
        var second = await repo.AsociarAsync(id, other.Id, 200, 9, "g");
        await repo.SincronizarInsumoConPrincipalAsync(id);
        Assert.Equal(1, await repo.PropagarNombreAInsumosAsync(database.Business.Id, main.Id, " Nombre nuevo "));
        database.Context.ChangeTracker.Clear();
        Assert.Equal("Nombre nuevo", (await database.Context.Insumos.FindAsync(id))!.Proveedor);

        await repo.EliminarAsociacionesDeProveedorAsync(main.Id);
        await repo.EliminarAsociacionesDeProveedorAsync(main.Id);
        database.Context.ChangeTracker.Clear();
        Assert.False(await repo.ExisteAsociacionAsync(id, main.Id));
        Assert.False(await database.Context.HistorialPrecioInsumos.AnyAsync(h => h.InsumoProveedorId == first.Id));
        Assert.True(await database.Context.HistorialPrecioInsumos.AnyAsync(h => h.InsumoProveedorId == second.Id));
        Assert.Null((await database.Context.Insumos.FindAsync(id))!.Proveedor);
    }
}
