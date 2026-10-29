using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <summary>
    /// Las migraciones anteriores se recortaron a mano asumiendo que varias
    /// tablas y columnas ya existían en la base de desarrollo. En una base
    /// nueva faltaban Proveedor, InsumoProveedor, HistorialPrecioInsumo y
    /// varias columnas del modelo. Esta migración las crea solo si faltan,
    /// por lo que también es segura en las bases ya existentes.
    /// </summary>
    public partial class RepararEsquemaFaltante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'CostoFijo', N'Activo') IS NULL
                    ALTER TABLE [CostoFijo] ADD [Activo] bit NOT NULL
                        CONSTRAINT [DF_CostoFijo_Activo] DEFAULT (1);

                IF COL_LENGTH(N'Cuenta', N'NombreCompleto') IS NULL
                    ALTER TABLE [Cuenta] ADD [NombreCompleto] nvarchar(150) NULL;
                IF COL_LENGTH(N'Cuenta', N'Telefono') IS NULL
                    ALTER TABLE [Cuenta] ADD [Telefono] varchar(20) NULL;

                IF COL_LENGTH(N'Insumo', N'Categoria') IS NULL
                    ALTER TABLE [Insumo] ADD [Categoria] varchar(20) NOT NULL
                        CONSTRAINT [DF_Insumo_Categoria] DEFAULT ('otro');
                IF COL_LENGTH(N'Insumo', N'Cantidad') IS NULL
                    ALTER TABLE [Insumo] ADD [Cantidad] decimal(38,2) NULL;

                IF COL_LENGTH(N'Receta', N'ImagenUrl') IS NULL
                    ALTER TABLE [Receta] ADD [ImagenUrl] nvarchar(max) NULL;

                IF COL_LENGTH(N'ProductoMenu', N'ImagenUrl') IS NULL
                    ALTER TABLE [ProductoMenu] ADD [ImagenUrl] nvarchar(max) NULL;

                IF NOT EXISTS (
                    SELECT 1
                      FROM sys.default_constraints AS d
                      INNER JOIN sys.columns AS c
                        ON c.object_id = d.parent_object_id
                       AND c.column_id = d.parent_column_id
                     WHERE d.parent_object_id = OBJECT_ID(N'[PasswordResetTokens]')
                       AND c.name = N'Usado')
                    ALTER TABLE [PasswordResetTokens]
                        ADD CONSTRAINT [DF_PasswordResetTokens_Usado] DEFAULT (0) FOR [Usado];

                IF NOT EXISTS (
                    SELECT 1
                      FROM sys.default_constraints AS d
                      INNER JOIN sys.columns AS c
                        ON c.object_id = d.parent_object_id
                       AND c.column_id = d.parent_column_id
                     WHERE d.parent_object_id = OBJECT_ID(N'[PerfilEditGrants]')
                       AND c.name = N'Usado')
                    ALTER TABLE [PerfilEditGrants]
                        ADD CONSTRAINT [DF_PerfilEditGrants_Usado] DEFAULT (0) FOR [Usado];

                IF NOT EXISTS (
                    SELECT 1
                      FROM sys.default_constraints AS d
                      INNER JOIN sys.columns AS c
                        ON c.object_id = d.parent_object_id
                       AND c.column_id = d.parent_column_id
                     WHERE d.parent_object_id = OBJECT_ID(N'[RefreshTokens]')
                       AND c.name = N'Revocado')
                    ALTER TABLE [RefreshTokens]
                        ADD CONSTRAINT [DF_RefreshTokens_Revocado] DEFAULT (0) FOR [Revocado];

                IF NOT EXISTS (
                    SELECT 1
                      FROM sys.default_constraints AS d
                      INNER JOIN sys.columns AS c
                        ON c.object_id = d.parent_object_id
                       AND c.column_id = d.parent_column_id
                     WHERE d.parent_object_id = OBJECT_ID(N'[UsuarioEmprendedor]')
                       AND c.name = N'OnboardingCompletado')
                    ALTER TABLE [UsuarioEmprendedor]
                        ADD CONSTRAINT [DF_UsuarioEmprendedor_OnboardingCompletado] DEFAULT (0) FOR [OnboardingCompletado];

                IF EXISTS (
                    SELECT 1
                      FROM sys.columns AS c
                      INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
                     WHERE c.object_id = OBJECT_ID(N'[PasswordResetTokens]')
                       AND c.name = N'Tipo'
                       AND t.name <> N'nvarchar')
                    ALTER TABLE [PasswordResetTokens] ALTER COLUMN [Tipo] nvarchar(50) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Proveedor]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Proveedor] (
                        [Id] int NOT NULL IDENTITY(1, 1),
                        [NegocioId] int NOT NULL,
                        [Nombre] nvarchar(150) NOT NULL,
                        [Telefono] varchar(20) NULL,
                        [Correo] nvarchar(255) NULL,
                        [FechaCreacion] datetime2 NOT NULL
                            CONSTRAINT [DF_Proveedor_FechaCreacion] DEFAULT (sysutcdatetime()),
                        CONSTRAINT [PK_Proveedor] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Proveedor_Negocio] FOREIGN KEY ([NegocioId])
                            REFERENCES [Negocio] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_Proveedor_NegocioId]
                        ON [Proveedor] ([NegocioId]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[InsumoProveedor]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [InsumoProveedor] (
                        [Id] int NOT NULL IDENTITY(1, 1),
                        [InsumoId] int NOT NULL,
                        [ProveedorId] int NOT NULL,
                        [Costo] decimal(12,2) NOT NULL,
                        [Cantidad] decimal(18,2) NOT NULL
                            CONSTRAINT [DF_InsumoProveedor_Cantidad] DEFAULT (0),
                        [UnidadMedida] nvarchar(max) NOT NULL
                            CONSTRAINT [DF_InsumoProveedor_UnidadMedida] DEFAULT (N''),
                        [EsPrincipal] bit NOT NULL
                            CONSTRAINT [DF_InsumoProveedor_EsPrincipal] DEFAULT (0),
                        [FechaActualizacion] datetime2 NOT NULL
                            CONSTRAINT [DF_InsumoProveedor_FechaActualizacion] DEFAULT (sysutcdatetime()),
                        CONSTRAINT [PK_InsumoProveedor] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_InsumoProveedor_Insumo] FOREIGN KEY ([InsumoId])
                            REFERENCES [Insumo] ([Id]),
                        CONSTRAINT [FK_InsumoProveedor_Proveedor] FOREIGN KEY ([ProveedorId])
                            REFERENCES [Proveedor] ([Id])
                    );

                    CREATE INDEX [IX_InsumoProveedor_ProveedorId]
                        ON [InsumoProveedor] ([ProveedorId]);
                    CREATE UNIQUE INDEX [UQ_InsumoProveedor]
                        ON [InsumoProveedor] ([InsumoId], [ProveedorId]);
                    CREATE UNIQUE INDEX [UQ_InsumoProveedor_Principal]
                        ON [InsumoProveedor] ([InsumoId])
                        WHERE [EsPrincipal] = 1;
                END
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[HistorialPrecioInsumo]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [HistorialPrecioInsumo] (
                        [Id] int NOT NULL IDENTITY(1, 1),
                        [InsumoProveedorId] int NULL,
                        [NegocioId] int NULL,
                        [InsumoIdOriginal] int NULL,
                        [NombreInsumo] nvarchar(150) NULL,
                        [ProveedorNombre] nvarchar(150) NULL,
                        [UnidadMedida] varchar(10) NULL,
                        [Cantidad] decimal(38,2) NULL,
                        [CostoAnterior] decimal(12,2) NOT NULL,
                        [CostoNuevo] decimal(12,2) NOT NULL,
                        [FechaCambio] datetime2 NOT NULL
                            CONSTRAINT [DF_HistorialPrecioInsumo_FechaCambio] DEFAULT (sysutcdatetime()),
                        [FechaEliminacion] datetime2 NULL,
                        CONSTRAINT [PK_HistorialPrecioInsumo] PRIMARY KEY ([Id])
                    );

                END

                IF OBJECT_ID(N'[HistorialPrecioInsumo]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1 FROM sys.indexes
                        WHERE object_id = OBJECT_ID(N'[HistorialPrecioInsumo]')
                          AND name = N'IX_HistorialPrecioInsumo_InsumoProveedorId')
                    CREATE INDEX [IX_HistorialPrecioInsumo_InsumoProveedorId]
                        ON [HistorialPrecioInsumo] ([InsumoProveedorId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se elimina nada: estos objetos pueden haber existido antes
            // de esta migración en las bases de datos ya en uso.
        }
    }
}
