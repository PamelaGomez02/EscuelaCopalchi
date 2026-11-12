using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace EscuelaCopalchi.UI.Models.MonitoreoNuevo
{
    // XLSX OpenXML y PDF sencillos, sin dependencias NuGet adicionales.
    public static class MonitoreoExportador
    {
        public static byte[] Excel(MonitoreoDashboardVM vm)
        {
            var rows = Filas(vm);
            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    Add(zip, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                    Add(zip, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                    Add(zip, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Rendimiento\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                    Add(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                    var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"/></sheetViews><sheetFormatPr defaultRowHeight=\"17\"/><cols><col min=\"1\" max=\"1\" width=\"23\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"35\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"25\" customWidth=\"1\"/><col min=\"4\" max=\"5\" width=\"17\" customWidth=\"1\"/></cols><sheetData>");
                    for (int i = 0; i < rows.Count; i++)
                    {
                        xml.Append("<row r=\"").Append(i + 1).Append("\">");
                        for (int j = 0; j < rows[i].Length; j++)
                        {
                            var v = rows[i][j] ?? "";
                            var cell = Column(j) + (i + 1);
                            xml.Append("<c r=\"").Append(cell).Append("\" t=\"inlineStr\"><is><t>").Append(Xml(v)).Append("</t></is></c>");
                        }
                        xml.Append("</row>");
                    }
                    xml.Append("</sheetData><autoFilter ref=\"A3:E").Append(rows.Count).Append("\"/></worksheet>");
                    Add(zip, "xl/worksheets/sheet1.xml", xml.ToString());
                }
                return ms.ToArray();
            }
        }
        private static string Column(int n) { return ((char)('A' + n)).ToString(); }
        private static string Xml(string s) { using (var sw = new StringWriter()) { using (var x = XmlWriter.Create(sw, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment })) { x.WriteString(s); } return sw.ToString(); } }
        private static void Add(ZipArchive z, string name, string value) { var e = z.CreateEntry(name); using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(value); }
        public static List<string[]> Filas(MonitoreoDashboardVM vm)
        {
            var r = new List<string[]> { new[] { "AULA VIRTUAL COPALCHI - MONITOREO ACADEMICO", "", "", "", "" }, new[] { "Anio: " + vm.Anio, "Trimestre: " + (vm.Trimestre == 0 ? "Todos" : vm.Trimestre.ToString()), "Grupo: " + (vm.IdGrupo == 0 ? "Todos" : vm.Grupos.Where(x => x.IdGrupo == vm.IdGrupo).Select(x => x.Nombre).FirstOrDefault()), "", "" }, new[] { "Estudiante", "Grupo", "Estado", "Promedio", "Materias" } };
            foreach (var e in vm.Estudiantes.OrderBy(x => x.Promedio).ThenBy(x => x.Nombre)) r.Add(new[] { e.Nombre, e.Grupo, e.Estado, e.Promedio.ToString("N2", CultureInfo.InvariantCulture), e.Materias.ToString() });
            return r;
        }
        // PDF A4 con la misma identidad visual del reporte institucional.
        public static byte[] Pdf(MonitoreoDashboardVM vm)
        {
            var registros = vm.Estudiantes.OrderBy(x => x.Nombre).ThenBy(x => x.Grupo).ToList();
            const int porPagina = 27;
            int paginas = Math.Max(1, (int)Math.Ceiling(registros.Count / (double)porPagina));
            var streams = new List<string>();
            string grupo = vm.IdGrupo == 0 ? "Todos los grupos" :
                (vm.Grupos.Where(x => x.IdGrupo == vm.IdGrupo).Select(x => x.Nombre).FirstOrDefault() ?? "Grupo seleccionado");
            string periodo = vm.Trimestre == 0 ? "Año completo" : (vm.Trimestre + "º trimestre");

            for (int pagina = 0; pagina < paginas; pagina++)
            {
                var b = new StringBuilder();
                // Franja superior azul petróleo, igual al reporte de referencia.
                Fill(b, 0.075, 0.235, 0.325);
                Rect(b, 0, 742, 595, 100);
                Fill(b, 1, 1, 1);
                Text(b, 40, 800, 19, "ESCUELA COPALCHI");
                Text(b, 40, 778, 11, "Reporte de monitoreo academico");

                Fill(b, 0.075, 0.235, 0.325);
                Text(b, 40, 714, 12, "Reporte de estudiantes - " + Cut(grupo, 50));
                Fill(b, 0.30, 0.40, 0.48);
                Text(b, 40, 695, 9, "Año lectivo: " + vm.Anio + "   |   " + periodo);
                Text(b, 40, 680, 9, "Evaluados: " + vm.TotalEvaluados + "   |   Promedio: " +
                    (vm.PromedioGeneral.HasValue ? vm.PromedioGeneral.Value.ToString("N2") : "Sin datos") +
                    "   |   Rendimiento bajo: " + vm.EnRiesgo);

                // Cabecera de la tabla.
                Fill(b, 0.894, 0.933, 0.953);
                Rect(b, 40, 643, 515, 25);
                Fill(b, 0.05, 0.16, 0.23);
                Text(b, 48, 652, 9, "Estudiante");
                Text(b, 258, 652, 9, "Grupo");
                Text(b, 363, 652, 9, "Prom.");
                Text(b, 415, 652, 9, "Materias");
                Text(b, 479, 652, 9, "Estado");

                int inicio = pagina * porPagina;
                int fin = Math.Min(registros.Count, inicio + porPagina);
                if (inicio == fin)
                {
                    Fill(b, 0.32, 0.40, 0.47);
                    Text(b, 48, 619, 9, "No hay estudiantes con calificaciones para los filtros seleccionados.");
                }
                for (int i = inicio; i < fin; i++)
                {
                    var e = registros[i];
                    int y = 619 - (i - inicio) * 20;
                    Fill(b, 0.05, 0.13, 0.20);
                    Text(b, 48, y, 9, Cut(e.Nombre, 37));
                    Text(b, 258, y, 9, Cut(e.Grupo, 17));
                    Text(b, 363, y, 9, e.Promedio.ToString("N2"));
                    Text(b, 423, y, 9, e.Materias.ToString());
                    Text(b, 479, y, 8, Cut(e.Estado, 13));
                    Stroke(b, 0.83, 0.88, 0.92);
                    Line(b, 40, y - 7, 555, y - 7);
                }
                Fill(b, 0.36, 0.44, 0.50);
                Text(b, 40, 46, 8, "Promedios de evaluaciones activas disponibles; no necesariamente notas finales oficiales.");
                Text(b, 500, 30, 8, "Pag. " + (pagina + 1) + "/" + paginas);
                streams.Add(b.ToString());
            }
            return BuildPdf(streams);
        }
        private static void Fill(StringBuilder b, double r, double g, double bl)
        {
            b.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} rg\n", r, g, bl);
        }
        private static void Stroke(StringBuilder b, double r, double g, double bl)
        {
            b.AppendFormat(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} RG\n", r, g, bl);
        }
        private static void Rect(StringBuilder b, int x, int y, int width, int height)
        {
            b.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} re f\n", x, y, width, height);
        }
        private static void Line(StringBuilder b, int x1, int y1, int x2, int y2)
        {
            b.AppendFormat(CultureInfo.InvariantCulture, "0.5 w {0} {1} m {2} {3} l S\n", x1, y1, x2, y2);
        }
        private static string Cut(string s, int max) { return s == null ? "" : s.Length <= max ? s : s.Substring(0, max - 2) + ".."; }
        private static void Text(StringBuilder b, int x, int y, int size, string s)
        {
            var latin = Encoding.GetEncoding(1252).GetBytes(s ?? "");
            var hex = BitConverter.ToString(latin).Replace("-", "");
            b.Append("BT /F1 ").Append(size).Append(" Tf 1 0 0 1 ").Append(x).Append(" ").Append(y).Append(" Tm <").Append(hex).Append("> Tj ET\n");
        }
        private static byte[] BuildPdf(List<string> streams)
        {
            using (var ms = new MemoryStream())
            {
                Action<string> put = s => { var b = Encoding.ASCII.GetBytes(s); ms.Write(b, 0, b.Length); };
                put("%PDF-1.4\n");
                int n = streams.Count; int font = 3 + 2 * n; int total = font;
                var offsets = new long[total + 1];
                Action<int, string> obj = (id, data) => { offsets[id] = ms.Position; put(id + " 0 obj\n" + data + "\nendobj\n"); };
                obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
                obj(2, "<< /Type /Pages /Kids [" + string.Join(" ", Enumerable.Range(0, n).Select(i => (3 + 2 * i) + " 0 R")) + "] /Count " + n + " >>");
                for (int i = 0; i < n; i++)
                {
                    int pg = 3 + 2 * i, ct = pg + 1;
                    obj(pg, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 " + font + " 0 R >> >> /Contents " + ct + " 0 R >>");
                    var data = Encoding.ASCII.GetBytes(streams[i]);
                    offsets[ct] = ms.Position; put(ct + " 0 obj\n<< /Length " + data.Length + " >>\nstream\n"); ms.Write(data, 0, data.Length); put("endstream\nendobj\n");
                }
                obj(font, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
                long xref = ms.Position; put("xref\n0 " + (total + 1) + "\n0000000000 65535 f \n");
                for (int i = 1; i <= total; i++) put(offsets[i].ToString("D10") + " 00000 n \n");
                put("trailer\n<< /Size " + (total + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
                return ms.ToArray();
            }
        }
    }
}
