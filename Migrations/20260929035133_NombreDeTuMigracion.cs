using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class NombreDeTuMigracion : Migration
    {
        // Migración de sincronización: agregaba Apellidos y Nombres en
        // UsuarioEmprendedor, pero AddProfileEditing (20260927202548, anterior en
        // el orden) ya las agrega y en la base de datos real ya existen.
        // Duplicarlas rompía Update-Database ("'Apellidos' se especifica más de
        // una vez"). Up/Down intencionalmente vacíos: solo se registra en
        // __EFMigrationsHistory.
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
