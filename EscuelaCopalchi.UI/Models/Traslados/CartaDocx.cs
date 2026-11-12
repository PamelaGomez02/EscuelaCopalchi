using EscuelaCopalchi.UI.Models.Reportes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace EscuelaCopalchi.UI.Models.Traslados
{
    // Genera la carta oficial de traslado como .docx (sin paquetes externos; usa System.IO.Compression)
    public static class CartaDocx
    {
        private static readonly string[] Meses =
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };

        public static string NombreArchivo(string estudiante)
        {
            var sb = new StringBuilder();
            foreach (char c in estudiante ?? "")
            {
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            return "Carta_Traslado_" + (sb.Length == 0 ? "Estudiante" : sb.ToString()) + ".docx";
        }

        private static string Esc(string s) { return SecurityElement.Escape(s ?? ""); }

        private static string N(decimal? v)
        {
            return v.HasValue ? v.Value.ToString("0.#", CultureInfo.InvariantCulture) : "-";
        }

        // ---------- bloques de Word ----------
        private static string Run(string texto, bool negrita = false, int tamano = 22, string color = null)
        {
            return "<w:r><w:rPr>" + (negrita ? "<w:b/>" : "") + (color != null ? "<w:color w:val=\"" + color + "\"/>" : "")
                 + "<w:sz w:val=\"" + tamano + "\"/><w:szCs w:val=\"" + tamano + "\"/></w:rPr>"
                 + "<w:t xml:space=\"preserve\">" + Esc(texto) + "</w:t></w:r>";
        }

        private static string Parrafo(string runs, string alineacion = "left", int despues = 160, int antes = 0)
        {
            return "<w:p><w:pPr><w:spacing w:before=\"" + antes + "\" w:after=\"" + despues + "\" w:line=\"276\" w:lineRule=\"auto\"/>"
                 + "<w:jc w:val=\"" + alineacion + "\"/></w:pPr>" + runs + "</w:p>";
        }

        private static string Celda(string texto, int ancho, bool negrita = false, string relleno = null,
                                    string color = null, string alineacion = "left", int span = 1)
        {
            return "<w:tc><w:tcPr><w:tcW w:w=\"" + ancho + "\" w:type=\"dxa\"/>"
                 + (span > 1 ? "<w:gridSpan w:val=\"" + span + "\"/>" : "")
                 + (relleno != null ? "<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"" + relleno + "\"/>" : "")
                 + "<w:vAlign w:val=\"center\"/></w:tcPr>"
                 + Parrafo(Run(texto, negrita, 19, color), alineacion, 40, 40) + "</w:tc>";
        }

        private static string Fila(params string[] celdas)
        {
            return "<w:tr><w:trPr><w:cantSplit/></w:trPr>" + string.Concat(celdas) + "</w:tr>";
        }

        public static byte[] Generar(FichaDatos d, List<FichaNota> notas, FichaAsistencia asis,
                                     string destino, string motivo, string lugar, string director, DateTime fecha)
        {
            var b = new StringBuilder();

            // ----- membrete -----
            b.Append(Parrafo(Run("ESCUELA COPALCHÍ", true, 32, "133C55"), "center", 40));
            b.Append(Parrafo(Run("Dirección", false, 22, "6C757D"), "center", 60));
            b.Append("<w:p><w:pPr><w:pBdr><w:bottom w:val=\"single\" w:sz=\"8\" w:space=\"1\" w:color=\"59A5D8\"/></w:pBdr>"
                   + "<w:spacing w:after=\"240\"/></w:pPr></w:p>");

            // ----- fecha y destinatario -----
            string fechaTxt = (string.IsNullOrWhiteSpace(lugar) ? "" : lugar.Trim() + ", ")
                            + fecha.Day + " de " + Meses[fecha.Month - 1] + " de " + fecha.Year;
            b.Append(Parrafo(Run(fechaTxt), "right", 240));

            b.Append(Parrafo(Run("CARTA OFICIAL DE TRASLADO", true, 28, "133C55"), "center", 240));

            b.Append(Parrafo(Run("Señores(as)", false), "left", 0));
            b.Append(Parrafo(Run(destino, true), "left", 0));
            b.Append(Parrafo(Run("Presente"), "left", 240));

            // ----- cuerpo -----
            string matricula = string.IsNullOrEmpty(d.Grupo) ? "" :
                ", quien cursa el grupo " + d.Grupo + (string.IsNullOrEmpty(d.Nivel) ? "" : " (" + d.Nivel + ")") + " en el año " + d.Anio;

            string cuerpo = "Por medio de la presente, la Dirección de la Escuela Copalchí hace constar que el(la) estudiante "
                          + d.NombreCompleto + ", identificación " + d.Identificacion + matricula
                          + ", ha estado matriculado(a) en este centro educativo. "
                          + (string.IsNullOrEmpty(d.Encargado) ? "A solicitud de su encargado(a), " : "A solicitud de su encargado(a) " + d.Encargado + ", ")
                          + "se emite la presente carta para formalizar su traslado a " + destino + ".";

            b.Append(Parrafo(Run(cuerpo), "both", 160));

            if (!string.IsNullOrWhiteSpace(motivo))
                b.Append(Parrafo(Run("Motivo del traslado: " + motivo.Trim()), "both", 160));

            b.Append(Parrafo(Run("Se adjunta el historial académico registrado en el sistema de la institución:"), "both", 160));

            // ----- historial académico -----
            b.Append(Parrafo(Run("Historial académico", true, 24, "133C55"), "left", 80, 120));

            int[] a = { 800, 1500, 3000, 1100, 1100, 1100, 1038 }; // suma 9638
            string[] enc = { "Año", "Grupo", "Asignatura", "Trim. 1", "Trim. 2", "Trim. 3", "Promedio" };

            if (notas.Count == 0)
            {
                b.Append(Parrafo(Run("No hay calificaciones registradas en el sistema para este estudiante.", false, 20, "6C757D"), "left", 160));
            }
            else
            {
                var t = new StringBuilder();
                t.Append("<w:tbl><w:tblPr><w:tblW w:w=\"9638\" w:type=\"dxa\"/><w:tblBorders>"
                       + "<w:top w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/><w:left w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/>"
                       + "<w:bottom w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/><w:right w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/>"
                       + "<w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/><w:insideV w:val=\"single\" w:sz=\"4\" w:color=\"BFC7D0\"/>"
                       + "</w:tblBorders><w:tblLayout w:type=\"fixed\"/></w:tblPr><w:tblGrid>");
                foreach (int x in a) t.Append("<w:gridCol w:w=\"" + x + "\"/>");
                t.Append("</w:tblGrid>");

                var cabecera = new List<string>();
                for (int i = 0; i < enc.Length; i++)
                    cabecera.Add(Celda(enc[i], a[i], true, "133C55", "FFFFFF", i >= 3 ? "center" : "left"));
                t.Append("<w:tr><w:trPr><w:cantSplit/><w:tblHeader/></w:trPr>" + string.Concat(cabecera) + "</w:tr>");

                foreach (var gr in notas.GroupBy(n => n.IdGrupo).OrderBy(g => g.First().Anio))
                {
                    var p = gr.First();
                    var todas = new List<decimal>();

                    foreach (var asig in gr.GroupBy(n => n.Asignatura).OrderBy(x => x.Key))
                    {
                        var finales = new List<decimal>();
                        var celdas = new List<string>
                        {
                            Celda(p.Anio.ToString(), a[0]), Celda(p.Grupo, a[1]), Celda(asig.Key, a[2])
                        };

                        for (int tri = 1; tri <= 3; tri++)
                        {
                            var f = asig.FirstOrDefault(n => n.Trimestre == tri);
                            if (f != null) { finales.Add(f.Nota); todas.Add(f.Nota); }
                            celdas.Add(Celda(f != null ? N(f.Nota) : "-", a[2 + tri], false, null,
                                             f != null && f.Nota < 70 ? "B02030" : null, "center"));
                        }

                        decimal? prom = finales.Count > 0 ? Math.Round(finales.Average(), 1) : (decimal?)null;
                        celdas.Add(Celda(N(prom), a[6], true, null, null, "center"));
                        t.Append(Fila(celdas.ToArray()));
                    }

                    decimal? pg = todas.Count > 0 ? Math.Round(todas.Average(), 1) : (decimal?)null;
                    t.Append(Fila(
                        Celda("Promedio general " + p.Anio + " - " + p.Grupo, a[0] + a[1] + a[2] + a[3] + a[4] + a[5], true, "EEF3F8", null, "left", 6),
                        Celda(N(pg), a[6], true, "EEF3F8", null, "center")));
                }

                t.Append("</w:tbl>");
                b.Append(t);
                b.Append(Parrafo("", "left", 120));
            }

            if (asis != null && asis.Clases > 0)
            {
                b.Append(Parrafo(Run("Asistencia registrada: " + asis.Clases + " lecciones, " + asis.Ausentes
                    + " ausencias injustificadas (" + N(asis.Porcentaje) + "% de asistencia)."), "both", 200));
            }

            b.Append(Parrafo(Run("Se extiende la presente a solicitud de la parte interesada, para los fines que estime convenientes."), "both", 560));

            // ----- firma -----
            b.Append(Parrafo(Run("______________________________"), "center", 0, 360));
            b.Append(Parrafo(Run(string.IsNullOrWhiteSpace(director) ? "Director(a)" : director, true), "center", 0));
            b.Append(Parrafo(Run("Director(a) - Escuela Copalchí"), "center", 0));

            string documento = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>"
                + b
                + "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/>"
                + "<w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/></w:sectPr>"
                + "</w:body></w:document>";

            string estilos = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:docDefaults><w:rPrDefault><w:rPr>"
                + "<w:rFonts w:ascii=\"Calibri\" w:hAnsi=\"Calibri\" w:cs=\"Calibri\" w:eastAsia=\"Calibri\"/>"
                + "<w:sz w:val=\"22\"/><w:szCs w:val=\"22\"/><w:lang w:val=\"es-CR\"/></w:rPr></w:rPrDefault></w:docDefaults>"
                + "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style></w:styles>";

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    Escribir(zip, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
                        + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
                        + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
                        + "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>"
                        + "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>"
                        + "</Types>");

                    Escribir(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                        + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>"
                        + "</Relationships>");

                    Escribir(zip, "word/_rels/document.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                        + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>"
                        + "</Relationships>");

                    Escribir(zip, "word/document.xml", documento);
                    Escribir(zip, "word/styles.xml", estilos);
                }
                return ms.ToArray();
            }
        }

        private static void Escribir(ZipArchive zip, string ruta, string contenido)
        {
            var e = zip.CreateEntry(ruta);
            using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false)))
            {
                w.Write(contenido);
            }
        }
    }
}
