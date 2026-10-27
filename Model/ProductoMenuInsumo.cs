using System;
using System.Collections.Generic;

namespace ChefMetrics_BackEnd.Model;

// Insumos directos (empaques) asociados a un producto del menú.
// Antes eran transitorios y se perdían al guardar el producto, dejando el
// costo real subestimado (propuesta: costo directo de empaques).
public partial class ProductoMenuInsumo
{
    public int Id { get; set; }

    public int ProductoMenuId { get; set; }

    public int InsumoId { get; set; }

    // Cantidad del insumo directo medida en UnidadMedida.
    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = null!;

    public virtual ProductoMenu ProductoMenu { get; set; } = null!;

    public virtual Insumo Insumo { get; set; } = null!;
}
