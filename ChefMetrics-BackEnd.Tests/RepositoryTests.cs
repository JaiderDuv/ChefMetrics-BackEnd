using ChefMetrics_BackEnd.Model;
using ChefMetrics_BackEnd.Repositories;
using ChefMetrics_BackEnd.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChefMetrics_BackEnd.Tests;

[Trait("Tipo", "ComponenteEnMemoria")]
public sealed class RepositoryTests
{
    [Theory]
    [InlineData("nombre", false)]
    [InlineData("nombre", true)]
    [InlineData("costo", false)]
    [InlineData("costo", true)]
    [InlineData("cantidad", false)]
    [InlineData("cantidad", true)]
    public async Task Insumos_filtran_por_negocio_categoria_y_estado(string order, bool descending)
    {
        using var database = new TestDatabase();
        var db = database.Context;
        db.AddRange(new Insumo { Id = 11, NegocioId = 2, Nombre = "Azucar", Categoria = "materia_prima", UnidadMedida = "g", Cantidad = 200, CostoUnitario = 50, Activo = false },
            new Insumo { Id = 12, NegocioId = 99, Nombre = "Ajeno", Categoria = "materia_prima", UnidadMedida = "g", Activo = true });
        await db.SaveChangesAsync();
        var repository = new InsumoRepository(db);
        Assert.Equal(2, await repository.ObtenerNegocioIdPorCuentaAsync(7));
        Assert.Null(await repository.ObtenerNegocioIdPorCuentaAsync(999));
        var result = await repository.ListarAsync(2, null, "materia_prima", false, order, descending, 1, 50);
        Assert.Equal(1, result.Total);
        Assert.Equal(10, Assert.Single(result.Items).Id);
        Assert.Equal(2, (await repository.ListarAsync(2, null, null, true, order, descending, 1, 50)).Total);
        Assert.Null(await repository.ObtenerPorIdAsync(99, 10));
        Assert.True(await repository.ExisteNombreAsync(2, "HARINA"));
        Assert.False(await repository.ExisteNombreAsync(2, "HARINA", 10));
        Assert.Empty(await repository.ContarRecetasPorInsumosAsync(Array.Empty<int>()));
        Assert.Equal(0, await repository.ContarRecetasAsociadasAsync(10));
        Assert.Empty(await repository.ContarRecetasPorInsumosAsync(new[] { 10 }));
    }

    [Fact]
    public async Task Recetas_y_productos_persisten_relaciones_y_aislan_negocios()
    {
        using var database = new TestDatabase();
        var db = database.Context;
        var recipes = new RecetaRepository(db);
        var recipe = await recipes.CrearAsync(new Receta { Nombre = "Pan", NegocioId = 2, PorcionesBase = 2,
            RecetaInsumos = { new RecetaInsumo { InsumoId = 10, UnidadMedida = "g", Cantidad = 10 } } });
        Assert.Single(await recipes.ListarAsync(2, "pan"));
        Assert.Empty(await recipes.ListarAsync(99, null));
        Assert.True(await recipes.ExisteNombreAsync(2, "PAN"));
        Assert.False(await recipes.ExisteNombreAsync(2, "PAN", recipe.Id));
        Assert.NotNull(await recipes.ObtenerPorIdAsync(2, recipe.Id));
        Assert.Null(await recipes.ObtenerPorIdAsync(99, recipe.Id));
        Assert.Single(await recipes.ObtenerInsumosActivosAsync(2, new[] { 10 }));
        var trackedRecipe = await recipes.ObtenerParaEditarAsync(2, recipe.Id);
        recipes.ReemplazarInsumos(trackedRecipe!, new[] { new RecetaInsumo { InsumoId = 10, Cantidad = 20, UnidadMedida = "g" } });
        await recipes.GuardarCambiosAsync();
        Assert.Equal(20, (await recipes.ObtenerPorIdAsync(2, recipe.Id))!.RecetaInsumos.Single().Cantidad);
        var products = new ProductoMenuRepository(db);
        var product = await products.CrearAsync(new ProductoMenu { Nombre = "Combo", NegocioId = 2, Activo = true,
            ProductoMenuReceta = { new ProductoMenuRecetum { RecetaId = recipe.Id, Cantidad = 1 } } });
        Assert.Equal(1, await recipes.ContarProductosMenuAsync(recipe.Id));
        Assert.Single(await products.ListarAsync(2, "COM", false));
        Assert.Empty(await products.ListarAsync(99, null, true));
        Assert.True(await products.ExisteNombreAsync(2, "combo"));
        Assert.False(await products.ExisteNombreAsync(2, "combo", product.Id));
        Assert.NotNull(await products.ObtenerPorIdAsync(2, product.Id));
        Assert.Single(await products.ObtenerRecetasConInsumosAsync(2, new[] { recipe.Id }));
        Assert.Empty(await products.ObtenerRecetasConInsumosAsync(2, Array.Empty<int>()));
        Assert.Single(await products.ObtenerInsumosActivosAsync(2, new[] { 10 }));
        var trackedProduct = await products.ObtenerParaEditarAsync(2, product.Id);
        products.ReemplazarRecetas(trackedProduct!, new[] { new ProductoMenuRecetum { RecetaId = recipe.Id, Cantidad = 2 } });
        products.ReemplazarInsumos(trackedProduct!, new[] { new ProductoMenuInsumo { InsumoId = 10, Cantidad = 1, UnidadMedida = "g" } });
        await products.GuardarCambiosAsync();
        Assert.Equal(2, (await products.ObtenerPorIdAsync(2, product.Id))!.ProductoMenuReceta.Single().Cantidad);
        products.Eliminar(trackedProduct!);
        await products.GuardarCambiosAsync();
        Assert.Null(await products.ObtenerPorIdAsync(2, product.Id));
        recipes.Eliminar(trackedRecipe!);
        await recipes.GuardarCambiosAsync();
        Assert.Null(await recipes.ObtenerPorIdAsync(2, recipe.Id));
    }

    [Fact]
    public async Task Configuracion_costos_fijos_se_guarda_y_consulta_por_negocio()
    {
        using var database = new TestDatabase();
        var repository = new CosteoRepository(database.Context);
        var item = await repository.CrearCostoFijoAsync(new CostoFijo { NegocioId = 2, Nombre = "Arriendo", Categoria = "Local", MontoMensual = 1000, Activo = true });
        Assert.Single(await repository.ListarCostosFijosAsync(2, true));
        Assert.Empty(await repository.ListarCostosFijosAsync(99, false));
        Assert.True(await repository.ExisteNombreCostoFijoAsync(2, "ARRIENDO"));
        Assert.False(await repository.ExisteNombreCostoFijoAsync(2, "ARRIENDO", item.Id));
        Assert.NotNull(await repository.ObtenerCostoFijoAsync(2, item.Id, false));
        Assert.NotNull(await repository.ObtenerCostoFijoAsync(2, item.Id, true));
        Assert.Null(await repository.ObtenerCostoFijoAsync(99, item.Id, false));
        Assert.True(await repository.ActualizarPreparacionesBaseMesAsync(2, 200));
        Assert.False(await repository.ActualizarPreparacionesBaseMesAsync(99, 200));
        Assert.Equal(200, await repository.ObtenerPreparacionesBaseMesAsync(2));
        var config = await repository.ObtenerConfiguracionCostosAsync(2);
        Assert.Single(config.CostosFijos);
        Assert.Equal(200, config.PreparacionesBaseMes);
        Assert.Empty((await repository.ObtenerConfiguracionCostosAsync(99)).CostosFijos);
        Assert.Null(await repository.ObtenerRecetaConInsumosAsync(2, 99));
        Assert.Empty(await repository.ObtenerCostosProveedorPrincipalAsync(Array.Empty<int>()));
        Assert.Empty(await repository.ObtenerCostosProveedorPrincipalAsync(new[] { 10 }));
        Assert.Equal(20m, await new UmbralMargenRepository(database.Context).ObtenerAsync(2));
        repository.EliminarCostoFijo(item);
        await repository.GuardarCambiosAsync();
        Assert.Empty(await repository.ListarCostosFijosAsync(2, true));
    }

    [Fact]
    public async Task Cuentas_y_perfiles_buscan_por_identidad_y_guardan_permisos()
    {
        using var database = new TestDatabase();
        var repository = new UserRepository(database.Context);
        Assert.True(await repository.ExisteCorreoAsync("ANA@example.com"));
        Assert.NotNull(await repository.ObtenerCuentaPorEmailAsync("ANA@example.com"));
        Assert.Null(await repository.ObtenerCuentaPorIdAsync(99));
        Assert.NotNull(await repository.ObtenerCuentaPorIdAsync(7));
        var refresh = await repository.RegistrarRefreshTokenAsync(new RefreshToken { CuentaId = 7, TokenHash = "hash", FechaExpiracion = DateTime.UtcNow.AddDays(1) });
        Assert.Equal(refresh.Id, (await repository.ObtenerRefreshTokenPorHashAsync("hash"))!.Id);
        var profile = new PerfilEmprendimientoRepository(database.Context);
        Assert.NotNull(await profile.ObtenerPerfilCompletoAsync(7));
        Assert.False(await profile.ExisteCorreoAsync(database.Account.Correo, 7));
        Assert.True(await profile.ExisteCorreoAsync(database.Account.Correo, 99));
        Assert.False(await profile.ExisteNitAsync("123", 7));
        Assert.True(await profile.ExisteDocumentoAsync("1234567", 99));
        var grant = await profile.RegistrarPermisoEdicionAsync(new PerfilEditGrant { CuentaId = 7, TokenHash = "edit", FechaExpiracion = DateTime.UtcNow.AddMinutes(15) });
        Assert.Equal(grant.Id, (await profile.ObtenerPermisoEdicionAsync("edit"))!.Id);
        database.Context.Roles.Add(new Role { Nombre = "Chef" });
        await profile.GuardarCambiosUsuarioAsync();
        var role = Assert.Single(await profile.ObtenerRolesAsync());
        Assert.True(await profile.ExisteRolAsync(role.Id));
        Assert.False(await profile.ExisteEmpleadoCorreoAsync(2, "missing@example.com", 99));
        var health = await new DbHealthCheck(database.Context).CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, health.Status);
    }
}
