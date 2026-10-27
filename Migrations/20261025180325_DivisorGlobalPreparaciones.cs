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
            migrationBuilder.AddColumn<int>(
                name: "PreparacionesBaseMes",
                table: "Negocio",
                type: "int",
                nullable: true);

            // Las columnas por fila tenían restricciones CHECK y un valor por
            // defecto. SQL Server no permite eliminarlas sin retirar antes esas
            // restricciones.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CK_CostoFijo_UnidadProrrateo]', N'C') IS NOT NULL
                    ALTER TABLE [CostoFijo] DROP CONSTRAINT [CK_CostoFijo_UnidadProrrateo];
                IF OBJECT_ID(N'[CK_CostoFijo_ValorDivisor]', N'C') IS NOT NULL
                    ALTER TABLE [CostoFijo] DROP CONSTRAINT [CK_CostoFijo_ValorDivisor];
                IF OBJECT_ID(N'[DF_CostoFijo_ValorDivisor]', N'D') IS NOT NULL
                    ALTER TABLE [CostoFijo] DROP CONSTRAINT [DF_CostoFijo_ValorDivisor];
                """);

            migrationBuilder.DropColumn(
                name: "UnidadProrrateo",
                table: "CostoFijo");

            migrationBuilder.DropColumn(
                name: "ValorDivisor",
                table: "CostoFijo");
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
