using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLogoUrlNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // En algunas bases la columna ya existe (se agregó manualmente antes
            // de registrar la migración), así que se crea solo si falta.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Negocio', N'LogoUrl') IS NULL
                    ALTER TABLE [Negocio] ADD [LogoUrl] nvarchar(max) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'Negocio', N'LogoUrl') IS NOT NULL
                    ALTER TABLE [Negocio] DROP COLUMN [LogoUrl];
                """);
        }
    }
}
