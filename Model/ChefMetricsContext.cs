using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Model;

public partial class ChefMetricsContext : DbContext
{
    public ChefMetricsContext(DbContextOptions<ChefMetricsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CostoFijo> CostoFijos { get; set; }

    public virtual DbSet<Cuenta> Cuenta { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<HistorialPrecioInsumo> HistorialPrecioInsumos { get; set; }

    public virtual DbSet<Insumo> Insumos { get; set; }

    public virtual DbSet<InsumoProveedor> InsumoProveedors { get; set; }

    public virtual DbSet<Negocio> Negocios { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<PerfilEditGrant> PerfilEditGrants { get; set; }

    public virtual DbSet<ProductoMenu> ProductoMenus { get; set; }

    public virtual DbSet<ProductoMenuRecetum> ProductoMenuReceta { get; set; }

    public virtual DbSet<ProductoMenuInsumo> ProductoMenuInsumos { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<RecetaInsumo> RecetaInsumos { get; set; }

    public virtual DbSet<Receta> Receta { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TipoEmprendimiento> TipoEmprendimientos { get; set; }

    public virtual DbSet<UsuarioEmprendedor> UsuarioEmprendedors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=.;Database=ChefMetrics;Trusted_Connection=true;TrustServerCertificate=true;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostoFijo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CostoFij__3214EC0770A7B8C6");

            entity.ToTable("CostoFijo");

            entity.HasIndex(e => e.NegocioId, "IX_CostoFijo_NegocioId");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Categoria)
                .HasMaxLength(50)
                .HasDefaultValue("Otros");
            entity.Property(e => e.MontoMensual).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.Negocio).WithMany(p => p.CostoFijos)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_CostoFijo_Negocio");
        });

        modelBuilder.Entity<Cuenta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cuenta__3214EC079E423EF8");

            entity.HasIndex(e => e.Correo, "UQ_Cuenta_Correo").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NombreCompleto).HasMaxLength(150);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Empleado__3214EC07B989D586");

            entity.ToTable("Empleado");

            entity.HasIndex(e => e.InvitadoPor, "IX_Empleado_InvitadoPor");

            entity.HasIndex(e => e.RolId, "IX_Empleado_RolId");

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

        modelBuilder.Entity<HistorialPrecioInsumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Historia__3214EC07AA1990A3");

            entity.ToTable("HistorialPrecioInsumo");

            entity.HasIndex(e => e.InsumoProveedorId, "IX_HistorialPrecioInsumo_InsumoProveedorId");

            entity.Property(e => e.CostoAnterior).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CostoNuevo).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Cantidad).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.FechaCambio).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NombreInsumo).HasMaxLength(150);
            entity.Property(e => e.ProveedorNombre).HasMaxLength(150);
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.InsumoProveedor).WithMany(p => p.HistorialPrecioInsumos)
                .HasForeignKey(d => d.InsumoProveedorId)
                .HasConstraintName("FK_HistorialPrecioInsumo_InsumoProveedor");
        });

        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Insumo__3214EC079C35B1C5");

            entity.ToTable("Insumo");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_Insumo_NegocioNombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Categoria)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("otro");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Cantidad).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.DensidadGPorMl).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Proveedor).HasMaxLength(150);
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Insumos)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Insumo_Negocio");
        });

        modelBuilder.Entity<InsumoProveedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InsumoPr__3214EC07BAA8F3C6");

            entity.ToTable("InsumoProveedor");

            entity.HasIndex(e => e.ProveedorId, "IX_InsumoProveedor_ProveedorId");

            entity.HasIndex(e => new { e.InsumoId, e.ProveedorId }, "UQ_InsumoProveedor").IsUnique();

            entity.HasIndex(e => e.InsumoId, "UQ_InsumoProveedor_Principal")
                .IsUnique()
                .HasFilter("([EsPrincipal]=(1))");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Costo).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Insumo).WithMany(p => p.InsumoProveedors)
                .HasForeignKey(d => d.InsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InsumoProveedor_Insumo");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.InsumoProveedors)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InsumoProveedor_Proveedor");
        });

        modelBuilder.Entity<Negocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Negocio__3214EC07CF68471B");

            entity.ToTable("Negocio");

            entity.HasIndex(e => e.TipoEmprendimientoId, "IX_Negocio_TipoEmprendimientoId");

            entity.HasIndex(e => e.UsuarioId, "UQ_Negocio_Usuario").IsUnique();

            entity.Property(e => e.CorreoContacto).HasMaxLength(255);
            entity.Property(e => e.CorreoEmpresa).HasMaxLength(255);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Rues)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TelefonoContacto)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UmbralMargenMinimo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PreparacionesBaseMes);

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
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07AD9074A4");

            entity.HasIndex(e => e.CuentaId, "IX_PasswordResetTokens_CuentaId");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Tipo).HasMaxLength(50);
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.CuentaId)
                .HasConstraintName("FK_PasswordResetTokens_Cuenta");
        });

        modelBuilder.Entity<PerfilEditGrant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PerfilEd__3214EC0755CC1067");

            entity.HasIndex(e => e.CuentaId, "IX_PerfilEditGrants_CuentaId");

            entity.HasIndex(e => e.TokenHash, "UQ_PerfilEditGrant_TokenHash").IsUnique();

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Cuenta).WithMany(p => p.PerfilEditGrants).HasForeignKey(d => d.CuentaId);
        });

        modelBuilder.Entity<ProductoMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producto__3214EC070EF49875");

            entity.ToTable("ProductoMenu");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_ProductoMenu_NegocioNombre").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImagenUrl);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Negocio).WithMany(p => p.ProductoMenus)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_ProductoMenu_Negocio");
        });

        modelBuilder.Entity<ProductoMenuRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Producto__3214EC073CF0454B");

            entity.HasIndex(e => e.RecetaId, "IX_ProductoMenuReceta_RecetaId");

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

        modelBuilder.Entity<ProductoMenuInsumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductoMenuInsumo");

            entity.ToTable("ProductoMenuInsumo");

            entity.HasIndex(e => e.InsumoId, "IX_ProductoMenuInsumo_InsumoId");

            entity.HasIndex(e => new { e.ProductoMenuId, e.InsumoId }, "UQ_ProductoMenuInsumo").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(12, 3)");

            entity.Property(e => e.UnidadMedida).HasMaxLength(10);

            entity.HasOne(d => d.ProductoMenu).WithMany(p => p.ProductoMenuInsumos)
                .HasForeignKey(d => d.ProductoMenuId)
                .HasConstraintName("FK_ProductoMenuInsumo_Producto");

            entity.HasOne(d => d.Insumo).WithMany()
                .HasForeignKey(d => d.InsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoMenuInsumo_Insumo");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Proveedo__3214EC072B116B88");

            entity.ToTable("Proveedor");

            entity.HasIndex(e => e.NegocioId, "IX_Proveedor_NegocioId");

            entity.Property(e => e.Correo).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Proveedors)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Proveedor_Negocio");
        });

        modelBuilder.Entity<RecetaInsumo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RecetaIn__3214EC07FBC68EB1");

            entity.ToTable("RecetaInsumo");

            entity.HasIndex(e => e.InsumoId, "IX_RecetaInsumo_InsumoId");

            entity.HasIndex(e => new { e.RecetaId, e.InsumoId }, "UQ_RecetaInsumo").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.PorcentajeMerma)
                .HasColumnType("decimal(5, 2)")
                .HasDefaultValue(0m);
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

        modelBuilder.Entity<Receta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Receta__3214EC078EB3406B");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "UQ_Receta_NegocioNombre").IsUnique();

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Categoria).HasMaxLength(30);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Receta)
                .HasForeignKey(d => d.NegocioId)
                .HasConstraintName("FK_Receta_Negocio");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07527DA724");

            entity.HasIndex(e => e.CuentaId, "IX_RefreshTokens_CuentaId");

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
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07D8B07346");

            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<TipoEmprendimiento>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoEmpr__3214EC07966BAD77");

            entity.ToTable("TipoEmprendimiento");

            entity.HasIndex(e => e.Nombre, "UQ_TipoEmprendimiento_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<UsuarioEmprendedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsuarioE__3214EC076185DF2C");

            entity.ToTable("UsuarioEmprendedor");

            entity.HasIndex(e => e.CuentaId, "UQ_UsuarioEmprendedor_Cuenta").IsUnique();

            entity.HasIndex(e => e.Nit, "UQ_UsuarioEmprendedor_NIT")
                .IsUnique()
                .HasFilter("([NIT] IS NOT NULL)");

            entity.HasIndex(e => e.NumeroDocumento, "UQ_UsuarioEmprendedor_NumeroDocumento")
                .IsUnique()
                .HasFilter("([NumeroDocumento] IS NOT NULL)");

            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FotoPerfilUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NIT");
            entity.Property(e => e.NombreEmprendimiento).HasMaxLength(150);
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasDefaultValue("");
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
