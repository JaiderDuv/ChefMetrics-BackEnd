using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class RepararUmbralMargenNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La migración PersistirEmpaquesProducto se recortó a mano y asumió que
            // esta columna ya existía en la base (se había creado por fuera de EF).
            // En bases nuevas no existe, así que la garantizamos aquí sin duplicarla.
            migrationBuilder.Sql(@"
IF COL_LENGTH('Negocio', 'UmbralMargenMinimo') IS NULL
    ALTER TABLE [Negocio] ADD [UmbralMargenMinimo] decimal(18,2) NOT NULL
        CONSTRAINT [DF_Negocio_UmbralMargenMinimo] DEFAULT (20);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se elimina: la columna puede haber existido antes de esta migración.
        }
    }
}
