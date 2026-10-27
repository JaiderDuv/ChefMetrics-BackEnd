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
            migrationBuilder.AddColumn<decimal>(
                name: "DensidadGPorMl",
                table: "Insumo",
                type: "decimal(10,4)",
                nullable: true);
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
