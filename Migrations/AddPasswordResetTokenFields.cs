using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class AddPasswordResetTokenFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetToken_Expira",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken_Hash",
                table: "RefreshTokens",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PasswordResetToken_Usado",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetToken_Expira",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken_Hash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken_Usado",
                table: "RefreshTokens");
        }
    }
}
