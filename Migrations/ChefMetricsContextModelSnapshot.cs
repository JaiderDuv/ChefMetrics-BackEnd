using System;
using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    [DbContext(typeof(ChefMetricsContext))]
    partial class ChefMetricsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.CostoFijo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("MontoMensual")
                        .HasColumnType("decimal(12, 2)");

                    b.Property<int>("NegocioId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("UnidadProrrateo")
                        .IsRequired()
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)");

                    b.HasKey("Id")
                        .HasName("PK__CostoFij__3214EC072F85996B");

                    b.HasIndex("NegocioId");

                    b.ToTable("CostoFijo", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Cuenta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Correo")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("EmailVerificado")
                        .HasColumnType("bit");

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("TipoCuenta")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id")
                        .HasName("PK__Cuenta__3214EC077F390334");

                    b.HasIndex(new[] { "Correo" }, "UQ_Cuenta_Correo")
                        .IsUnique();

                    b.ToTable("Cuenta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Empleado", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CorreoInvitado")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("CuentaId")
                        .HasColumnType("int");

                    b.Property<string>("EstadoInvitacion")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasDefaultValue("pendiente");

                    b.Property<DateTime?>("FechaAceptacion")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("FechaInvitacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<int>("InvitadoPor")
                        .HasColumnType("int");

                    b.Property<int>("NegocioId")
                        .HasColumnType("int");

                    b.Property<int>("RolId")
                        .HasColumnType("int");

                    b.Property<string>("TokenInvitacion")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id")
                        .HasName("PK__Empleado__3214EC07FB53A3CF");

                    b.HasIndex("InvitadoPor");

                    b.HasIndex("RolId");

                    b.HasIndex(new[] { "NegocioId", "CorreoInvitado" }, "UQ_Empleado_CorreoNegocio")
                        .IsUnique();

                    b.HasIndex(new[] { "CuentaId" }, "UQ_Empleado_Cuenta")
                        .IsUnique()
                        .HasFilter("([CuentaId] IS NOT NULL)");

                    b.HasIndex(new[] { "TokenInvitacion" }, "UQ_Empleado_Token")
                        .IsUnique()
                        .HasFilter("([TokenInvitacion] IS NOT NULL)");

                    b.ToTable("Empleado", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Insumo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<decimal>("CostoUnitario")
                        .HasColumnType("decimal(12, 2)");

                    b.Property<DateTime>("FechaActualizacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<int>("NegocioId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<decimal>("PorcentajeMerma")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<string>("Proveedor")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)");

                    b.HasKey("Id")
                        .HasName("PK__Insumo__3214EC077D4ECF38");

                    b.HasIndex(new[] { "NegocioId", "Nombre" }, "UQ_Insumo_NegocioNombre")
                        .IsUnique();

                    b.ToTable("Insumo", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Negocio", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CorreoContacto")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Direccion")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("FechaActualizacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<string>("TelefonoContacto")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)");

                    b.Property<int>("TipoEmprendimientoId")
                        .HasColumnType("int");

                    b.Property<int>("UsuarioId")
                        .HasColumnType("int");

                    b.HasKey("Id")
                        .HasName("PK__Negocio__3214EC078E9A5500");

                    b.HasIndex("TipoEmprendimientoId");

                    b.HasIndex(new[] { "UsuarioId" }, "UQ_Negocio_Usuario")
                        .IsUnique();

                    b.ToTable("Negocio", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.PasswordResetToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CuentaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<DateTime>("FechaExpiracion")
                        .HasColumnType("datetime2");

                    b.Property<string>("Tipo")
                        .HasMaxLength(30)
                        .IsUnicode(false)
                        .HasColumnType("varchar(30)");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("Usado")
                        .HasColumnType("bit");

                    b.HasKey("Id")
                        .HasName("PK__Password__3214EC07F910C144");

                    b.HasIndex("CuentaId");

                    b.ToTable("PasswordResetTokens");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.ProductoMenu", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("Activo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<int>("NegocioId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<decimal?>("PrecioVenta")
                        .HasColumnType("decimal(12, 2)");

                    b.HasKey("Id")
                        .HasName("PK__Producto__3214EC0782EE917A");

                    b.HasIndex(new[] { "NegocioId", "Nombre" }, "UQ_ProductoMenu_NegocioNombre")
                        .IsUnique();

                    b.ToTable("ProductoMenu", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.ProductoMenuRecetum", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(12, 3)");

                    b.Property<int>("ProductoMenuId")
                        .HasColumnType("int");

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.HasKey("Id")
                        .HasName("PK__Producto__3214EC07832CE19B");

                    b.HasIndex("RecetaId");

                    b.HasIndex(new[] { "ProductoMenuId", "RecetaId" }, "UQ_ProductoMenuReceta")
                        .IsUnique();

                    b.ToTable("ProductoMenuReceta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Receta", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<string>("Instrucciones")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NegocioId")
                        .HasColumnType("int");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int>("PorcionesBase")
                        .HasColumnType("int");

                    b.HasKey("Id")
                        .HasName("PK__Receta__3214EC073424F899");

                    b.HasIndex(new[] { "NegocioId", "Nombre" }, "UQ_Receta_NegocioNombre")
                        .IsUnique();

                    b.ToTable("Receta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RecetaInsumo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Cantidad")
                        .HasColumnType("decimal(12, 3)");

                    b.Property<int>("InsumoId")
                        .HasColumnType("int");

                    b.Property<int>("RecetaId")
                        .HasColumnType("int");

                    b.Property<string>("UnidadMedida")
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasColumnType("varchar(10)");

                    b.HasKey("Id")
                        .HasName("PK__RecetaIn__3214EC07E9666E15");

                    b.HasIndex("InsumoId");

                    b.HasIndex(new[] { "RecetaId", "InsumoId" }, "UQ_RecetaInsumo")
                        .IsUnique();

                    b.ToTable("RecetaInsumo", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RefreshToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CuentaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<DateTime>("FechaExpiracion")
                        .HasColumnType("datetime2");

                    b.Property<bool>("Revocado")
                        .HasColumnType("bit");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id")
                        .HasName("PK__RefreshT__3214EC07AFA0CE9A");

                    b.HasIndex("CuentaId");

                    b.ToTable("RefreshTokens");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id")
                        .HasName("PK__Roles__3214EC0722B9A582");

                    b.HasIndex(new[] { "Nombre" }, "UQ_Roles_Nombre")
                        .IsUnique();

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.TipoEmprendimiento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id")
                        .HasName("PK__TipoEmpr__3214EC07F05B8645");

                    b.HasIndex(new[] { "Nombre" }, "UQ_TipoEmprendimiento_Nombre")
                        .IsUnique();

                    b.ToTable("TipoEmprendimiento", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CuentaId")
                        .HasColumnType("int");

                    b.Property<DateTime>("FechaCreacion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("(sysutcdatetime())");

                    b.Property<string>("Nit")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("NIT");

                    b.Property<string>("NombreEmprendimiento")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("NumeroDocumento")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)");

                    b.Property<bool>("OnboardingCompletado")
                        .HasColumnType("bit");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<string>("TipoDocumento")
                        .HasMaxLength(5)
                        .IsUnicode(false)
                        .HasColumnType("varchar(5)");

                    b.Property<string>("TipoFormalizacion")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)");

                    b.HasKey("Id")
                        .HasName("PK__UsuarioE__3214EC072683BE2F");

                    b.HasIndex(new[] { "CuentaId" }, "UQ_UsuarioEmprendedor_Cuenta")
                        .IsUnique();

                    b.HasIndex(new[] { "Nit" }, "UQ_UsuarioEmprendedor_NIT")
                        .IsUnique()
                        .HasFilter("([NIT] IS NOT NULL)");

                    b.HasIndex(new[] { "NumeroDocumento" }, "UQ_UsuarioEmprendedor_NumeroDocumento")
                        .IsUnique()
                        .HasFilter("([NumeroDocumento] IS NOT NULL)");

                    b.ToTable("UsuarioEmprendedor", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.CostoFijo", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Negocio", "Negocio")
                        .WithMany("CostoFijos")
                        .HasForeignKey("NegocioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_CostoFijo_Negocio");

                    b.Navigation("Negocio");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Empleado", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Cuenta", "Cuenta")
                        .WithOne("Empleado")
                        .HasForeignKey("ChefMetrics_BackEnd.Model.Empleado", "CuentaId")
                        .HasConstraintName("FK_Empleado_Cuenta");

                    b.HasOne("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", "InvitadoPorNavigation")
                        .WithMany("Empleados")
                        .HasForeignKey("InvitadoPor")
                        .IsRequired()
                        .HasConstraintName("FK_Empleado_InvitadoPor");

                    b.HasOne("ChefMetrics_BackEnd.Model.Negocio", "Negocio")
                        .WithMany("Empleados")
                        .HasForeignKey("NegocioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Empleado_Negocio");

                    b.HasOne("ChefMetrics_BackEnd.Model.Role", "Rol")
                        .WithMany("Empleados")
                        .HasForeignKey("RolId")
                        .IsRequired()
                        .HasConstraintName("FK_Empleado_Rol");

                    b.Navigation("Cuenta");

                    b.Navigation("InvitadoPorNavigation");

                    b.Navigation("Negocio");

                    b.Navigation("Rol");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Insumo", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Negocio", "Negocio")
                        .WithMany("Insumos")
                        .HasForeignKey("NegocioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Insumo_Negocio");

                    b.Navigation("Negocio");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Negocio", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.TipoEmprendimiento", "TipoEmprendimiento")
                        .WithMany("Negocios")
                        .HasForeignKey("TipoEmprendimientoId")
                        .IsRequired()
                        .HasConstraintName("FK_Negocio_TipoEmprendimiento");

                    b.HasOne("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", "Usuario")
                        .WithOne("Negocio")
                        .HasForeignKey("ChefMetrics_BackEnd.Model.Negocio", "UsuarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Negocio_Usuario");

                    b.Navigation("TipoEmprendimiento");

                    b.Navigation("Usuario");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.PasswordResetToken", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Cuenta", "Cuenta")
                        .WithMany("PasswordResetTokens")
                        .HasForeignKey("CuentaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_PasswordResetTokens_Cuenta");

                    b.Navigation("Cuenta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.ProductoMenu", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Negocio", "Negocio")
                        .WithMany("ProductoMenus")
                        .HasForeignKey("NegocioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_ProductoMenu_Negocio");

                    b.Navigation("Negocio");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.ProductoMenuRecetum", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.ProductoMenu", "ProductoMenu")
                        .WithMany("ProductoMenuReceta")
                        .HasForeignKey("ProductoMenuId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_ProductoMenuReceta_Producto");

                    b.HasOne("ChefMetrics_BackEnd.Model.Receta", "Receta")
                        .WithMany("ProductoMenuReceta")
                        .HasForeignKey("RecetaId")
                        .IsRequired()
                        .HasConstraintName("FK_ProductoMenuReceta_Receta");

                    b.Navigation("ProductoMenu");

                    b.Navigation("Receta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Receta", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Negocio", "Negocio")
                        .WithMany("Receta")
                        .HasForeignKey("NegocioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Receta_Negocio");

                    b.Navigation("Negocio");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RecetaInsumo", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Insumo", "Insumo")
                        .WithMany("RecetaInsumos")
                        .HasForeignKey("InsumoId")
                        .IsRequired()
                        .HasConstraintName("FK_RecetaInsumo_Insumo");

                    b.HasOne("ChefMetrics_BackEnd.Model.Receta", "Receta")
                        .WithMany("RecetaInsumos")
                        .HasForeignKey("RecetaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_RecetaInsumo_Receta");

                    b.Navigation("Insumo");

                    b.Navigation("Receta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RefreshToken", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Cuenta", "Cuenta")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("CuentaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_RefreshTokens_Cuenta");

                    b.Navigation("Cuenta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Cuenta", "Cuenta")
                        .WithOne("UsuarioEmprendedor")
                        .HasForeignKey("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", "CuentaId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_UsuarioEmprendedor_Cuenta");

                    b.Navigation("Cuenta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Cuenta", b =>
                {
                    b.Navigation("Empleado");

                    b.Navigation("PasswordResetTokens");

                    b.Navigation("RefreshTokens");

                    b.Navigation("UsuarioEmprendedor");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Insumo", b =>
                {
                    b.Navigation("RecetaInsumos");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Negocio", b =>
                {
                    b.Navigation("CostoFijos");

                    b.Navigation("Empleados");

                    b.Navigation("Insumos");

                    b.Navigation("ProductoMenus");

                    b.Navigation("Receta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.ProductoMenu", b =>
                {
                    b.Navigation("ProductoMenuReceta");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Receta", b =>
                {
                    b.Navigation("ProductoMenuReceta");

                    b.Navigation("RecetaInsumos");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Role", b =>
                {
                    b.Navigation("Empleados");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.TipoEmprendimiento", b =>
                {
                    b.Navigation("Negocios");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.Navigation("Empleados");

                    b.Navigation("Negocio");
                });
#pragma warning restore 612, 618
        }
    }
}
