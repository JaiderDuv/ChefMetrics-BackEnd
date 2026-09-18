using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class MoveVerificationTokensToRefreshTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id_RefreshToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario_Emprendedor = table.Column<int>(type: "int", nullable: false),
                    Token_Verificacion_Hash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    Token_Verificacion_Expira = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Correo_Verificado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id_RefreshToken);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_UsuarioEmprendedor",
                        column: x => x.IdUsuario_Emprendedor,
                        principalTable: "UsuarioEmprendedor",
                        principalColumn: "IdUsuario_Emprendedor",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IdUsuario_Emprendedor",
                table: "RefreshTokens",
                column: "IdUsuario_Emprendedor");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_Verificacion_Hash",
                table: "RefreshTokens",
                column: "Token_Verificacion_Hash");

            migrationBuilder.Sql("""
                INSERT INTO [RefreshTokens]
                    ([IdUsuario_Emprendedor], [Token_Verificacion_Hash],
                     [Token_Verificacion_Expira], [Correo_Verificado])
                SELECT [IdUsuario_Emprendedor], [Token_Verificacion_Hash],
                       [Token_Verificacion_Expira], [Correo_Verificado]
                FROM [UsuarioEmprendedor]
                WHERE [Token_Verificacion_Hash] IS NOT NULL
                   OR [Token_Verificacion_Expira] IS NOT NULL
                   OR [Correo_Verificado] = 1;
                """);

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

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.Sql("""
                UPDATE usuario
                SET [Correo_Verificado] = token.[Correo_Verificado],
                    [Token_Verificacion_Expira] = token.[Token_Verificacion_Expira],
                    [Token_Verificacion_Hash] = token.[Token_Verificacion_Hash]
                FROM [UsuarioEmprendedor] AS usuario
                INNER JOIN [RefreshTokens] AS token
                    ON token.[IdUsuario_Emprendedor] = usuario.[IdUsuario_Emprendedor]
                WHERE token.[Id_RefreshToken] = (
                    SELECT MAX(latest.[Id_RefreshToken])
                    FROM [RefreshTokens] AS latest
                    WHERE latest.[IdUsuario_Emprendedor] = usuario.[IdUsuario_Emprendedor]
                );
                """);

            migrationBuilder.DropTable(
                name: "RefreshTokens");
        }
    }
}
