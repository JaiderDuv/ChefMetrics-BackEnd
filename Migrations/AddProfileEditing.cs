using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class AddProfileEditing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellidos",
                table: "UsuarioEmprendedor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombres",
                table: "UsuarioEmprendedor",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorreoEmpresa",
                table: "Negocio",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rues",
                table: "Negocio",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneSitioFisico",
                table: "Negocio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PerfilEditGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilEditGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilEditGrants_Cuenta_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SociosNegocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioEmprendedorId = table.Column<int>(type: "int", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoDocumento = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SociosNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SociosNegocio_UsuarioEmprendedor_UsuarioEmprendedorId",
                        column: x => x.UsuarioEmprendedorId,
                        principalTable: "UsuarioEmprendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilEditGrants_CuentaId",
                table: "PerfilEditGrants",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "UQ_PerfilEditGrant_TokenHash",
                table: "PerfilEditGrants",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SocioNegocio_Usuario_Documento",
                table: "SociosNegocio",
                columns: new[] { "UsuarioEmprendedorId", "NumeroDocumento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfilEditGrants");

            migrationBuilder.DropTable(
                name: "SociosNegocio");

            migrationBuilder.DropColumn(
                name: "Apellidos",
                table: "UsuarioEmprendedor");

            migrationBuilder.DropColumn(
                name: "Nombres",
                table: "UsuarioEmprendedor");

            migrationBuilder.DropColumn(
                name: "CorreoEmpresa",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "Rues",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "TieneSitioFisico",
                table: "Negocio");
        }
    }
}
