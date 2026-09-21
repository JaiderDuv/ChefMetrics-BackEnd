using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    public partial class BaselineV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    TipoCuenta = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EmailVerificado = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cuenta__3214EC077F390334", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__3214EC0722B9A582", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoEmprendimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TipoEmpr__3214EC07F05B8645", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
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
                    table.PrimaryKey("PK__Password__3214EC07F910C144", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Cuenta",
                        column: x => x.CuentaId,
                        principalTable: "Cuenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revocado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RefreshT__3214EC07AFA0CE9A", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Cuenta",
                        column: x => x.CuentaId,
                        principalTable: "Cuenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioEmprendedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    TipoFormalizacion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    NIT = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TipoDocumento = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true),
                    NumeroDocumento = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    NombreEmprendimiento = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OnboardingCompletado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UsuarioE__3214EC072683BE2F", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioEmprendedor_Cuenta",
                        column: x => x.CuentaId,
                        principalTable: "Cuenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TipoEmprendimientoId = table.Column<int>(type: "int", nullable: false),
                    TelefonoContacto = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CorreoContacto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Negocio__3214EC078E9A5500", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Negocio_TipoEmprendimiento",
                        column: x => x.TipoEmprendimientoId,
                        principalTable: "TipoEmprendimiento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Negocio_Usuario",
                        column: x => x.UsuarioId,
                        principalTable: "UsuarioEmprendedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostoFijo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NegocioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MontoMensual = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    UnidadProrrateo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CostoFij__3214EC072F85996B", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostoFijo_Negocio",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empleado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaId = table.Column<int>(type: "int", nullable: true),
                    NegocioId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    CorreoInvitado = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EstadoInvitacion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "pendiente"),
                    TokenInvitacion = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    InvitadoPor = table.Column<int>(type: "int", nullable: false),
                    FechaInvitacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    FechaAceptacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Empleado__3214EC07FB53A3CF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleado_Cuenta",
                        column: x => x.CuentaId,
                        principalTable: "Cuenta",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Empleado_InvitadoPor",
                        column: x => x.InvitadoPor,
                        principalTable: "UsuarioEmprendedor",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Empleado_Negocio",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Empleado_Rol",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Insumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NegocioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UnidadMedida = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PorcentajeMerma = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Insumo__3214EC077D4ECF38", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insumo_Negocio",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductoMenu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NegocioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Producto__3214EC0782EE917A", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoMenu_Negocio",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NegocioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PorcionesBase = table.Column<int>(type: "int", nullable: false),
                    Instrucciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Receta__3214EC073424F899", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receta_Negocio",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductoMenuReceta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoMenuId = table.Column<int>(type: "int", nullable: false),
                    RecetaId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Producto__3214EC07832CE19B", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoMenuReceta_Producto",
                        column: x => x.ProductoMenuId,
                        principalTable: "ProductoMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductoMenuReceta_Receta",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecetaInsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecetaId = table.Column<int>(type: "int", nullable: false),
                    InsumoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RecetaIn__3214EC07E9666E15", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaInsumo_Insumo",
                        column: x => x.InsumoId,
                        principalTable: "Insumo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecetaInsumo_Receta",
                        column: x => x.RecetaId,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostoFijo_NegocioId",
                table: "CostoFijo",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "UQ_Cuenta_Correo",
                table: "Cuenta",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_InvitadoPor",
                table: "Empleado",
                column: "InvitadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Empleado_RolId",
                table: "Empleado",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "UQ_Empleado_CorreoNegocio",
                table: "Empleado",
                columns: new[] { "NegocioId", "CorreoInvitado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Empleado_Cuenta",
                table: "Empleado",
                column: "CuentaId",
                unique: true,
                filter: "([CuentaId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_Empleado_Token",
                table: "Empleado",
                column: "TokenInvitacion",
                unique: true,
                filter: "([TokenInvitacion] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_Insumo_NegocioNombre",
                table: "Insumo",
                columns: new[] { "NegocioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_TipoEmprendimientoId",
                table: "Negocio",
                column: "TipoEmprendimientoId");

            migrationBuilder.CreateIndex(
                name: "UQ_Negocio_Usuario",
                table: "Negocio",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_CuentaId",
                table: "PasswordResetTokens",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductoMenu_NegocioNombre",
                table: "ProductoMenu",
                columns: new[] { "NegocioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoMenuReceta_RecetaId",
                table: "ProductoMenuReceta",
                column: "RecetaId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductoMenuReceta",
                table: "ProductoMenuReceta",
                columns: new[] { "ProductoMenuId", "RecetaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Receta_NegocioNombre",
                table: "Receta",
                columns: new[] { "NegocioId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecetaInsumo_InsumoId",
                table: "RecetaInsumo",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "UQ_RecetaInsumo",
                table: "RecetaInsumo",
                columns: new[] { "RecetaId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CuentaId",
                table: "RefreshTokens",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TipoEmprendimiento_Nombre",
                table: "TipoEmprendimiento",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioEmprendedor_Cuenta",
                table: "UsuarioEmprendedor",
                column: "CuentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioEmprendedor_NIT",
                table: "UsuarioEmprendedor",
                column: "NIT",
                unique: true,
                filter: "([NIT] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_UsuarioEmprendedor_NumeroDocumento",
                table: "UsuarioEmprendedor",
                column: "NumeroDocumento",
                unique: true,
                filter: "([NumeroDocumento] IS NOT NULL)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostoFijo");

            migrationBuilder.DropTable(
                name: "Empleado");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "ProductoMenuReceta");

            migrationBuilder.DropTable(
                name: "RecetaInsumo");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ProductoMenu");

            migrationBuilder.DropTable(
                name: "Insumo");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "Negocio");

            migrationBuilder.DropTable(
                name: "TipoEmprendimiento");

            migrationBuilder.DropTable(
                name: "UsuarioEmprendedor");

            migrationBuilder.DropTable(
                name: "Cuenta");
        }
    }
}
