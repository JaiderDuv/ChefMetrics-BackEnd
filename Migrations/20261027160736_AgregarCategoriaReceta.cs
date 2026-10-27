using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCategoriaReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // En algunas bases la columna ya se creó a mano (varchar(30)); no la duplicamos.
            migrationBuilder.Sql(@"
IF COL_LENGTH('Receta', 'Categoria') IS NULL
    ALTER TABLE [Receta] ADD [Categoria] nvarchar(30) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Receta");
        }
    }
}
