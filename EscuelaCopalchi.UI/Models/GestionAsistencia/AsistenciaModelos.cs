using System;
using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.GestionAsistencia
{
    public static class PermisosAsistencia
    {
        public const string Registrar = "REGISTRAR_ASISTENCIA";
        public const string VerTodo = "VER_TODA_ASISTENCIA";
    }

    public static class EstadosAsistencia
    {
        public const string Presente = "Presente";
        public const string Ausente = "Ausente";
        public const string Justificado = "Justificado";
        public const string Tardanza = "Tardanza";

        public static readonly string[] Todos = { Presente, Ausente, Justificado, Tardanza };
    }

    public class GrupoAsistenciaItem
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public int Anio { get; set; }
        public string Periodo { get; set; }
        public string Docente { get; set; }
        public int TotalEstudiantes { get; set; }
    }

    // Una fila de asistencia. IdAsistencia es null cuando el estudiante aún no tiene registro ese día.
    public class AsistenciaItem
    {
        public int? IdAsistencia { get; set; }
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; }
        public DateTime? Fecha { get; set; }
        public string Estado { get; set; }
        public string HoraLlegada { get; set; }
        public string Observacion { get; set; }
    }

    // Lo que envía el formulario de Registrar (una fila por estudiante).
    public class AsistenciaGuardarItem
    {
        public int IdEstudiante { get; set; }
        public string Estado { get; set; }
        public string HoraLlegada { get; set; }
        public string Observacion { get; set; }
    }

    public class PorcentajeItem
    {
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; }
        public int TotalRegistros { get; set; }
        public int Presentes { get; set; }
        public int Tardanzas { get; set; }
        public int Justificados { get; set; }
        public int Ausentes { get; set; }
        public decimal? Porcentaje { get; set; }   // null = sin registros en el rango
    }

    // Totales y porcentaje (Presentes, Ausentes, etc.)
    public class ResumenAsistencia
    {
        public int Total { get; set; }
        public int Presentes { get; set; }
        public int Tardanzas { get; set; }
        public int Justificados { get; set; }
        public int Ausentes { get; set; }
        public decimal? Porcentaje { get; set; }

        // A partir de una lista de asistencia (consulta e historial)
        public static ResumenAsistencia De(IEnumerable<AsistenciaItem> filas)
        {
            var l = (filas ?? Enumerable.Empty<AsistenciaItem>())
                    .Where(f => f.IdAsistencia.HasValue).ToList();

            var r = new ResumenAsistencia
            {
                Total = l.Count,
                Presentes = l.Count(f => f.Estado == EstadosAsistencia.Presente),
                Tardanzas = l.Count(f => f.Estado == EstadosAsistencia.Tardanza),
                Justificados = l.Count(f => f.Estado == EstadosAsistencia.Justificado),
                Ausentes = l.Count(f => f.Estado == EstadosAsistencia.Ausente)
            };

            // Misma fórmula que el procedimiento: (Presente + Tardanza) / total
            if (r.Total > 0)
                r.Porcentaje = Math.Round((r.Presentes + r.Tardanzas) * 100m / r.Total, 2);

            return r;
        }

        // A partir de las filas por estudiante (pantalla de porcentaje)
        public static ResumenAsistencia De(IEnumerable<PorcentajeItem> filas)
        {
            var l = (filas ?? Enumerable.Empty<PorcentajeItem>()).ToList();

            var r = new ResumenAsistencia
            {
                Total = l.Sum(x => x.TotalRegistros),
                Presentes = l.Sum(x => x.Presentes),
                Tardanzas = l.Sum(x => x.Tardanzas),
                Justificados = l.Sum(x => x.Justificados),
                Ausentes = l.Sum(x => x.Ausentes)
            };

            if (r.Total > 0)
                r.Porcentaje = Math.Round((r.Presentes + r.Tardanzas) * 100m / r.Total, 2);

            return r;
        }
    }

    // ---------------- View models de las pantallas ----------------

    public class AsistenciaIndexVM
    {
        public List<GrupoAsistenciaItem> Grupos { get; set; } = new List<GrupoAsistenciaItem>();
        public int IdGrupo { get; set; }
        public string NombreGrupo { get; set; }
        public DateTime Fecha { get; set; }
        public bool Consultado { get; set; }
        public bool PuedeEditar { get; set; }
        public List<AsistenciaItem> Filas { get; set; } = new List<AsistenciaItem>();
        public ResumenAsistencia Resumen { get; set; }
    }

    public class AsistenciaRegistrarVM
    {
        public List<GrupoAsistenciaItem> Grupos { get; set; } = new List<GrupoAsistenciaItem>();
        public int IdGrupo { get; set; }
        public DateTime Fecha { get; set; }
        public bool Cargada { get; set; }        // ya se cargó la lista de estudiantes
        public bool YaRegistrada { get; set; }   // el grupo ya tiene asistencia ese día
        public List<AsistenciaItem> Filas { get; set; } = new List<AsistenciaItem>();
    }

    public class AsistenciaHistorialVM
    {
        public const int MaxFilas = 500;

        public List<GrupoAsistenciaItem> Grupos { get; set; } = new List<GrupoAsistenciaItem>();
        public List<AsistenciaItem> Estudiantes { get; set; } = new List<AsistenciaItem>();   // para el selector

        public int IdGrupo { get; set; }
        public string NombreGrupo { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        public bool Consultado { get; set; }
        public List<AsistenciaItem> Filas { get; set; } = new List<AsistenciaItem>();
        public int TotalFilas { get; set; }                 // total antes de recortar a MaxFilas
        public ResumenAsistencia Resumen { get; set; }

        public bool Truncado { get { return TotalFilas > MaxFilas; } }
    }

    public class AsistenciaPorcentajeVM
    {
        public List<GrupoAsistenciaItem> Grupos { get; set; } = new List<GrupoAsistenciaItem>();
        public int IdGrupo { get; set; }
        public string NombreGrupo { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        public bool Consultado { get; set; }
        public List<PorcentajeItem> Filas { get; set; } = new List<PorcentajeItem>();
        public ResumenAsistencia Resumen { get; set; }
    }
}