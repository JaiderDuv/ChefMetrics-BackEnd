using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AgregarImagenProductoMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Algunas bases ya tienen la columna creada a mano y la migración
            // no registrada; en ese caso no se debe intentar agregarla otra vez.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'ProductoMenu', N'ImagenUrl') IS NULL
                    ALTER TABLE [ProductoMenu] ADD [ImagenUrl] nvarchar(max) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'ProductoMenu', N'ImagenUrl') IS NOT NULL
                    ALTER TABLE [ProductoMenu] DROP COLUMN [ImagenUrl];
                """);
        }
    }
}
