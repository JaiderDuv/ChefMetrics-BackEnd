using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDensidadInsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // En algunas bases la columna ya se creó a mano; no la duplicamos.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Insumo', N'DensidadGPorMl') IS NULL
                    ALTER TABLE [Insumo] ADD [DensidadGPorMl] decimal(10,4) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DensidadGPorMl",
                table: "Insumo");
        }
    }
}
