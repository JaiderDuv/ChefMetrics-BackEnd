using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <summary>
    /// Defensa en profundidad para datos que también pueden llegar por SQL
    /// directo (scripts, importaciones o una futura integración): completa la
    /// relación de historial, agrega índices de consulta, restringe valores
    /// inválidos y garantiza que las tablas puente no mezclen negocios.
    /// </summary>
    public partial class EndurecerIntegridadTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El modelo declara esta relación, pero la base no la tenía.
            // SET NULL conserva el historial cuando se desasocia un proveedor.
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_HistorialPrecioInsumo_InsumoProveedor'
      AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    ALTER TABLE [dbo].[HistorialPrecioInsumo] WITH CHECK
        ADD CONSTRAINT [FK_HistorialPrecioInsumo_InsumoProveedor]
        FOREIGN KEY ([InsumoProveedorId])
        REFERENCES [dbo].[InsumoProveedor] ([Id])
        ON DELETE SET NULL;
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_InsumoProveedorId'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistorialPrecioInsumo_InsumoProveedorId]
        ON [dbo].[HistorialPrecioInsumo] ([InsumoProveedorId])
        INCLUDE ([NegocioId], [InsumoIdOriginal], [CostoAnterior], [CostoNuevo], [FechaCambio]);
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_NegocioId_FechaCambio'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistorialPrecioInsumo_NegocioId_FechaCambio]
        ON [dbo].[HistorialPrecioInsumo] ([NegocioId], [FechaCambio] DESC)
        INCLUDE ([InsumoIdOriginal], [NombreInsumo], [ProveedorNombre], [CostoAnterior], [CostoNuevo]);
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_InsumoIdOriginal_FechaCambio'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistorialPrecioInsumo_InsumoIdOriginal_FechaCambio]
        ON [dbo].[HistorialPrecioInsumo] ([InsumoIdOriginal], [FechaCambio] DESC);
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProductoMenu_NegocioId_Activo_Nombre'
      AND object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductoMenu_NegocioId_Activo_Nombre]
        ON [dbo].[ProductoMenu] ([NegocioId], [Activo], [Nombre])
        INCLUDE ([PrecioVenta], [ImagenUrl], [FechaCreacion]);
END;");

            // El precio 0 no es válido para un producto del menú (la API ya
            // exige > 0). Se conserva la regla anterior en Down().
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenu_Precio'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenu] DROP CONSTRAINT [CK_ProductoMenu_Precio];
END;

ALTER TABLE [dbo].[ProductoMenu] WITH CHECK
    ADD CONSTRAINT [CK_ProductoMenu_Precio]
    CHECK ([PrecioVenta] IS NULL OR ([PrecioVenta] > 0 AND [PrecioVenta] <= 999999.99));");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenu_Nombre'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenu] WITH CHECK
        ADD CONSTRAINT [CK_ProductoMenu_Nombre]
        CHECK ([Nombre] = LTRIM(RTRIM([Nombre])) AND LEN([Nombre]) >= 3 AND LEN([Nombre]) <= 75);
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenuInsumo_Cantidad'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenuInsumo]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenuInsumo] WITH CHECK
        ADD CONSTRAINT [CK_ProductoMenuInsumo_Cantidad]
        CHECK ([Cantidad] > 0 AND [Cantidad] <= 100000);
END;");

            // Los triggers de validación de negocio se retiraron porque SQL Server
            // no permite OUTPUT INSERTED.* en tablas con triggers habilitados,
            // lo que rompía las altas de EF Core. La validación de negocio se
            // hace en los servicios; la migración QuitarTriggersIncompatibles
            // elimina los triggers en las bases que ya los tenían.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_InsumoProveedor_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_RecetaInsumo_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuInsumo_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuReceta_ValidarNegocio];");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenuInsumo_Cantidad'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenuInsumo]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenuInsumo] DROP CONSTRAINT [CK_ProductoMenuInsumo_Cantidad];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenu_Nombre'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenu] DROP CONSTRAINT [CK_ProductoMenu_Nombre];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_ProductoMenu_Precio'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    ALTER TABLE [dbo].[ProductoMenu] DROP CONSTRAINT [CK_ProductoMenu_Precio];
END;

ALTER TABLE [dbo].[ProductoMenu] WITH CHECK
    ADD CONSTRAINT [CK_ProductoMenu_Precio]
    CHECK ([PrecioVenta] IS NULL OR [PrecioVenta] >= 0);");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProductoMenu_NegocioId_Activo_Nombre'
      AND object_id = OBJECT_ID(N'[dbo].[ProductoMenu]'))
BEGIN
    DROP INDEX [IX_ProductoMenu_NegocioId_Activo_Nombre] ON [dbo].[ProductoMenu];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_InsumoIdOriginal_FechaCambio'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    DROP INDEX [IX_HistorialPrecioInsumo_InsumoIdOriginal_FechaCambio] ON [dbo].[HistorialPrecioInsumo];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_NegocioId_FechaCambio'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    DROP INDEX [IX_HistorialPrecioInsumo_NegocioId_FechaCambio] ON [dbo].[HistorialPrecioInsumo];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_HistorialPrecioInsumo_InsumoProveedorId'
      AND object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    DROP INDEX [IX_HistorialPrecioInsumo_InsumoProveedorId] ON [dbo].[HistorialPrecioInsumo];
END;");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_HistorialPrecioInsumo_InsumoProveedor'
      AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialPrecioInsumo]'))
BEGIN
    ALTER TABLE [dbo].[HistorialPrecioInsumo] DROP CONSTRAINT [FK_HistorialPrecioInsumo_InsumoProveedor];
END;");
        }
    }
}
