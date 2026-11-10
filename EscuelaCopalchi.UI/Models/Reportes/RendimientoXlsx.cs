using System.Collections.Generic;
using System.Linq;
using global::EscuelaCopalchi.UI.Models.GestionNotas;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    // Exporta el reporte de rendimiento y asistencia a Excel (usa el escritor .xlsx de Gestión de notas)
    public static class RendimientoXlsx
    {
        private const int Normal = 0, Negrita = 1, Encabezado = 2, Titulo = 3;

        public static byte[] Generar(string titulo, List<EstudianteRendimiento> estudiantes, List<AsignaturaPromedio> asignaturas)
        {
            var libro = new XlsxLibro();

            // ----- hoja 1: estudiantes -----
            var h = libro.NuevaHoja("Rendimiento");
            h.Anchos = new double[] { 32, 16, 18, 12, 12, 12, 12, 12, 12, 12, 30 };

            h.Fila(Titulo, "Reporte de rendimiento y asistencia - " + titulo);
            h.Fila(Normal, "Alertas: promedio menor a 70, o asistencia menor a 80% (solo ausencias injustificadas).");
            h.Fila(Normal);
            h.Fila(Encabezado, "Estudiante", "Identificación", "Grupo", "Promedio", "Clases", "Presentes",
                   "Tardanzas", "Justificadas", "Ausentes", "% Asistencia", "Seguimiento");

            foreach (var e in estudiantes)
            {
                var alertas = new List<string>();
                if (e.BajoRendimiento) alertas.Add("Bajo rendimiento");
                if (e.BajaAsistencia) alertas.Add("Asistencia baja");

                h.Fila(Normal, e.Estudiante, e.Identificacion, e.Grupo,
                       e.Promedio.HasValue ? (object)e.Promedio.Value : null,
                       e.Clases, e.Presentes, e.Tardanzas, e.Justificados, e.Ausentes,
                       e.PctAsistencia.HasValue ? (object)e.PctAsistencia.Value : null,
                       alertas.Count == 0 ? "Sin alertas" : string.Join("; ", alertas));
            }

            h.Fila(Normal);
            var conNota = estudiantes.Where(e => e.Promedio.HasValue).ToList();
            h.Fila(Negrita, "Estudiantes", estudiantes.Count);
            h.Fila(Negrita, "Promedio general");
            h.Ultima().Poner(1, conNota.Count == 0 ? (object)"-" : System.Math.Round(conNota.Average(e => e.Promedio.Value), 1));
            h.Fila(Negrita, "Requieren seguimiento", estudiantes.Count(e => e.EnRiesgo));

            // ----- hoja 2: asignaturas -----
            var a = libro.NuevaHoja("Por asignatura");
            a.Anchos = new double[] { 34, 14, 14 };
            a.Fila(Titulo, "Promedio por asignatura - " + titulo);
            a.Fila(Normal);
            a.Fila(Encabezado, "Asignatura", "Promedio", "Estudiantes");
            foreach (var x in asignaturas) a.Fila(Normal, x.Asignatura, x.Promedio, x.Estudiantes);
            if (asignaturas.Count == 0) a.Fila(Normal, "Aún no hay notas registradas.");

            return libro.Guardar();
        }
    }
}
