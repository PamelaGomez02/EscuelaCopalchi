using System;
using System.Collections.Generic;

namespace EscuelaCopalchi.UI.Models.MonitoreoNuevo
{
    public class MonitoreoDashboardVM
    {
        public int Anio { get; set; }
        public int Trimestre { get; set; }
        public int IdGrupo { get; set; }
        public List<int> Anios { get; set; } = new List<int>();
        public List<MonitoreoGrupoItem> Grupos { get; set; } = new List<MonitoreoGrupoItem>();
        public List<MonitoreoEstudianteItem> Estudiantes { get; set; } = new List<MonitoreoEstudianteItem>();
        public List<MonitoreoEstudianteItem> Mejores { get; set; } = new List<MonitoreoEstudianteItem>();
        public List<MonitoreoEstudianteItem> Menores { get; set; } = new List<MonitoreoEstudianteItem>();
        public List<MonitoreoGrupoResumen> RankingGrupos { get; set; } = new List<MonitoreoGrupoResumen>();
        public decimal? PromedioGeneral { get; set; }
        public int TotalEvaluados { get; set; }
        public string Error { get; set; }
    }
    public class MonitoreoGrupoItem
    {
        public int IdGrupo { get; set; }
        public string Nombre { get; set; }
    }
    public class MonitoreoEstudianteItem
    {
        public int IdEstudiante { get; set; }
        public string Nombre { get; set; }
        public string Grupo { get; set; }
        public int IdGrupo { get; set; }
        public decimal Promedio { get; set; }
        public int Materias { get; set; }
    }
    public class MonitoreoGrupoResumen
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public decimal Promedio { get; set; }
        public int Estudiantes { get; set; }
    }
}
