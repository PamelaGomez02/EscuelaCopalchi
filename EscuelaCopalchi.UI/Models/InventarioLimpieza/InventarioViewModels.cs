using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models
{
    public class InventarioProductoViewModel
    {
        public int IdProducto { get; set; }
        [Required(ErrorMessage = "Indique el nombre.")][StringLength(150)] public string Nombre { get; set; }
        [StringLength(500)] public string Descripcion { get; set; }
        [Required(ErrorMessage = "Indique la unidad.")][StringLength(50)] public string UnidadMedida { get; set; }
        [StringLength(100)] public string Categoria { get; set; }
        [Range(0, int.MaxValue)] public int StockActual { get; set; }
        [Range(0, int.MaxValue)] public int StockMinimo { get; set; }
        [DataType(DataType.Date)] public DateTime? FechaVencimiento { get; set; }
        public bool Estado { get; set; }
        public bool StockBajo { get { return StockActual <= StockMinimo; } }
    }
    public class InventarioMovimientoViewModel
    {
        public long IdMovimiento { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto.")] public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        [Required] public string Tipo { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")] public int Cantidad { get; set; }
        [StringLength(150)] public string Proveedor { get; set; }
        [StringLength(500)] public string Motivo { get; set; }
        public int IdUsuario { get; set; }
        public string Responsable { get; set; }
        public DateTime Fecha { get; set; }
    }
    public class InventarioReporteFila
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public string Unidad { get; set; }
        public int Entradas { get; set; }
        public int Salidas { get; set; }
        public int SaldoFinal { get; set; }
    }
    public class InventarioReporteViewModel
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public List<InventarioReporteFila> Filas { get; set; } = new List<InventarioReporteFila>();
    }
}
