using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChefMetrics_BackEnd.Migrations;

[DbContext(typeof(ChefMetricsContext))]
[Migration("20261107020000_EmpleadoAcceso")]
public sealed class EmpleadoAcceso : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Activo",
            table: "Empleado",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "CredencialCifrada",
            table: "Empleado",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TokenInvitacionExpira",
            table: "Empleado",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Activo", table: "Empleado");
        migrationBuilder.DropColumn(name: "CredencialCifrada", table: "Empleado");
        migrationBuilder.DropColumn(name: "TokenInvitacionExpira", table: "Empleado");
    }
}
