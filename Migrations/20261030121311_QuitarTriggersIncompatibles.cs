using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class QuitarTriggersIncompatibles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF Core inserta filas con OUTPUT INSERTED.[Id]. SQL Server no
            // permite esa cláusula si la tabla tiene triggers habilitados.
            // La validación de negocio ya se hace en los servicios, así que
            // se eliminan los triggers para no bloquear las altas.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_InsumoProveedor_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_RecetaInsumo_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuInsumo_ValidarNegocio];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProductoMenuReceta_ValidarNegocio];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
