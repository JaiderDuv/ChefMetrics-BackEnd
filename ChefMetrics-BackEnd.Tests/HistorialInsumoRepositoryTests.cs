using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class HistorialInsumoRepositoryTests
{
    [Fact]
    public async Task Archivar_y_revincular_conserva_snapshots_y_respeta_el_negocio()
    {
        using var database = new TestDatabase(relational: true);
        var db = database.Context;
        var repo = new InsumoRepository(db);
        var supplier = new Proveedor { NegocioId = database.Business.Id, Nombre = "Molino Central" };
        var link = await repo.CrearInsumoProveedorAsync(new InsumoProveedor
        {
            Insumo = database.Ingredient, Proveedor = supplier, UnidadMedida = "kg", Costo = 100, Cantidad = 1
        });
        var price = await repo.RegistrarCambioPrecioAsync(new HistorialPrecioInsumo
        {
            InsumoProveedorId = link.Id, CostoAnterior = 80, CostoNuevo = 100
        });
        Assert.Equal("kg", price.UnidadMedida);
        Assert.Equal("Molino Central", price.ProveedorNombre);
        Assert.Equal("Harina", price.NombreInsumo);
        var businessId = database.Business.Id;
        var ingredientId = database.Ingredient.Id;

        Assert.NotEmpty(await repo.ListarHistorialPorInsumoAsync(ingredientId));
        Assert.Single(await repo.ListarHistorialGeneralAsync(businessId, " HAR ", " MOL "));
        Assert.Empty(await repo.ListarHistorialGeneralAsync(999, null, null));
        Assert.Single(await repo.ListarParaHistorialAsync(businessId, "HAR", "CENTRAL"));
        Assert.Empty(await repo.ListarParaHistorialAsync(businessId, null, "no coincide"));

        await repo.ArchivarHistorialAsync(businessId, ingredientId);
        db.ChangeTracker.Clear();
        Assert.False(await db.InsumoProveedors.AnyAsync(x => x.Id == link.Id));
        var archived = Assert.Single(await repo.ListarHistorialArchivadoPorInsumoAsync(businessId, ingredientId));
        Assert.Equal(price.Id, archived.Id);
        Assert.Null(archived.InsumoProveedorId);
        Assert.NotNull(archived.FechaEliminacion);
        Assert.Single(await repo.ListarHistorialArchivadoGeneralAsync(businessId, "HARINA", "MOLINO"));
        Assert.Empty(await repo.ListarHistorialArchivadoGeneralAsync(999, null, null));

        var replacement = await repo.CrearInsumoProveedorAsync(new InsumoProveedor
        {
            InsumoId = ingredientId, ProveedorId = supplier.Id, UnidadMedida = "kg", Costo = 110, Cantidad = 1
        });
        Assert.Equal(0, await repo.RevincularHistorialEliminadoAsync(999, "Harina", ingredientId, replacement.Id));
        Assert.Equal(1, await repo.RevincularHistorialEliminadoAsync(businessId, " HARINA ", ingredientId, replacement.Id));
        var restored = Assert.Single(await repo.ListarHistorialPorInsumoAsync(ingredientId));
        Assert.Equal(replacement.Id, restored.InsumoProveedorId);
        Assert.Null(restored.FechaEliminacion);
    }

    [Fact]
    public async Task Transaccion_revierte_el_insumo_si_falla_la_operacion()
    {
        using var database = new TestDatabase(relational: true);
        var repo = new InsumoRepository(database.Context);
        var ingredient = database.Ingredient;
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.EjecutarEnTransaccionAsync<int>(async () =>
        {
            ingredient.Nombre = "Cambio sin confirmar";
            await repo.ActualizarAsync(ingredient);
            throw new InvalidOperationException();
        }));
        await database.Context.Entry(ingredient).ReloadAsync();
        Assert.Equal("Harina", ingredient.Nombre);

        var savedId = await repo.EjecutarEnTransaccionAsync(async () =>
        {
            ingredient.Nombre = "Harina de trigo";
            await repo.ActualizarAsync(ingredient);
            return ingredient.Id;
        });
        await database.Context.Entry(ingredient).ReloadAsync();
        Assert.Equal(ingredient.Id, savedId);
        Assert.Equal("Harina de trigo", ingredient.Nombre);
    }
}
