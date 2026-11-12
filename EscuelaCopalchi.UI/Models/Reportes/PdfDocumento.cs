using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    // Escritor mínimo de PDF (texto, rectángulos y líneas; fuente Helvetica). Sin paquetes externos.
    public class PdfDocumento
    {
        public const double Ancho = 595.28;
        public const double Alto = 841.89;

        // Anchos de Helvetica / Helvetica-Bold (códigos 32..255, unidades por 1000)
        private static readonly int[] WN = { 278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 333, 333, 333, 556, 537, 278, 333, 333, 365, 556, 834, 834, 834, 611, 667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278, 556, 556, 556, 556, 556, 556, 556, 584, 611, 556, 556, 556, 556, 500, 556, 500 };
        private static readonly int[] WB = { 278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 333, 333, 333, 611, 556, 278, 333, 333, 365, 556, 834, 834, 834, 611, 722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278, 611, 611, 611, 611, 611, 611, 611, 584, 611, 611, 611, 611, 611, 556, 611, 556 };

        private readonly List<StringBuilder> paginas = new List<StringBuilder>();
        private StringBuilder actual;

        public int NumPaginas { get { return paginas.Count; } }

        public PdfDocumento() { NuevaPagina(); }

        public void NuevaPagina()
        {
            actual = new StringBuilder();
            paginas.Add(actual);
        }

        private static string F(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        // Deja solo caracteres representables (Latin-1) y escapa ( ) \
        private static string Limpiar(string t)
        {
            var sb = new StringBuilder();
            foreach (char c in t ?? "")
            {
                char x = c;
                if (c == '–' || c == '—' || c == '•') x = '-';
                else if (c == '‘' || c == '’') x = '\'';
                else if (c == '“' || c == '”') x = '"';
                else if (c < 32 || (c > 126 && c < 160) || c > 255) x = (c == '\t' || c == '\n' || c == '\r') ? ' ' : '?';
                sb.Append(x);
            }
            return sb.ToString();
        }

        public double Medir(string texto, double tamano, bool negrita)
        {
            int[] w = negrita ? WB : WN;
            double total = 0;
            foreach (char c in Limpiar(texto)) total += w[c - 32];
            return total * tamano / 1000.0;
        }

        // y = línea base del texto, medida desde el borde superior de la página
        public void Texto(double x, double y, string texto, double tamano, bool negrita = false, int r = 0, int g = 0, int b = 0)
        {
            string t = Limpiar(texto).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            actual.Append("BT /" + (negrita ? "F2" : "F1") + " " + F(tamano) + " Tf "
                + F(r / 255.0) + " " + F(g / 255.0) + " " + F(b / 255.0) + " rg "
                + F(x) + " " + F(Alto - y) + " Td (" + t + ") Tj ET\n");
        }

        public void TextoDerecha(double xDerecha, double y, string texto, double tamano, bool negrita = false, int r = 0, int g = 0, int b = 0)
        {
            Texto(xDerecha - Medir(texto, tamano, negrita), y, texto, tamano, negrita, r, g, b);
        }

        public void TextoCentro(double xCentro, double y, string texto, double tamano, bool negrita = false, int r = 0, int g = 0, int b = 0)
        {
            Texto(xCentro - Medir(texto, tamano, negrita) / 2, y, texto, tamano, negrita, r, g, b);
        }

        // y = borde superior del rectángulo
        public void Rect(double x, double y, double ancho, double alto, int r, int g, int b)
        {
            actual.Append(F(r / 255.0) + " " + F(g / 255.0) + " " + F(b / 255.0) + " rg "
                + F(x) + " " + F(Alto - y - alto) + " " + F(ancho) + " " + F(alto) + " re f\n");
        }

        public void Linea(double x1, double y1, double x2, double y2, double grosor, int r, int g, int b)
        {
            actual.Append(F(grosor) + " w " + F(r / 255.0) + " " + F(g / 255.0) + " " + F(b / 255.0) + " RG "
                + F(x1) + " " + F(Alto - y1) + " m " + F(x2) + " " + F(Alto - y2) + " l S\n");
        }

        // Parte el texto en líneas que caben en el ancho indicado
        public List<string> Ajustar(string texto, double ancho, double tamano, bool negrita = false)
        {
            var lineas = new List<string>();
            string linea = "";
            foreach (string palabra in (texto ?? "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string prueba = linea.Length == 0 ? palabra : linea + " " + palabra;
                if (Medir(prueba, tamano, negrita) <= ancho || linea.Length == 0)
                    linea = prueba;
                else
                {
                    lineas.Add(linea);
                    linea = palabra;
                }
            }
            if (linea.Length > 0) lineas.Add(linea);
            if (lineas.Count == 0) lineas.Add("");
            return lineas;
        }

        // Recorta con "..." si no cabe
        public string Recortar(string texto, double ancho, double tamano, bool negrita = false)
        {
            if (Medir(texto, tamano, negrita) <= ancho) return texto;
            string t = texto ?? "";
            while (t.Length > 0 && Medir(t + "...", tamano, negrita) > ancho) t = t.Substring(0, t.Length - 1);
            return t + "...";
        }

        // piePagina: texto fijo a la izquierda; a la derecha se agrega "Página i de n"
        public byte[] Guardar(string piePagina)
        {
            int n = paginas.Count;
            for (int i = 0; i < n; i++)
            {
                var antes = actual;
                actual = paginas[i];
                Linea(40, Alto - 38, Ancho - 40, Alto - 38, 0.5, 200, 205, 212);
                Texto(40, Alto - 25, piePagina, 8, false, 108, 117, 125);
                TextoDerecha(Ancho - 40, Alto - 25, "Página " + (i + 1) + " de " + n, 8, false, 108, 117, 125);
                actual = antes;
            }

            var enc = Encoding.GetEncoding("iso-8859-1");
            var ms = new MemoryStream();
            var offsets = new List<long>();

            Action<string> escribir = s => { byte[] b = enc.GetBytes(s); ms.Write(b, 0, b.Length); };
            Action<int, string> objeto = (num, cuerpo) =>
            {
                while (offsets.Count < num) offsets.Add(0);
                offsets[num - 1] = ms.Position;
                escribir(num + " 0 obj\n" + cuerpo + "\nendobj\n");
            };

            escribir("%PDF-1.4\n");

            // 1 catálogo, 2 páginas, 3 y 4 fuentes, luego (página, contenido) por página
            objeto(1, "<< /Type /Catalog /Pages 2 0 R >>");

            var kids = new StringBuilder();
            for (int i = 0; i < n; i++) kids.Append((5 + i * 2) + " 0 R ");
            objeto(2, "<< /Type /Pages /Kids [ " + kids + "] /Count " + n + " >>");
            objeto(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            objeto(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

            for (int i = 0; i < n; i++)
            {
                int pag = 5 + i * 2, cont = pag + 1;
                objeto(pag, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + F(Ancho) + " " + F(Alto) + "] "
                    + "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + cont + " 0 R >>");

                string contenido = paginas[i].ToString();
                objeto(cont, "<< /Length " + enc.GetByteCount(contenido) + " >>\nstream\n" + contenido + "endstream");
            }

            long xref = ms.Position;
            int total = offsets.Count + 1;
            escribir("xref\n0 " + total + "\n0000000000 65535 f \n");
            foreach (long o in offsets) escribir(o.ToString("0000000000") + " 00000 n \n");
            escribir("trailer\n<< /Size " + total + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
