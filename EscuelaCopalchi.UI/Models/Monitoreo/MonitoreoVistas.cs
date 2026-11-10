using System;
using System.Collections.Generic;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // ---------------------------------------------------------------
    // Pantalla principal del docente: rendimiento de sus estudiantes
    // ---------------------------------------------------------------
    public class MonitoreoIndexVM
    {
        public List<int> Anios { get; set; } = new List<int>();
        public int Anio { get; set; }

        public List<GrupoMonitoreoItem> Grupos { get; set; } = new List<GrupoMonitoreoItem>();
        public int IdGrupo { get; set; }                       // 0 = todos mis grupos

        public List<string> Materias { get; set; } = new List<string>();
        public string Materia { get; set; } = "";              // "" = todas
        public int Trimestre { get; set; }                     // 0 = todos
        public string Estado { get; set; } = "";               // "" = todos
        public string Buscar { get; set; } = "";

        public List<RendimientoEstudiante> Estudiantes { get; set; } = new List<RendimientoEstudiante>();
        public ResumenRendimiento Resumen { get; set; } = new ResumenRendimiento();
        public UmbralesRendimiento Umbrales { get; set; }

        public bool SinInformacion { get; set; }               // el docente no tiene grupos con estudiantes
        public string Error { get; set; }
    }

    // ---------------------------------------------------------------
    // Generar reporte (por grupo o por materia)
    // ---------------------------------------------------------------
    public class MonitoreoReporteVM
    {
        public List<int> Anios { get; set; } = new List<int>();
        public int Anio { get; set; }

        public List<GrupoMonitoreoItem> Grupos { get; set; } = new List<GrupoMonitoreoItem>();
        public int IdGrupo { get; set; }                       // 0 = todavía no eligió grupo

        public List<string> Materias { get; set; } = new List<string>();
        public string Materia { get; set; } = "";              // "" = reporte del grupo completo
        public int Trimestre { get; set; }                     // 0 = todos

        public UmbralesRendimiento Umbrales { get; set; }

        public bool SinGrupos { get; set; }
        public bool Generado { get; set; }
        public ReporteMonitoreo Reporte { get; set; }
        public string MensajeVacio { get; set; }               // "No hay información disponible."
        public string Error { get; set; }
    }

    // Utilidades de presentación compartidas por todas las vistas del módulo.
    public static class MonitoreoUI
    {
        public static string Fmt(decimal? valor)
        {
            return valor.HasValue ? valor.Value.ToString("0.0", CultureInfo.InvariantCulture) : "-";
        }

        // Porcentaje para el ancho de las barras (ej. "33.3"); siempre con punto decimal
        public static string Pct(int parte, int total)
        {
            double v = total <= 0 ? 0 : parte * 100.0 / total;
            return v.ToString("0.#", CultureInfo.InvariantCulture);
        }

        public static string Trimestre(int t)
        {
            switch (t)
            {
                case 1: return "I Trimestre";
                case 2: return "II Trimestre";
                case 3: return "III Trimestre";
                default: return "Todos los trimestres";
            }
        }

        // Insignia del estado (clases definidas en _EstilosMonitoreo.cshtml)
        public static string ClaseEstado(string estado)
        {
            switch (estado)
            {
                case EstadosRendimiento.Aprobado: return "status-ok";
                case EstadosRendimiento.EnRiesgo: return "status-warning";
                case EstadosRendimiento.Reprobado: return "status-danger";
                default: return "status-muted";
            }
        }

        public static string IconoEstado(string estado)
        {
            switch (estado)
            {
                case EstadosRendimiento.Aprobado: return "bi-check-circle";
                case EstadosRendimiento.EnRiesgo: return "bi-exclamation-triangle";
                case EstadosRendimiento.Reprobado: return "bi-x-circle";
                default: return "bi-dash-circle";
            }
        }

        // Color de la nota según el estado del estudiante
        public static string ClaseNota(string estado)
        {
            switch (estado)
            {
                case EstadosRendimiento.Aprobado: return "grade-success";
                case EstadosRendimiento.EnRiesgo: return "grade-warning";
                case EstadosRendimiento.Reprobado: return "grade-danger";
                default: return "grade-muted";
            }
        }
    }
}