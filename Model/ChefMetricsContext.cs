using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Model;

public partial class ChefMetricsContext : DbContext
{
    public ChefMetricsContext()
    {
    }

    public ChefMetricsContext(DbContextOptions<ChefMetricsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CostoFijo> CostoFijos { get; set; }

    public virtual DbSet<Cuentum> Cuenta { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<Insumo> Insumos { get; set; }

    public virtual DbSet<Negocio> Negocios { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<ProductoMenu> ProductoMenus { get; set; }

    public virtual DbSet<ProductoMenuRecetum> ProductoMenuReceta { get; set; }

    public virtual DbSet<RecetaInsumo> RecetaInsumos { get; set; }

    public virtual DbSet<Recetum> Receta { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TipoEmprendimiento> TipoEmprendimientos { get; set; }

    public virtual DbSet<UsuarioEmprendedor> UsuarioEmprendedors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-615JQ39;Database=ChefMetrics;Trusted_Connection=true;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostoFijo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CostoFij__3214EC072F85996B");

            entity.ToTable("CostoFijo");

            entity.Property(e => e.MontoMensual).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.UnidadProrrateo)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Negocio).WithMany(p => p.CostoFijos)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_CostoFijo_Negocio");
        });

        modelBuilder.Entity<Cuentum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cuenta__3214EC077F390334");

            entity.HasIndex(e => e.Correo, "UQ_Cuenta_Correo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Empleado__3214EC07FB53A3CF");

            entity.ToTable("Empleado");

            entity.HasIndex(e => new { e.NegocioId, e.CorreoInvitado }, "UQ_Empleado_CorreoNegocio").IsUnique();

            entity.HasIndex(e => e.CuentaId, "UQ_Empleado_Cuenta")
                .IsUnique()
                .HasFilter("([CuentaId] IS NOT NULL)");

            entity.HasIndex(e => e.TokenInvitacion, "UQ_Empleado_Token")
                .IsUnique()
                .HasFilter("([TokenInvitacion] IS NOT NULL)");

            entity.Property(e => e.CorreoInvitado).HasMaxLength(255);
            entity.Property(e => e.EstadoInvitacion)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pendiente");
            entity.Property(e => e.FechaInvitacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenInvitacion)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithOne(p => p.Empleado)
                .HasForeignKey<Empleado>(d => d.CuentaId)
                .HasConstraintName("FK_Empleado_Cuenta");

            entity.HasOne(d => d.InvitadoPorNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.InvitadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleado_InvitadoPor");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Empleado_Negocio");

            entity.HasOne(d => d.Rol).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleado_Rol");
        });

        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Insumo__3214EC077D4ECF38");

            entity.ToTable("Insumo");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_Insumo_NegocioNombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.PorcentajeMerma).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Proveedor).HasMaxLength(150);
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Insumos)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Insumo_Negocio");
        });

        modelBuilder.Entity<Negocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Negocio__3214EC078E9A5500");

            entity.ToTable("Negocio");

            entity.HasIndex(e => e.UsuarioId, "UQ_Negocio_Usuario").IsUnique();

            entity.Property(e => e.CorreoContacto).HasMaxLength(255);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TelefonoContacto)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.TipoEmprendimiento).WithMany(p => p.Negocios)
                .HasForeignKey(d => d.TipoEmprendimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Negocio_TipoEmprendimiento");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Negocio)
                .HasForeignKey<Negocio>(d => d.UsuarioId)
                .HasConstraintName("FK_Negocio_Usuario");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07F910C144");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.CuentaId)
                .HasConstraintName("FK_PasswordResetTokens_Cuenta");
        });

        modelBuilder.Entity<ProductoMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producto__3214EC0782EE917A");

            entity.ToTable("ProductoMenu");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_ProductoMenu_NegocioNombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Negocio).WithMany(p => p.ProductoMenus)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_ProductoMenu_Negocio");
        });

        modelBuilder.Entity<ProductoMenuRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producto__3214EC07832CE19B");

            entity.HasIndex(e => new { e.ProductoMenuId, e.RecetaId }, "UQ_ProductoMenuReceta").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(12, 3)");

            entity.HasOne(d => d.ProductoMenu).WithMany(p => p.ProductoMenuReceta)
                .HasForeignKey(d => d.ProductoMenuId)
                .HasConstraintName("FK_ProductoMenuReceta_Producto");

            entity.HasOne(d => d.Receta).WithMany(p => p.ProductoMenuReceta)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoMenuReceta_Receta");
        });

        modelBuilder.Entity<RecetaInsumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RecetaIn__3214EC07E9666E15");

            entity.ToTable("RecetaInsumo");

            entity.HasIndex(e => new { e.RecetaId, e.InsumoId }, "UQ_RecetaInsumo").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Insumo).WithMany(p => p.RecetaInsumos)
                .HasForeignKey(d => d.InsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecetaInsumo_Insumo");

            entity.HasOne(d => d.Receta).WithMany(p => p.RecetaInsumos)
                .HasForeignKey(d => d.RecetaId)
                .HasConstraintName("FK_RecetaInsumo_Receta");
        });

        modelBuilder.Entity<Recetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Receta__3214EC073424F899");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_Receta_NegocioNombre").IsUnique();

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Receta)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Receta_Negocio");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07AFA0CE9A");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.CuentaId)
                .HasConstraintName("FK_RefreshTokens_Cuenta");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0722B9A582");

            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<TipoEmprendimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoEmpr__3214EC07F05B8645");

            entity.ToTable("TipoEmprendimiento");

            entity.HasIndex(e => e.Nombre, "UQ_TipoEmprendimiento_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<UsuarioEmprendedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsuarioE__3214EC072683BE2F");

            entity.ToTable("UsuarioEmprendedor");

            entity.HasIndex(e => e.CuentaId, "UQ_UsuarioEmprendedor_Cuenta").IsUnique();

            entity.HasIndex(e => e.Nit, "UQ_UsuarioEmprendedor_NIT")
                .IsUnique()
                .HasFilter("([NIT] IS NOT NULL)");

            entity.HasIndex(e => e.NumeroDocumento, "UQ_UsuarioEmprendedor_NumeroDocumento")
                .IsUnique()
                .HasFilter("([NumeroDocumento] IS NOT NULL)");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NIT");
            entity.Property(e => e.NombreEmprendimiento).HasMaxLength(150);
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocial).HasMaxLength(150);
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.TipoFormalizacion)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithOne(p => p.UsuarioEmprendedor)
                .HasForeignKey<UsuarioEmprendedor>(d => d.CuentaId)
                .HasConstraintName("FK_UsuarioEmprendedor_Cuenta");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
