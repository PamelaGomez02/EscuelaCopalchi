using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models.Auditoria
{
    public class AuditoriaRegistro
    {
        public long IdAuditoria { get; set; }
        public int? IdUsuario { get; set; }
        public string Responsable { get; set; }
        public string Rol { get; set; }
        public string Accion { get; set; }
        public string Modulo { get; set; }
        public string TablaAfectada { get; set; }
        public string RegistroAfectado { get; set; }
        public string Descripcion { get; set; }
        public string DireccionIp { get; set; }
        public DateTime Fecha { get; set; }
        public string Resultado { get; set; }
        public bool RegistroIncompleto { get; set; }
    }

    public class AuditoriaFiltro
    {
        public int? IdUsuario { get; set; }
        [StringLength(100)] public string Modulo { get; set; }
        [StringLength(100)] public string Accion { get; set; }
        [DataType(DataType.Date)] public DateTime? Desde { get; set; }
        [DataType(DataType.Date)] public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
    }

    public class AuditoriaListadoVM
    {
        public AuditoriaFiltro Filtro { get; set; } = new AuditoriaFiltro();
        public List<AuditoriaRegistro> Registros { get; set; } = new List<AuditoriaRegistro>();
        public long Total { get; set; }
        public long TotalPaginas { get { return (Total + Filtro.TamanoPagina - 1) / Filtro.TamanoPagina; } }
    }
}
