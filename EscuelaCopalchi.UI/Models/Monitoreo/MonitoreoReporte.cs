using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // Un reporte académico ya armado: sirve igual para la pantalla, Excel y PDF.
    public class ReporteMonitoreo
    {
        public string Tipo { get; set; }                 // "Grupo" o "Materia"
        public string Titulo { get; set; }
        public string Subtitulo { get; set; }
        public string Grupo { get; set; }
        public string Docente { get; set; }
        public string Materia { get; set; }              // "" en el reporte por grupo
        public int Anio { get; set; }
        public int Trimestre { get; set; }
        public DateTime Generado { get; set; }

        public List<string> Columnas { get; set; } = new List<string>();
        public List<string[]> Filas { get; set; } = new List<string[]>();

        public ResumenRendimiento Resumen { get; set; }
        public UmbralesRendimiento Umbrales { get; set; }

        // Nombre de archivo sin extensión (sin tildes ni caracteres raros)
        public string NombreArchivo
        {
            get
            {
                string baseNombre = "Reporte_" + Tipo + "_" + Grupo
                    + (string.IsNullOrEmpty(Materia) ? "" : "_" + Materia)
                    + "_" + Anio + "_" + Generado.ToString("yyyyMMdd");
                return Limpiar(baseNombre);
            }
        }

        private static string Limpiar(string texto)
        {
            string d = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }
            return sb.ToString();
        }
    }

    public static class MonitoreoReporteBuilder
    {
        private static string Resumen(ResumenRendimiento r)
        {
            return "Promedio " + MonitoreoUI.Fmt(r.Promedio)
                 + " | Aprobados " + r.Aprobados
                 + " | En riesgo " + r.EnRiesgo
                 + " | Reprobados " + r.Reprobados
                 + " | Sin calificaciones " + r.SinNotas;
        }

        // Reporte del grupo: una fila por estudiante, una columna por materia.
        public static ReporteMonitoreo PorGrupo(
            GrupoMonitoreoItem grupo, IEnumerable<NotaFinalFila> filas,
            UmbralesRendimiento umbrales, int trimestre)
        {
            var todas = filas.ToList();
            var estudiantes = MonitoreoCalculo.ArmarRendimiento(todas, umbrales, trimestre);

            var materias = todas.Where(f => f.TieneAsignatura)
                                .Select(f => f.Asignatura)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(a => a, StringComparer.CurrentCultureIgnoreCase)
                                .ToList();

            var r = Base("Grupo", grupo, umbrales, trimestre, "");
            r.Resumen = MonitoreoCalculo.Resumir(grupo.Grupo, estudiantes, grupo.IdGrupo);

            r.Columnas.Add("Estudiante");
            r.Columnas.Add("Identificación");
            r.Columnas.AddRange(materias);
            r.Columnas.Add("Promedio");
            r.Columnas.Add("Estado");

            foreach (var e in estudiantes)
            {
                var porMateria = MonitoreoCalculo.PorAsignatura(e.Detalle);
                var fila = new List<string> { e.Estudiante, e.Identificacion };

                foreach (string m in materias)
                {
                    var n = porMateria.FirstOrDefault(x =>
                        string.Equals(x.Asignatura, m, StringComparison.OrdinalIgnoreCase));
                    fila.Add(MonitoreoUI.Fmt(n == null ? null : n.Promedio));
                }

                fila.Add(MonitoreoUI.Fmt(e.Promedio));
                fila.Add(e.Estado);
                r.Filas.Add(fila.ToArray());
            }

            r.Titulo = "Reporte académico del grupo " + grupo.Grupo;
            r.Subtitulo = "Año " + grupo.Anio + " | " + MonitoreoUI.Trimestre(trimestre) + " | " + Resumen(r.Resumen);
            return r;
        }

        // Reporte de una materia: una fila por estudiante, una columna por trimestre.
        public static ReporteMonitoreo PorMateria(
            GrupoMonitoreoItem grupo, IEnumerable<NotaFinalFila> filas,
            UmbralesRendimiento umbrales, string materia, int trimestre)
        {
            var estudiantes = MonitoreoCalculo.ArmarRendimiento(filas, umbrales, trimestre, materia);

            var r = Base("Materia", grupo, umbrales, trimestre, materia);
            r.Resumen = MonitoreoCalculo.Resumir(materia, estudiantes, grupo.IdGrupo);

            r.Columnas.AddRange(new[] { "Estudiante", "Identificación",
                                        "I Trimestre", "II Trimestre", "III Trimestre",
                                        "Promedio", "Estado" });

            foreach (var e in estudiantes)
            {
                var n = MonitoreoCalculo.PorAsignatura(e.Detalle).FirstOrDefault();
                r.Filas.Add(new[]
                {
                    e.Estudiante, e.Identificacion,
                    MonitoreoUI.Fmt(n == null ? null : n.T1),
                    MonitoreoUI.Fmt(n == null ? null : n.T2),
                    MonitoreoUI.Fmt(n == null ? null : n.T3),
                    MonitoreoUI.Fmt(e.Promedio),
                    e.Estado
                });
            }

            r.Titulo = "Rendimiento de " + materia + " - grupo " + grupo.Grupo;
            r.Subtitulo = "Año " + grupo.Anio + " | " + MonitoreoUI.Trimestre(trimestre) + " | " + Resumen(r.Resumen);
            return r;
        }

        private static ReporteMonitoreo Base(
            string tipo, GrupoMonitoreoItem grupo, UmbralesRendimiento umbrales, int trimestre, string materia)
        {
            return new ReporteMonitoreo
            {
                Tipo = tipo,
                Grupo = grupo.Grupo,
                Docente = grupo.Docente,
                Materia = materia ?? "",
                Anio = grupo.Anio,
                Trimestre = trimestre,
                Generado = DateTime.Now,
                Umbrales = umbrales
            };
        }
    }
}