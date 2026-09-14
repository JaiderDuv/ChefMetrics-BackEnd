using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Model;

public partial class ChedMetricsContext : DbContext
{
    public ChedMetricsContext()
    {
    }

    public ChedMetricsContext(DbContextOptions<ChedMetricsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<UsuarioEmprendedor> UsuarioEmprendedors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-615JQ39;Database=ChedMetrics;Trusted_Connection=true;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado);

            entity.HasIndex(e => e.CorreoElectronicoEmpleado, "UQ_Empleados_Correo").IsUnique();

            entity.Property(e => e.CorreoElectronicoEmpleado)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Correo_Electronico_Empleado");
            entity.Property(e => e.DocIdentidadEmpleado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Doc_IdentidadEmpleado");
            entity.Property(e => e.HashContrasena)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Hash_Contrasena");
            entity.Property(e => e.NombreEmpleado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Empleado");
            entity.Property(e => e.NumeroTel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Numero_tel");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolEmpleado");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK_IdRol");

            entity.Property(e => e.NombreRol)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Rol");
        });

        modelBuilder.Entity<UsuarioEmprendedor>(entity =>
        {
            entity.HasKey(e => e.IdUsuarioEmprendedor).HasName("PK_UsuarioE");

            entity.ToTable("UsuarioEmprendedor");

            entity.HasIndex(e => e.CorreoElectronico, "UQ_UsuarioE_Correo").IsUnique();

            entity.Property(e => e.IdUsuarioEmprendedor).HasColumnName("IdUsuario_Emprendedor");
            entity.Property(e => e.CategoriaNegocio)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Categoria_Negocio");
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Correo_Electronico");
            entity.Property(e => e.CorreoElectronicoContacto)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Correo_Electronico_Contacto");
            entity.Property(e => e.DocumentoIdentidadUe)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Documento_IdentidadUE");
            entity.Property(e => e.HashContrasena)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue("No_cambio")
                .HasColumnName("Hash_Contrasena");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NIT");
            entity.Property(e => e.NombreEmprendimiento)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Nombre_Emprendimiento");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Razon_Social");
            entity.Property(e => e.TelefonoContacto)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Telefono_Contacto");
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Tipo_Documento");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.UsuarioEmprendedors)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolUsuarioE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
