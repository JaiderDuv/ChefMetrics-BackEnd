using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class DivisorGlobalPreparaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // En bases creadas por fuera de EF la columna ya existe; en bases
            // nuevas hay que crearla.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Negocio', N'PreparacionesBaseMes') IS NULL
                    ALTER TABLE [Negocio] ADD [PreparacionesBaseMes] int NULL;
                """);

            // Las columnas por fila tenían restricciones CHECK y un valor por
            // defecto. SQL Server no permite eliminarlas sin retirar antes esas
            // restricciones. En una base nueva ValorDivisor nunca se creó, por
            // eso la eliminación también es condicional.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CK_CostoFijo_UnidadProrrateo]', N'C') IS NOT NULL
                    ALTER TABLE [CostoFijo] DROP CONSTRAINT [CK_CostoFijo_UnidadProrrateo];
                IF OBJECT_ID(N'[CK_CostoFijo_ValorDivisor]', N'C') IS NOT NULL
                    ALTER TABLE [CostoFijo] DROP CONSTRAINT [CK_CostoFijo_ValorDivisor];

                DECLARE @dfUnidad sysname;
                SELECT @dfUnidad = d.name
                  FROM sys.default_constraints AS d
                  INNER JOIN sys.columns AS c
                    ON c.object_id = d.parent_object_id
                   AND c.column_id = d.parent_column_id
                 WHERE d.parent_object_id = OBJECT_ID(N'[CostoFijo]')
                   AND c.name = N'UnidadProrrateo';
                IF @dfUnidad IS NOT NULL
                    EXEC(N'ALTER TABLE [CostoFijo] DROP CONSTRAINT [' + @dfUnidad + N'];');

                DECLARE @dfValor sysname;
                SELECT @dfValor = d.name
                  FROM sys.default_constraints AS d
                  INNER JOIN sys.columns AS c
                    ON c.object_id = d.parent_object_id
                   AND c.column_id = d.parent_column_id
                 WHERE d.parent_object_id = OBJECT_ID(N'[CostoFijo]')
                   AND c.name = N'ValorDivisor';
                IF @dfValor IS NOT NULL
                    EXEC(N'ALTER TABLE [CostoFijo] DROP CONSTRAINT [' + @dfValor + N'];');

                IF COL_LENGTH(N'CostoFijo', N'UnidadProrrateo') IS NOT NULL
                    EXEC(N'ALTER TABLE [CostoFijo] DROP COLUMN [UnidadProrrateo];');
                IF COL_LENGTH(N'CostoFijo', N'ValorDivisor') IS NOT NULL
                    EXEC(N'ALTER TABLE [CostoFijo] DROP COLUMN [ValorDivisor];');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreparacionesBaseMes",
                table: "Negocio");

            migrationBuilder.AddColumn<string>(
                name: "UnidadProrrateo",
                table: "CostoFijo",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "unidades_producidas");

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDivisor",
                table: "CostoFijo",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CK_CostoFijo_UnidadProrrateo]', N'C') IS NULL
                    ALTER TABLE [CostoFijo] WITH CHECK
                        ADD CONSTRAINT [CK_CostoFijo_UnidadProrrateo]
                        CHECK ([UnidadProrrateo]='recetas_activas'
                            OR [UnidadProrrateo]='horas_produccion'
                            OR [UnidadProrrateo]='unidades_producidas');
                IF OBJECT_ID(N'[CK_CostoFijo_ValorDivisor]', N'C') IS NULL
                    ALTER TABLE [CostoFijo] WITH CHECK
                        ADD CONSTRAINT [CK_CostoFijo_ValorDivisor]
                        CHECK ([ValorDivisor]>(0));
                """);
        }
    }
}
