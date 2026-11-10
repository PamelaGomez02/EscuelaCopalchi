using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    public static class FichaPdf
    {
        private const double M = 40;                       // margen
        private const double W = PdfDocumento.Ancho - 80;  // ancho útil

        private static string N(decimal? v)
        {
            return v.HasValue ? v.Value.ToString("0.#", CultureInfo.InvariantCulture) : "-";
        }

        public static string NombreArchivo(string nombre)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in nombre ?? "")
            {
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            return "Ficha_" + (sb.Length == 0 ? "Estudiante" : sb.ToString()) + ".pdf";
        }

        public static byte[] Generar(FichaDatos d, List<FichaNota> notas, FichaAsistencia asis, List<FichaExterna> externas, string generadoPor)
        {
            var pdf = new PdfDocumento();
            double y = 0;

            // Salta de página si no hay espacio suficiente
            Action<double> espacio = alto =>
            {
                if (y + alto > PdfDocumento.Alto - 60)
                {
                    pdf.NuevaPagina();
                    y = 50;
                }
            };

            Action<string> seccion = titulo =>
            {
                espacio(40);
                y += 8;
                pdf.Texto(M, y + 11, titulo, 12, true, 19, 60, 85);
                pdf.Linea(M, y + 17, M + W, y + 17, 1, 89, 165, 216);
                y += 28;
            };

            // ---------- encabezado ----------
            pdf.Rect(0, 0, PdfDocumento.Ancho, 78, 19, 60, 85);
            pdf.Texto(M, 36, "Escuela Copalchí", 18, true, 255, 255, 255);
            pdf.Texto(M, 56, "Ficha académica del estudiante", 11, false, 220, 232, 242);
            pdf.TextoDerecha(M + W, 36, DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 10, false, 220, 232, 242);
            y = 96;

            // ---------- datos del estudiante ----------
            pdf.Texto(M, y + 12, d.NombreCompleto, 15, true, 19, 60, 85);
            y += 24;

            var datos = new List<string[]>
            {
                new[] { "Identificación", d.Identificacion },
                new[] { "Fecha de nacimiento", d.FechaNacimiento.HasValue ? d.FechaNacimiento.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "-" },
                new[] { "Estado", d.Activo ? "Activo" : "Inactivo" },
                new[] { "Matrícula", string.IsNullOrEmpty(d.Grupo) ? "Sin matrícula" : d.Grupo + " · " + d.Nivel + " · " + d.Anio + (string.IsNullOrEmpty(d.EstadoMatricula) ? "" : " (" + d.EstadoMatricula + ")") },
                new[] { "Docente", string.IsNullOrEmpty(d.Docente) ? "-" : d.Docente },
                new[] { "Encargado", string.IsNullOrEmpty(d.Encargado) ? "-" : d.Encargado },
                new[] { "Teléfono del encargado", string.IsNullOrEmpty(d.TelefonoEncargado) ? "-" : d.TelefonoEncargado },
                new[] { "Dirección", string.IsNullOrEmpty(d.Direccion) ? "-" : d.Direccion }
            };

            double colEt = 125, colVal = W - colEt - 10;
            pdf.Rect(M, y, W, 4, 244, 247, 250);

            foreach (var par in datos)
            {
                var lineas = pdf.Ajustar(par[1], colVal, 10);
                double alto = Math.Max(1, lineas.Count) * 14 + 4;
                espacio(alto);
                pdf.Rect(M, y, W, alto, 244, 247, 250);
                pdf.Texto(M + 8, y + 13, par[0], 9, true, 83, 97, 109);
                for (int i = 0; i < lineas.Count; i++)
                    pdf.Texto(M + colEt + 8, y + 13 + i * 14, lineas[i], 10);
                y += alto;
                pdf.Linea(M, y, M + W, y, 0.4, 227, 232, 238);
            }
            y += 6;

            // ---------- rendimiento ----------
            seccion("Rendimiento académico");

            if (notas.Count == 0)
            {
                pdf.Texto(M, y + 10, "Aún no hay calificaciones registradas.", 10, false, 108, 117, 125);
                y += 20;
            }

            double[] cols = { 195, 80, 80, 80, 80 };
            string[] enc = { "Asignatura", "Trimestre 1", "Trimestre 2", "Trimestre 3", "Promedio" };

            foreach (var gr in notas.GroupBy(n => n.IdGrupo).OrderByDescending(g => g.First().Anio))
            {
                var primera = gr.First();
                espacio(70);
                pdf.Texto(M, y + 10, primera.Grupo + " - " + primera.Anio, 10.5, true, 56, 111, 164);
                y += 16;

                pdf.Rect(M, y, W, 20, 19, 60, 85);
                double x = M;
                for (int i = 0; i < enc.Length; i++)
                {
                    if (i == 0) pdf.Texto(x + 8, y + 14, enc[i], 9, true, 255, 255, 255);
                    else pdf.TextoCentro(x + cols[i] / 2, y + 14, enc[i], 9, true, 255, 255, 255);
                    x += cols[i];
                }
                y += 20;

                bool alterna = false;
                var todas = new List<decimal>();

                foreach (var asig in gr.GroupBy(n => n.Asignatura).OrderBy(a => a.Key))
                {
                    espacio(22);
                    if (alterna) pdf.Rect(M, y, W, 20, 244, 247, 250);
                    alterna = !alterna;

                    pdf.Texto(M + 8, y + 14, pdf.Recortar(asig.Key, cols[0] - 12, 10), 10);

                    var finales = new List<decimal>();
                    x = M + cols[0];
                    for (int t = 1; t <= 3; t++)
                    {
                        var f = asig.FirstOrDefault(n => n.Trimestre == t);
                        if (f != null)
                        {
                            finales.Add(f.Nota);
                            todas.Add(f.Nota);
                            if (f.Nota < 70) pdf.TextoCentro(x + cols[t] / 2, y + 14, N(f.Nota), 10, false, 176, 32, 48);
                            else pdf.TextoCentro(x + cols[t] / 2, y + 14, N(f.Nota), 10);
                        }
                        else pdf.TextoCentro(x + cols[t] / 2, y + 14, "-", 10, false, 150, 150, 150);
                        x += cols[t];
                    }

                    if (finales.Count > 0)
                    {
                        decimal p = Math.Round(finales.Average(), 1);
                        pdf.TextoCentro(x + cols[4] / 2, y + 14, N(p), 10, true, p < 70 ? 176 : 15, p < 70 ? 32 : 81, p < 70 ? 48 : 50);
                    }
                    else pdf.TextoCentro(x + cols[4] / 2, y + 14, "-", 10, false, 150, 150, 150);

                    y += 20;
                }

                espacio(24);
                pdf.Linea(M, y, M + W, y, 0.8, 19, 60, 85);
                pdf.Texto(M + 8, y + 15, "Promedio general", 10, true, 19, 60, 85);
                if (todas.Count > 0)
                {
                    decimal pg = Math.Round(todas.Average(), 1);
                    pdf.TextoCentro(M + W - cols[4] / 2, y + 15, N(pg), 11, true, pg < 70 ? 176 : 15, pg < 70 ? 32 : 81, pg < 70 ? 48 : 50);
                }
                y += 30;
            }

            // ---------- asistencia ----------
            seccion("Asistencia");

            if (asis.Clases == 0)
            {
                pdf.Texto(M, y + 10, "Aún no hay asistencia registrada.", 10, false, 108, 117, 125);
                y += 20;
            }
            else
            {
                string[] et = { "Clases", "Presentes", "Tardanzas", "Justificadas", "Ausentes", "Asistencia" };
                string[] val = { asis.Clases.ToString(), asis.Presentes.ToString(), asis.Tardanzas.ToString(), asis.Justificados.ToString(), asis.Ausentes.ToString(), N(asis.Porcentaje) + "%" };
                double cw = W / 6;
                espacio(50);
                pdf.Rect(M, y, W, 44, 244, 247, 250);
                for (int i = 0; i < 6; i++)
                {
                    pdf.TextoCentro(M + cw * i + cw / 2, y + 16, et[i], 8.5, false, 83, 97, 109);
                    bool bajo = i == 5 && asis.Porcentaje.HasValue && asis.Porcentaje.Value < 80;
                    pdf.TextoCentro(M + cw * i + cw / 2, y + 34, val[i], 13, true, bajo ? 176 : 19, bajo ? 32 : 60, bajo ? 48 : 85);
                }
                y += 50;
                pdf.Texto(M, y + 8, "La asistencia descuenta solo las ausencias injustificadas.", 8, false, 108, 117, 125);
                y += 14;
            }

            // ---------- otras escuelas ----------
            if (externas.Count > 0)
            {
                seccion("Calificaciones de escuelas anteriores");

                double[] ce = { 175, 130, 80, 50, 80 };
                string[] he = { "Escuela", "Asignatura", "Periodo", "Año", "Nota" };

                espacio(40);
                pdf.Rect(M, y, W, 20, 19, 60, 85);
                double x = M;
                for (int i = 0; i < he.Length; i++)
                {
                    pdf.Texto(x + 8, y + 14, he[i], 9, true, 255, 255, 255);
                    x += ce[i];
                }
                y += 20;

                bool alterna = false;
                foreach (var e in externas)
                {
                    espacio(22);
                    if (alterna) pdf.Rect(M, y, W, 20, 244, 247, 250);
                    alterna = !alterna;

                    x = M;
                    string[] v = { e.Escuela, e.Asignatura, e.Periodo, e.Anio.ToString(), N(e.Nota) };
                    for (int i = 0; i < v.Length; i++)
                    {
                        pdf.Texto(x + 8, y + 14, pdf.Recortar(v[i], ce[i] - 12, 10), 10);
                        x += ce[i];
                    }
                    y += 20;
                }
            }

            string pie = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                       + (string.IsNullOrEmpty(generadoPor) ? "" : " por " + generadoPor);

            return pdf.Guardar(pie);
        }
    }
}
