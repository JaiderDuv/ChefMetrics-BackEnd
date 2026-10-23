using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <summary>
    /// Crea la tabla ProductoMenuInsumo para persistir los empaques (insumos
    /// directos) de un producto del menú.
    ///
    /// NOTA: este Up fue reducido a mano. Las demás diferencias que detectó EF
    /// (Receta.ImagenUrl, Negocio.UmbralMargenMinimo, columnas de
    /// InsumoProveedor e HistorialPrecioInsumo) cambiaron la base de datos por
    /// fuera de las migraciones; ya existen y volver a crearlas rompería la
    /// aplicación. El ModelSnapshot quedó actualizado al modelo completo.
    /// </summary>
    public partial class PersistirEmpaquesProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductoMenuInsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoMenuId = table.Column<int>(type: "int", nullable: false),
                    InsumoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoMenuInsumo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoMenuInsumo_Insumo",
                        column: x => x.InsumoId,
                        principalTable: "Insumo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductoMenuInsumo_Producto",
                        column: x => x.ProductoMenuId,
                        principalTable: "ProductoMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoMenuInsumo_InsumoId",
                table: "ProductoMenuInsumo",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductoMenuInsumo",
                table: "ProductoMenuInsumo",
                columns: new[] { "ProductoMenuId", "InsumoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductoMenuInsumo");
        }
    }
}
