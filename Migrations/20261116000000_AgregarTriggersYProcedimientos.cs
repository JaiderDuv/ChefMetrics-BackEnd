using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChefMetrics_BackEnd.Migrations;

[DbContext(typeof(ChefMetricsContext))]
[Migration("20261116000000_AgregarTriggersYProcedimientos")]
public sealed class AgregarTriggersYProcedimientos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Cada definición necesita su propio lote, también en scripts idempotentes.
        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER TRIGGER [dbo].[TR_InsumoProveedor_ValidarNegocio]
            ON [dbo].[InsumoProveedor]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS i
                    JOIN [dbo].[Insumo] AS a ON a.Id = i.InsumoId
                    JOIN [dbo].[Proveedor] AS b ON b.Id = i.ProveedorId
                    WHERE a.NegocioId <> b.NegocioId
                )
                    THROW 51010, N''Los registros de InsumoProveedor deben pertenecer al mismo negocio.'', 1;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER TRIGGER [dbo].[TR_RecetaInsumo_ValidarNegocio]
            ON [dbo].[RecetaInsumo]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS i
                    JOIN [dbo].[Receta] AS a ON a.Id = i.RecetaId
                    JOIN [dbo].[Insumo] AS b ON b.Id = i.InsumoId
                    WHERE a.NegocioId <> b.NegocioId
                )
                    THROW 51010, N''Los registros de RecetaInsumo deben pertenecer al mismo negocio.'', 1;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER TRIGGER [dbo].[TR_ProductoMenuInsumo_ValidarNegocio]
            ON [dbo].[ProductoMenuInsumo]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS i
                    JOIN [dbo].[ProductoMenu] AS a ON a.Id = i.ProductoMenuId
                    JOIN [dbo].[Insumo] AS b ON b.Id = i.InsumoId
                    WHERE a.NegocioId <> b.NegocioId
                )
                    THROW 51010, N''Los registros de ProductoMenuInsumo deben pertenecer al mismo negocio.'', 1;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER TRIGGER [dbo].[TR_ProductoMenuReceta_ValidarNegocio]
            ON [dbo].[ProductoMenuReceta]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS i
                    JOIN [dbo].[ProductoMenu] AS a ON a.Id = i.ProductoMenuId
                    JOIN [dbo].[Receta] AS b ON b.Id = i.RecetaId
                    WHERE a.NegocioId <> b.NegocioId
                )
                    THROW 51010, N''Los registros de ProductoMenuReceta deben pertenecer al mismo negocio.'', 1;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER TRIGGER [dbo].[TR_HistorialPrecioInsumo_ValidarNegocio]
            ON [dbo].[HistorialPrecioInsumo]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted AS h
                    JOIN [dbo].[InsumoProveedor] AS ip ON ip.Id = h.InsumoProveedorId
                    JOIN [dbo].[Insumo] AS i ON i.Id = ip.InsumoId
                    WHERE h.NegocioId <> i.NegocioId
                )
                    THROW 51010, N''El historial de precios debe pertenecer al negocio del insumo.'', 1;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER PROCEDURE [dbo].[ListarProveedoresPorNegocio]
                @NegocioId int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT p.*
                FROM [dbo].[Proveedor] AS p
                WHERE p.NegocioId = @NegocioId
                ORDER BY p.Nombre;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER PROCEDURE [dbo].[ListarCostosFijos]
                @NegocioId int, @SoloActivos bit
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT c.*
                FROM [dbo].[CostoFijo] AS c
                WHERE c.NegocioId = @NegocioId AND (@SoloActivos = 0 OR c.Activo = 1)
                ORDER BY c.Nombre;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER PROCEDURE [dbo].[ObtenerPreparacionesBaseMes]
                @NegocioId int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT n.PreparacionesBaseMes AS [Value]
                FROM [dbo].[Negocio] AS n
                WHERE n.Id = @NegocioId;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER PROCEDURE [dbo].[ListarHistorialArchivadoPorInsumo]
                @NegocioId int, @InsumoId int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT h.*
                FROM [dbo].[HistorialPrecioInsumo] AS h
                WHERE h.NegocioId = @NegocioId
                  AND h.InsumoIdOriginal = @InsumoId
                  AND h.FechaEliminacion IS NOT NULL
                ORDER BY h.FechaCambio DESC;
            END;
            ');
            """);

        migrationBuilder.Sql("""
            EXEC(N'
            CREATE OR ALTER PROCEDURE [dbo].[ListarRoles]
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT r.*
                FROM [dbo].[Roles] AS r
                ORDER BY r.Nombre;
            END;
            ');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP PROCEDURE IF EXISTS [dbo].[ListarProveedoresPorNegocio];
            DROP PROCEDURE IF EXISTS [dbo].[ListarCostosFijos];
            DROP PROCEDURE IF EXISTS [dbo].[ObtenerPreparacionesBaseMes];
            DROP PROCEDURE IF EXISTS [dbo].[ListarHistorialArchivadoPorInsumo];
            DROP PROCEDURE IF EXISTS [dbo].[ListarRoles];
            DROP TRIGGER IF EXISTS [dbo].[TR_InsumoProveedor_ValidarNegocio];
            DROP TRIGGER IF EXISTS [dbo].[TR_RecetaInsumo_ValidarNegocio];
            DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuInsumo_ValidarNegocio];
            DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuReceta_ValidarNegocio];
            DROP TRIGGER IF EXISTS [dbo].[TR_HistorialPrecioInsumo_ValidarNegocio];
            """);
    }
}
