using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Genera un PDF sencillo (tabla con título) sin librerías externas.
    /// Página carta horizontal, fuente Helvetica, con paginación automática.
    /// </summary>
    public static class PdfHelper
    {
        private const float AnchoPagina = 792f;   // 11 in
        private const float AltoPagina = 612f;    // 8.5 in
        private const float Margen = 40f;
        private const float AltoFila = 18f;
        private const float TamanoTexto = 8.5f;

        private static readonly Encoding Windows1252 = Encoding.GetEncoding(1252);

        public static byte[] GenerarTabla(string titulo, string subtitulo, IList<string> columnas, IList<string[]> filas)
        {
            float anchoUtil = AnchoPagina - 2 * Margen;
            float[] anchos = CalcularAnchos(columnas, filas, anchoUtil);

            // Divide las filas en páginas
            var paginas = new List<string>();
            int indice = 0;
            int numeroPagina = 1;
            int totalPaginas = Math.Max(1, (int)Math.Ceiling((filas.Count) / (double)FilasPorPagina(true)));

            do
            {
                bool primera = numeroPagina == 1;
                int capacidad = FilasPorPagina(primera);
                var bloque = filas.Skip(indice).Take(capacidad).ToList();
                indice += bloque.Count;

                paginas.Add(ContenidoPagina(titulo, subtitulo, columnas, bloque, anchos, primera, numeroPagina));
                numeroPagina++;
            }
            while (indice < filas.Count);

            totalPaginas = paginas.Count;
            for (int i = 0; i < paginas.Count; i++)
                paginas[i] = paginas[i].Replace("{TOTAL_PAGINAS}", totalPaginas.ToString(CultureInfo.InvariantCulture));

            return ConstruirPdf(paginas);
        }

        private static int FilasPorPagina(bool primera)
        {
            float inicio = AltoPagina - Margen - (primera ? 70f : 20f);
            return Math.Max(1, (int)((inicio - Margen - 30f) / AltoFila) - 1);
        }

        private static float[] CalcularAnchos(IList<string> columnas, IList<string[]> filas, float anchoUtil)
        {
            var pesos = new float[columnas.Count];

            for (int i = 0; i < columnas.Count; i++)
            {
                int largo = columnas[i].Length;
                foreach (string[] f in filas.Take(200))
                    if (i < f.Length && f[i] != null)
                        largo = Math.Max(largo, f[i].Length);

                pesos[i] = Math.Max(6, Math.Min(largo, 45));
            }

            float total = pesos.Sum();
            return pesos.Select(p => anchoUtil * p / total).ToArray();
        }

        private static string ContenidoPagina(string titulo, string subtitulo, IList<string> columnas, List<string[]> filas,
                                              float[] anchos, bool primera, int numeroPagina)
        {
            var sb = new StringBuilder();
            float y = AltoPagina - Margen;

            if (primera)
            {
                // Franja de color y título
                sb.Append("0.075 0.235 0.333 rg\n");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} 4 re f\n", Margen, y + 6, AnchoPagina - 2 * Margen);
                Texto(sb, "F2", 15, Margen, y - 14, titulo, "0.075 0.235 0.333");
                Texto(sb, "F1", 9, Margen, y - 30, "Escuela Copalchi · Aula Virtual", "0.42 0.46 0.49");
                if (!string.IsNullOrEmpty(subtitulo))
                    Texto(sb, "F1", 9, Margen, y - 43, subtitulo, "0.42 0.46 0.49");
                y -= 70;
            }
            else
            {
                Texto(sb, "F2", 10, Margen, y - 8, titulo + " (continuación)", "0.075 0.235 0.333");
                y -= 20;
            }

            // Encabezado de la tabla
            sb.Append("0.075 0.235 0.333 rg\n");
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} re f\n", Margen, y - AltoFila, AnchoPagina - 2 * Margen, AltoFila);

            float x = Margen;
            for (int i = 0; i < columnas.Count; i++)
            {
                Texto(sb, "F2", TamanoTexto, x + 4, y - 12.5f, Recortar(columnas[i], anchos[i] - 8, true), "1 1 1");
                x += anchos[i];
            }
            y -= AltoFila;

            // Filas
            for (int f = 0; f < filas.Count; f++)
            {
                if (f % 2 == 1)
                {
                    sb.Append("0.953 0.973 0.992 rg\n");
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} re f\n", Margen, y - AltoFila, AnchoPagina - 2 * Margen, AltoFila);
                }

                x = Margen;
                for (int i = 0; i < columnas.Count; i++)
                {
                    string valor = i < filas[f].Length ? filas[f][i] : "";
                    Texto(sb, "F1", TamanoTexto, x + 4, y - 12.5f, Recortar(valor, anchos[i] - 8, false), "0.2 0.24 0.27");
                    x += anchos[i];
                }

                // Línea inferior
                sb.Append("0.89 0.91 0.93 RG 0.5 w\n");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} m {2} {1} l S\n", Margen, y - AltoFila, AnchoPagina - Margen);
                y -= AltoFila;
            }

            if (filas.Count == 0 && primera)
                Texto(sb, "F1", 10, Margen, y - 16, "No hay información para los filtros seleccionados.", "0.42 0.46 0.49");

            // Pie
            Texto(sb, "F1", 8, Margen, 22, "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), "0.55 0.58 0.6");
            Texto(sb, "F1", 8, AnchoPagina - Margen - 60, 22, "Página " + numeroPagina + " de {TOTAL_PAGINAS}", "0.55 0.58 0.6");

            return sb.ToString();
        }

        private static void Texto(StringBuilder sb, string fuente, float tamano, float x, float y, string texto, string color)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "BT {0} rg /{1} {2} Tf {3} {4} Td ({5}) Tj ET\n",
                            color, fuente, tamano, x, y, Escapar(texto));
        }

        /// <summary>Recorta el texto para que quepa en el ancho (estimación por carácter).</summary>
        private static string Recortar(string texto, float ancho, bool negrita)
        {
            texto = (texto ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            float porCaracter = TamanoTexto * (negrita ? 0.56f : 0.52f);
            int maximo = Math.Max(3, (int)(ancho / porCaracter));

            return texto.Length <= maximo ? texto : texto.Substring(0, maximo - 1) + "…";
        }

        private static string Escapar(string texto)
        {
            var sb = new StringBuilder();
            foreach (char c in texto ?? string.Empty)
            {
                if (c == '\\' || c == '(' || c == ')') sb.Append('\\');
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static byte[] ConstruirPdf(List<string> paginas)
        {
            // Objetos: 1 catálogo, 2 páginas, 3 Helvetica, 4 Helvetica-Bold, luego (página, contenido) por cada página
            var objetos = new List<byte[]>();
            int totalObjetos = 4 + paginas.Count * 2;

            var kids = string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => (5 + i * 2) + " 0 R"));

            objetos.Add(Ascii("<< /Type /Catalog /Pages 2 0 R >>"));
            objetos.Add(Ascii("<< /Type /Pages /Kids [" + kids + "] /Count " + paginas.Count + " >>"));
            objetos.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
            objetos.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));

            for (int i = 0; i < paginas.Count; i++)
            {
                int idContenido = 6 + i * 2;
                objetos.Add(Ascii(string.Format(CultureInfo.InvariantCulture,
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0} {1}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {2} 0 R >>",
                    AnchoPagina, AltoPagina, idContenido)));

                byte[] contenido = Windows1252.GetBytes(paginas[i]);
                using (var ms = new MemoryStream())
                {
                    byte[] cabecera = Ascii("<< /Length " + contenido.Length + " >>\nstream\n");
                    byte[] pie = Ascii("\nendstream");
                    ms.Write(cabecera, 0, cabecera.Length);
                    ms.Write(contenido, 0, contenido.Length);
                    ms.Write(pie, 0, pie.Length);
                    objetos.Add(ms.ToArray());
                }
            }

            using (var pdf = new MemoryStream())
            {
                var offsets = new long[totalObjetos + 1];
                Escribir(pdf, Ascii("%PDF-1.4\n%âãÏÓ\n"));

                for (int i = 0; i < objetos.Count; i++)
                {
                    offsets[i + 1] = pdf.Position;
                    Escribir(pdf, Ascii((i + 1) + " 0 obj\n"));
                    Escribir(pdf, objetos[i]);
                    Escribir(pdf, Ascii("\nendobj\n"));
                }

                long inicioXref = pdf.Position;
                var xref = new StringBuilder();
                xref.Append("xref\n0 " + (totalObjetos + 1) + "\n");
                xref.Append("0000000000 65535 f \n");
                for (int i = 1; i <= totalObjetos; i++)
                    xref.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");

                xref.Append("trailer\n<< /Size " + (totalObjetos + 1) + " /Root 1 0 R >>\nstartxref\n" + inicioXref + "\n%%EOF");
                Escribir(pdf, Ascii(xref.ToString()));

                return pdf.ToArray();
            }
        }

        private static byte[] Ascii(string texto)
        {
            return Windows1252.GetBytes(texto);
        }

        private static void Escribir(Stream s, byte[] datos)
        {
            s.Write(datos, 0, datos.Length);
        }
    }
}