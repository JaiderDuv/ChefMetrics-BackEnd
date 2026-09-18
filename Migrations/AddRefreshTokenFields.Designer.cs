using System;
using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    [DbContext(typeof(ChedMetricsContext))]
    [Migration("20260918135930_AddRefreshTokenFields")]
    partial class AddRefreshTokenFields
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Empleado", b =>
                {
                    b.Property<int>("IdEmpleado")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdEmpleado"));

                    b.Property<string>("CorreoElectronicoEmpleado")
                        .IsRequired()
                        .HasMaxLength(100)
                        .IsUnicode(false)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("Correo_Electronico_Empleado");

                    b.Property<string>("DocIdentidadEmpleado")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("Doc_IdentidadEmpleado");

                    b.Property<string>("HashContrasena")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("Hash_Contrasena");

                    b.Property<int>("IdRol")
                        .HasColumnType("int");

                    b.Property<string>("NombreEmpleado")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Nombre_Empleado");

                    b.Property<string>("NumeroTel")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("Numero_tel");

                    b.HasKey("IdEmpleado");

                    b.HasIndex("IdRol");

                    b.HasIndex(new[] { "CorreoElectronicoEmpleado" }, "UQ_Empleados_Correo")
                        .IsUnique();

                    b.ToTable("Empleados");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RefreshToken", b =>
                {
                    b.Property<int>("IdRefreshToken")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("Id_RefreshToken");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdRefreshToken"));

                    b.Property<bool>("CorreoVerificado")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("Correo_Verificado");

                    b.Property<int>("IdUsuarioEmprendedor")
                        .HasColumnType("int")
                        .HasColumnName("IdUsuario_Emprendedor");

                    b.Property<DateTime?>("RefreshTokenExpira")
                        .HasColumnType("datetime2")
                        .HasColumnName("RefreshToken_Expira");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(64)
                        .IsUnicode(false)
                        .HasColumnType("varchar(64)")
                        .HasColumnName("RefreshToken_Hash");

                    b.Property<bool>("RefreshTokenRevocado")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("RefreshToken_Revocado");

                    b.Property<DateTime?>("TokenVerificacionExpira")
                        .HasColumnType("datetime2")
                        .HasColumnName("Token_Verificacion_Expira");

                    b.Property<string>("TokenVerificacionHash")
                        .HasMaxLength(64)
                        .IsUnicode(false)
                        .HasColumnType("varchar(64)")
                        .HasColumnName("Token_Verificacion_Hash");

                    b.HasKey("IdRefreshToken");

                    b.HasIndex("IdUsuarioEmprendedor");

                    b.HasIndex("TokenVerificacionHash");

                    b.ToTable("RefreshTokens", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Role", b =>
                {
                    b.Property<int>("IdRol")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdRol"));

                    b.Property<string>("NombreRol")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Nombre_Rol");

                    b.HasKey("IdRol")
                        .HasName("PK_IdRol");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.Property<int>("IdUsuarioEmprendedor")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("IdUsuario_Emprendedor");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IdUsuarioEmprendedor"));

                    b.Property<string>("CategoriaNegocio")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Categoria_Negocio");

                    b.Property<string>("CorreoElectronico")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Correo_Electronico");

                    b.Property<string>("CorreoElectronicoContacto")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Correo_Electronico_Contacto");

                    b.Property<string>("DocumentoIdentidadUe")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Documento_IdentidadUE");

                    b.Property<bool>("Formalizado")
                        .HasColumnType("bit");

                    b.Property<string>("HashContrasena")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasDefaultValue("No_cambio")
                        .HasColumnName("Hash_Contrasena");

                    b.Property<int>("IdRol")
                        .HasColumnType("int");

                    b.Property<string>("Nit")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("NIT");

                    b.Property<string>("NombreEmprendimiento")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Nombre_Emprendimiento");

                    b.Property<bool>("OnboardingCompletado")
                        .HasColumnType("bit");

                    b.Property<string>("RazonSocial")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("Razon_Social");

                    b.Property<string>("TelefonoContacto")
                        .IsRequired()
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("Telefono_Contacto");

                    b.Property<string>("TipoDocumento")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("Tipo_Documento");

                    b.HasKey("IdUsuarioEmprendedor")
                        .HasName("PK_UsuarioE");

                    b.HasIndex("IdRol");

                    b.HasIndex(new[] { "CorreoElectronico" }, "UQ_UsuarioE_Correo")
                        .IsUnique();

                    b.ToTable("UsuarioEmprendedor", (string)null);
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Empleado", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Role", "IdRolNavigation")
                        .WithMany("Empleados")
                        .HasForeignKey("IdRol")
                        .IsRequired()
                        .HasConstraintName("FK_RolEmpleado");

                    b.Navigation("IdRolNavigation");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.RefreshToken", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", "IdUsuarioEmprendedorNavigation")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("IdUsuarioEmprendedor")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_RefreshTokens_UsuarioEmprendedor");

                    b.Navigation("IdUsuarioEmprendedorNavigation");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.HasOne("ChefMetrics_BackEnd.Model.Role", "IdRolNavigation")
                        .WithMany("UsuarioEmprendedors")
                        .HasForeignKey("IdRol")
                        .IsRequired()
                        .HasConstraintName("FK_RolUsuarioE");

                    b.Navigation("IdRolNavigation");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.Role", b =>
                {
                    b.Navigation("Empleados");

                    b.Navigation("UsuarioEmprendedors");
                });

            modelBuilder.Entity("ChefMetrics_BackEnd.Model.UsuarioEmprendedor", b =>
                {
                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
