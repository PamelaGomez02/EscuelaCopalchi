using System;
using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.Home
{
    public class HomeDashboardViewModel
    {
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public bool EsAdministrador { get; set; }
        public HashSet<string> Permisos { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool Puede(params string[] codigos) => codigos.Any(Permisos.Contains);
        public int? Estudiantes { get; set; }
        public int? Usuarios { get; set; }
        public int? Grupos { get; set; }
        public decimal? Asistencia { get; set; }
        public decimal? PromedioNotas { get; set; }
        public int? PlanesPendientes { get; set; }
        public int? Alertas { get; set; }
        public int? Notificaciones { get; set; }
        public int? StockBajo { get; set; }
        public List<SerieMes> AsistenciaMensual { get; set; } = new List<SerieMes>();
        public List<SerieGrupo> NotasPorGrupo { get; set; } = new List<SerieGrupo>();
        public List<ActividadHome> Actividad { get; set; } = new List<ActividadHome>();
        public List<ModuloHome> Modulos { get; set; } = new List<ModuloHome>();
    }
    public class SerieMes { public string Mes { get; set; } public decimal Valor { get; set; } }
    public class SerieGrupo { public string Grupo { get; set; } public decimal Valor { get; set; } }
    public class ActividadHome { public string Accion { get; set; } public string Descripcion { get; set; } public DateTime Fecha { get; set; } }
    public class ModuloHome
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Icono { get; set; }
        public string Controlador { get; set; }
        public string Accion { get; set; } = "Index";
        public string[] Permisos { get; set; }
    }
}
