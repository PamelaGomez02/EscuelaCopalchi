
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models
{
    public class InventarioCocinaProductoViewModel
    {
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "Indique el nombre del producto.")]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Indique la unidad de medida.")]
        [StringLength(50)]
        public string UnidadMedida { get; set; }

        [StringLength(100)]
        public string Categoria { get; set; }

        [Range(0, int.MaxValue)]
        public int StockActual { get; set; }

        [Range(0, int.MaxValue)]
        public int StockMinimo { get; set; }

        public bool Estado { get; set; }

        public bool EsPerecedero { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        public int DiasParaVencer
        {
            get
            {
                return FechaVencimiento.HasValue
                    ? (FechaVencimiento.Value.Date - DateTime.Today).Days
                    : int.MaxValue;
            }
        }

        public string EstadoProducto
        {
            get
            {
                if (!Estado)
                    return "INACTIVO";

                if (StockActual <= 0)
                    return "AGOTADO";

                if (FechaVencimiento.HasValue)
                {
                    if (DiasParaVencer < 0)
                        return "VENCIDO";

                    if (DiasParaVencer <= 7)
                        return "PROXIMO_A_VENCER";
                }

                if (StockActual <= StockMinimo)
                    return "STOCK_BAJO";

                return "DISPONIBLE";
            }
        }
    }

    public class InventarioCocinaLoteViewModel
    {
        public int IdLote { get; set; }

        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }

        [Required(ErrorMessage = "Indique la cantidad.")]
        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        [StringLength(150)]
        public string Proveedor { get; set; }

        public DateTime FechaIngreso { get; set; }

        public string EstadoLote
        {
            get
            {
                if (Cantidad <= 0)
                    return "AGOTADO";

                if (!FechaVencimiento.HasValue)
                    return "DISPONIBLE";

                int dias = (FechaVencimiento.Value.Date
                    - DateTime.Today).Days;

                if (dias < 0)
                    return "VENCIDO";

                if (dias <= 7)
                    return "PROXIMO_A_VENCER";

                return "DISPONIBLE";
            }
        }
    }

    public class InventarioCocinaMovimientoViewModel
    {
        public long IdMovimiento { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Seleccione un producto.")]
        public int IdProducto { get; set; }

        public string NombreProducto { get; set; }

        [Required(ErrorMessage = "Seleccione el movimiento.")]
        public string Tipo { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Cantidad { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        [StringLength(150)]
        public string Proveedor { get; set; }

        [StringLength(500)]
        public string Motivo { get; set; }

        public DateTime Fecha { get; set; }

        public string Responsable { get; set; }
    }

    public class InventarioCocinaIndexViewModel
    {
        public List<InventarioCocinaProductoViewModel> Productos
        { get; set; } =
                new List<InventarioCocinaProductoViewModel>();

        public int TotalProductos
        {
            get { return Productos.Count; }
        }

        public int ProductosVencidos
        {
            get
            {
                int total = 0;

                foreach (var producto in Productos)
                    if (producto.EstadoProducto == "VENCIDO")
                        total++;

                return total;
            }
        }

        public int ProximosAVencer
        {
            get
            {
                int total = 0;

                foreach (var producto in Productos)
                    if (producto.EstadoProducto == "PROXIMO_A_VENCER")
                        total++;

                return total;
            }
        }

        public int ProductosAgotados
        {
            get
            {
                int total = 0;

                foreach (var producto in Productos)
                    if (producto.EstadoProducto == "AGOTADO")
                        total++;

                return total;
            }
        }
    }
}
