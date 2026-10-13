using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

public partial class HistorialPrecioInsumo
{
    public int Id { get; set; }

    public int? InsumoProveedorId { get; set; }

    public int? NegocioId { get; set; }

    public int? InsumoIdOriginal { get; set; }

    public string? NombreInsumo { get; set; }

    public string? ProveedorNombre { get; set; }

    public string? UnidadMedida { get; set; }

    public decimal CostoAnterior { get; set; }

    public decimal CostoNuevo { get; set; }

    public decimal? Cantidad { get; set; }

    public DateTime FechaCambio { get; set; }

    public DateTime? FechaEliminacion { get; set; }

    public virtual InsumoProveedor InsumoProveedor { get; set; } = null!;
}
