using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChefMetrics_BackEnd.Migrations;

[DbContext(typeof(ChefMetricsContext))]
[Migration("20261107030000_EmpleadoSesionVersion")]
public sealed class EmpleadoSesionVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SessionVersion",
            table: "Empleado",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SessionVersion", table: "Empleado");
    }
}
