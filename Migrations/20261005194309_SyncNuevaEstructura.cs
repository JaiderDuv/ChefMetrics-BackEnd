using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class SyncNuevaEstructura : Migration
    {
        // Migración de sincronización: la base de datos YA contiene la columna
        // Insumo.Categoria y las tablas Proveedor, InsumoProveedor e
        // HistorialPrecioInsumo (creadas fuera de EF). Up/Down intencionalmente
        // vacíos: solo se registra en __EFMigrationsHistory y se actualiza el
        // snapshot del modelo para futuras migraciones.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
