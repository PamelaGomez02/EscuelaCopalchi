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
        public int Pagina { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public string Buscar { get; set; }
        public string Estado { get; set; }
        public string Materia { get; set; }
        public List<MonitoreoEstudianteItem> Listado { get; set; } = new List<MonitoreoEstudianteItem>();
        public List<MonitoreoMateriaResumen> RankingMaterias { get; set; } = new List<MonitoreoMateriaResumen>();
        public List<MonitoreoTrimestreResumen> Evolucion { get; set; } = new List<MonitoreoTrimestreResumen>();
        public int EnRiesgo { get; set; }
        public int Criticos { get; set; }
        public int AlertasPendientes { get; set; }
        public decimal? Asistencia { get; set; }
        public int SinNotas { get; set; }
        public int SeguimientosAbiertos { get; set; }
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
        public string Estado { get { return Promedio < 50 ? "Crítico" : Promedio < 65 ? "En riesgo" : "Satisfactorio"; } }
    }
    public class MonitoreoGrupoResumen
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public decimal Promedio { get; set; }
        public int Estudiantes { get; set; }
    }
    public class MonitoreoMateriaResumen { public string Materia { get; set; } public decimal Promedio { get; set; } public int Estudiantes { get; set; } }
    public class MonitoreoTrimestreResumen { public int Trimestre { get; set; } public decimal Promedio { get; set; } public int Estudiantes { get; set; } }
    public class MonitoreoDetalleVM { public int IdEstudiante { get; set; } public string Nombre { get; set; } public int Anio { get; set; } public int IdGrupo { get; set; } public string Grupo { get; set; } public List<MonitoreoNotaDetalle> Notas { get; set; } = new List<MonitoreoNotaDetalle>(); public List<MonitoreoSeguimientoItem> Seguimientos { get; set; } = new List<MonitoreoSeguimientoItem>(); public decimal? Promedio { get; set; } public decimal? Asistencia { get; set; } }
    public class MonitoreoNotaDetalle { public int Trimestre { get; set; } public string Materia { get; set; } public decimal Promedio { get; set; } }
    public class MonitoreoSeguimientoItem { public int IdSeguimiento { get; set; } public int IdEstudiante { get; set; } public int IdGrupo { get; set; } public int Anio { get; set; } public byte? Trimestre { get; set; } public string Estudiante { get; set; } public string Grupo { get; set; } public string NivelRiesgo { get; set; } public string Estado { get; set; } public string Observacion { get; set; } public string AccionRealizada { get; set; } public DateTime FechaRegistro { get; set; } }
    public class MonitoreoSeguimientoVM { public MonitoreoDashboardVM Dashboard { get; set; } public List<MonitoreoSeguimientoItem> Casos { get; set; } = new List<MonitoreoSeguimientoItem>(); public int Pagina { get; set; } public int TotalPaginas { get; set; } }
}

