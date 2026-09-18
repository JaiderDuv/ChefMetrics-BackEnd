using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class AddRefreshTokenFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshToken_Expira",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken_Hash",
                table: "RefreshTokens",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RefreshToken_Revocado",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken_Expira",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RefreshToken_Hash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RefreshToken_Revocado",
                table: "RefreshTokens");
        }
    }
}
