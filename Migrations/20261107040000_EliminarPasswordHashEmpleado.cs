using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ChefMetrics_BackEnd.Migrations;

[DbContext(typeof(ChefMetricsContext))]
[Migration("20261107040000_EliminarPasswordHashEmpleado")]
public sealed class EliminarPasswordHashEmpleado : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Some databases contain this legacy column although it is absent from
        // the current Empleado model and is not used by authentication.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Empleado', N'PasswordHash') IS NOT NULL
                ALTER TABLE [dbo].[Empleado] DROP COLUMN [PasswordHash];
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The old hash values are intentionally not recoverable after Up.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Empleado', N'PasswordHash') IS NULL
                ALTER TABLE [dbo].[Empleado]
                ADD [PasswordHash] varchar(255) NULL;
            """);
    }
}
