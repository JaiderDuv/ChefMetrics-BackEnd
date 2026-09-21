using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class Negocio
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public int TipoEmprendimientoId { get; set; }

    public string TelefonoContacto { get; set; } = null!;

    public string CorreoContacto { get; set; } = null!;

    public string? Direccion { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public virtual ICollection<CostoFijo> CostoFijos { get; set; } = new List<CostoFijo>();

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();

    public virtual ICollection<ProductoMenu> ProductoMenus { get; set; } = new List<ProductoMenu>();

    public virtual ICollection<Receta> Receta { get; set; } = new List<Receta>();

    public virtual TipoEmprendimiento TipoEmprendimiento { get; set; } = null!;

    public virtual UsuarioEmprendedor Usuario { get; set; } = null!;
}
