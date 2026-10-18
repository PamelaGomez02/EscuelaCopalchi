using System;
using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.GestionNotas
{
    public class GrupoNotasItem
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public int Anio { get; set; }
        public string Docente { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalEvaluaciones { get; set; }
        public int NotasRegistradas { get; set; }

        public int Pendientes
        {
            get { return Math.Max(0, TotalEstudiantes * TotalEvaluaciones - NotasRegistradas); }
        }

        public string Estado
        {
            get
            {
                if (TotalEvaluaciones == 0) return "Sin evaluaciones";
                return (Pendientes == 0 && TotalEstudiantes > 0) ? "Completo" : "En proceso";
            }
        }
    }

    public class EvaluacionItem
    {
        public int IdEvaluacion { get; set; }
        public int IdGrupo { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public string Nombre { get; set; }
        public decimal Porcentaje { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal NotaMaxima { get; set; }
    }

    // Una nota de un estudiante en una evaluación (Nota = null si todavía no se registra)
    public class NotaCelda
    {
        public int IdEvaluacion { get; set; }
        public string Evaluacion { get; set; }
        public decimal Porcentaje { get; set; }
        public decimal NotaMaxima { get; set; }
        public int? IdCalificacion { get; set; }
        public decimal? Nota { get; set; }
    }

    public class EstudianteNotas
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public List<NotaCelda> Notas { get; set; } = new List<NotaCelda>();

        // Promedio ponderado de las evaluaciones que ya tienen nota, sobre 100
        public decimal? NotaFinal
        {
            get
            {
                var con = Notas.Where(n => n.Nota.HasValue && n.NotaMaxima > 0).ToList();
                if (con.Count == 0) return null;
                decimal pct = con.Sum(n => n.Porcentaje);
                if (pct <= 0) return null;
                decimal puntos = con.Sum(n => n.Nota.Value / n.NotaMaxima * n.Porcentaje);
                return Math.Round(puntos / pct * 100m, 1);
            }
        }
    }

    public class RegistrarNotasViewModel
    {
        public int IdGrupo { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public List<GrupoNotasItem> Grupos { get; set; } = new List<GrupoNotasItem>();
        public List<string> Asignaturas { get; set; } = new List<string>();
        public List<EvaluacionItem> Evaluaciones { get; set; } = new List<EvaluacionItem>();
        public List<EstudianteNotas> Estudiantes { get; set; } = new List<EstudianteNotas>();

        public decimal PorcentajeTotal
        {
            get { return Evaluaciones.Sum(e => e.Porcentaje); }
        }
    }

    // Lo que llega del formulario (la nota llega como texto para evitar problemas de cultura)
    public class NotaInput
    {
        public int IdEvaluacion { get; set; }
        public int IdEstudiante { get; set; }
        public string Nota { get; set; }
    }

    public class NotaGuardar
    {
        public int IdEvaluacion { get; set; }
        public int IdEstudiante { get; set; }
        public decimal? Nota { get; set; }
        public string Observacion { get; set; }
    }

    public class EstudianteItem
    {
        public int IdEstudiante { get; set; }
        public string Nombre { get; set; }
    }

    public class HistorialItem
    {
        public string Origen { get; set; }              // "Escuela" o "Escuela anterior"
        public int? IdCalificacion { get; set; }
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; }
        public string Grupo { get; set; }
        public string Asignatura { get; set; }
        public string Periodo { get; set; }
        public int Anio { get; set; }
        public string Evaluacion { get; set; }
        public decimal Nota { get; set; }
        public decimal NotaMaxima { get; set; }
        public string EscuelaProcedencia { get; set; }
        public DateTime Fecha { get; set; }
        public int? IdDocente { get; set; }

        public decimal Porcentaje
        {
            get { return NotaMaxima > 0 ? Nota / NotaMaxima * 100m : 0m; }
        }
    }

    public class HistorialViewModel
    {
        public int Grupo { get; set; }
        public int Estudiante { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public int IdDocenteActual { get; set; }
        public bool PuedeEditar { get; set; }
        public List<GrupoNotasItem> Grupos { get; set; } = new List<GrupoNotasItem>();
        public List<EstudianteItem> Estudiantes { get; set; } = new List<EstudianteItem>();
        public List<HistorialItem> Items { get; set; } = new List<HistorialItem>();
    }

    public class CalificacionDetalle
    {
        public int IdCalificacion { get; set; }
        public int IdEvaluacion { get; set; }
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; }
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public int? IdDocente { get; set; }
        public string Evaluacion { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public decimal Porcentaje { get; set; }
        public decimal NotaMaxima { get; set; }
        public decimal Nota { get; set; }
        public string Observacion { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    public class NotaExterna
    {
        public int IdEstudiante { get; set; }
        public string Escuela { get; set; }
        public string Asignatura { get; set; }
        public string Periodo { get; set; }
        public int Anio { get; set; }
        public decimal Nota { get; set; }
    }

    public class ImportarViewModel
    {
        public int IdGrupo { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public List<GrupoNotasItem> Grupos { get; set; } = new List<GrupoNotasItem>();
        public List<string> Asignaturas { get; set; } = new List<string>();
        public List<EvaluacionItem> Evaluaciones { get; set; } = new List<EvaluacionItem>();
        public List<string> Errores { get; set; } = new List<string>();
    }
}
