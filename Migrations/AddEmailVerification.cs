using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class AddEmailVerification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Correo_Verificado",
                table: "UsuarioEmprendedor",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "Token_Verificacion_Expira",
                table: "UsuarioEmprendedor",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Token_Verificacion_Hash",
                table: "UsuarioEmprendedor",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Correo_Verificado",
                table: "UsuarioEmprendedor");

            migrationBuilder.DropColumn(
                name: "Token_Verificacion_Expira",
                table: "UsuarioEmprendedor");

            migrationBuilder.DropColumn(
                name: "Token_Verificacion_Hash",
                table: "UsuarioEmprendedor");
        }
    }
}
