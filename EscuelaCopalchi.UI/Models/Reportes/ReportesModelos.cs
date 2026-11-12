using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    public class EstudianteRendimiento
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string Estudiante { get; set; }
        public decimal? Promedio { get; set; }
        public int Periodos { get; set; }
        public int Clases { get; set; }
        public int Presentes { get; set; }
        public int Tardanzas { get; set; }
        public int Justificados { get; set; }
        public int Ausentes { get; set; }
        public decimal? PctAsistencia { get; set; }
        public bool BajoRendimiento { get; set; }
        public bool BajaAsistencia { get; set; }
        public bool EnRiesgo { get { return BajoRendimiento || BajaAsistencia; } }
    }

    public class AsignaturaPromedio
    {
        public string Asignatura { get; set; }
        public decimal Promedio { get; set; }
        public int Estudiantes { get; set; }
    }

    public class ReportesViewModel
    {
        public int Grupo { get; set; }
        public string Buscar { get; set; }
        public List<EscuelaCopalchi.UI.Models.GestionNotas.GrupoNotasItem> Grupos { get; set; }
        public List<EstudianteRendimiento> Estudiantes { get; set; }
        public List<AsignaturaPromedio> Asignaturas { get; set; }
        public List<EstudianteRendimiento> ComparacionGrupos { get; set; }

        public ReportesViewModel()
        {
            Grupos = new List<EscuelaCopalchi.UI.Models.GestionNotas.GrupoNotasItem>();
            Estudiantes = new List<EstudianteRendimiento>();
            Asignaturas = new List<AsignaturaPromedio>();
            ComparacionGrupos = new List<EstudianteRendimiento>();
        }

        public decimal? PromedioGeneral
        {
            get
            {
                var l = Estudiantes.Where(e => e.Promedio.HasValue).ToList();
                return l.Count == 0 ? (decimal?)null : System.Math.Round(l.Average(e => e.Promedio.Value), 1);
            }
        }

        public decimal? AsistenciaGeneral
        {
            get
            {
                int clases = Estudiantes.Sum(e => e.Clases);
                int aus = Estudiantes.Sum(e => e.Ausentes);
                return clases == 0 ? (decimal?)null : System.Math.Round(100m * (clases - aus) / clases, 1);
            }
        }

        public int EnRiesgo { get { return Estudiantes.Count(e => e.EnRiesgo); } }
    }
}
