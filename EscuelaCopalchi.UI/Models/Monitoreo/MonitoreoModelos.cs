using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // Permisos del módulo (se crean en BaseDatos/ema/9_SP_Monitoreo.sql)
    public static class PermisosMonitoreo
    {
        public const string Docente = "MONITOREO_DOCENTE";
        public const string Institucional = "MONITOREO_INSTITUCIONAL";
    }

    public static class EstadosRendimiento
    {
        public const string Aprobado = "Aprobado";
        public const string EnRiesgo = "En Riesgo";
        public const string Reprobado = "Reprobado";
        public const string SinNotas = "Sin calificaciones";

        public const string MensajeSinNotas = "Aún no existen calificaciones para calcular el promedio.";
        public const string MensajeSinInformacion = "No hay información disponible.";
    }

    // Umbrales de clasificación. Se leen de Web.config (appSettings) y, si faltan o son
    // inválidos, se usan 75 y 65.
    //   promedio >= NotaAprobado               -> Aprobado
    //   NotaMinima <= promedio < NotaAprobado  -> En Riesgo
    //   promedio < NotaMinima                  -> Reprobado
    public class UmbralesRendimiento
    {
        public const decimal AprobadoPorDefecto = 75m;
        public const decimal MinimaPorDefecto = 65m;

        public decimal NotaAprobado { get; set; }
        public decimal NotaMinima { get; set; }

        public static UmbralesRendimiento Actual()
        {
            decimal aprobado = Leer("MON_NotaAprobado", AprobadoPorDefecto);
            decimal minima = Leer("MON_NotaMinima", MinimaPorDefecto);

            if (minima >= aprobado)
            {
                aprobado = AprobadoPorDefecto;
                minima = MinimaPorDefecto;
            }

            return new UmbralesRendimiento { NotaAprobado = aprobado, NotaMinima = minima };
        }

        private static decimal Leer(string clave, decimal predeterminado)
        {
            decimal v;
            string txt = ConfigurationManager.AppSettings[clave];

            if (!string.IsNullOrWhiteSpace(txt)
                && decimal.TryParse(txt.Trim().Replace(',', '.'), NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out v)
                && v > 0 && v <= 100)
            {
                return v;
            }
            return predeterminado;
        }

        public string Clasificar(decimal? promedio)
        {
            if (!promedio.HasValue) return EstadosRendimiento.SinNotas;
            if (promedio.Value >= NotaAprobado) return EstadosRendimiento.Aprobado;
            if (promedio.Value >= NotaMinima) return EstadosRendimiento.EnRiesgo;
            return EstadosRendimiento.Reprobado;
        }
    }

    // Una fila de SP_MON_Notas_Finales: estudiante + asignatura + trimestre.
    // Asignatura = "" y Trimestre = null cuando el grupo todavía no tiene evaluaciones.
    public class NotaFinalFila
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string Estudiante { get; set; }
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public string Seccion { get; set; }
        public int Anio { get; set; }
        public string Asignatura { get; set; }
        public int? Trimestre { get; set; }
        public int EvaluacionesTotal { get; set; }
        public int EvaluacionesConNota { get; set; }
        public decimal? NotaFinal { get; set; }     // null = aún sin notas

        public bool TieneAsignatura { get { return !string.IsNullOrEmpty(Asignatura); } }
    }

    // Promedio y estado de un estudiante en un grupo (HU docente: "visualizar promedios").
    public class RendimientoEstudiante
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string Estudiante { get; set; }
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public string Seccion { get; set; }
        public int Anio { get; set; }

        public decimal? Promedio { get; set; }      // null = sin calificaciones
        public string Estado { get; set; }
        public int NotasUsadas { get; set; }        // asignatura/trimestre con nota que entraron al promedio
        public List<NotaFinalFila> Detalle { get; set; } = new List<NotaFinalFila>();

        public bool TieneNotas { get { return Promedio.HasValue; } }
        public string Mensaje { get { return TieneNotas ? "" : EstadosRendimiento.MensajeSinNotas; } }

        public string Iniciales
        {
            get
            {
                var p = (Estudiante ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0) return "?";
                if (p.Length == 1) return p[0].Substring(0, 1).ToUpperInvariant();
                return (p[0].Substring(0, 1) + p[1].Substring(0, 1)).ToUpperInvariant();
            }
        }
    }

    // Notas de una asignatura por trimestre (detalle de un estudiante).
    public class NotaPorAsignatura
    {
        public string Asignatura { get; set; }
        public decimal? T1 { get; set; }
        public decimal? T2 { get; set; }
        public decimal? T3 { get; set; }
        public decimal? Promedio { get; set; }
    }

    // Totales de un conjunto de estudiantes (un grupo, una sección, un nivel o una materia).
    public class ResumenRendimiento
    {
        public int Id { get; set; }                 // id del grupo cuando aplica, si no 0
        public string Etiqueta { get; set; }
        public int Total { get; set; }
        public int ConNotas { get; set; }
        public int SinNotas { get; set; }
        public int Aprobados { get; set; }
        public int EnRiesgo { get; set; }
        public int Reprobados { get; set; }
        public decimal? Promedio { get; set; }      // promedio de los promedios de los estudiantes

        public bool TieneDatos { get { return Total > 0; } }

        public decimal? PorcentajeAprobacion
        {
            get
            {
                return ConNotas == 0
                    ? (decimal?)null
                    : Math.Round(Aprobados * 100m / ConNotas, 1, MidpointRounding.AwayFromZero);
            }
        }
    }

    // Opciones de los filtros (grupos, años, estudiantes).
    public class GrupoMonitoreoItem
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public string Seccion { get; set; }
        public int Anio { get; set; }
        public string Docente { get; set; }
        public int TotalEstudiantes { get; set; }
    }

    public class EstudianteBusquedaItem
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string Estudiante { get; set; }
        public string GrupoActual { get; set; }
    }
}