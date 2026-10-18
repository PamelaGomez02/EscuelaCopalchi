using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace EscuelaCopalchi.UI.Models.GestionNotas
{
    public class ReporteFila
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string Estudiante { get; set; }
        public string Grupo { get; set; }
        public int Anio { get; set; }
        public string Docente { get; set; }
        public string Asignatura { get; set; }      // "" si el grupo aún no tiene evaluaciones
        public int Trimestre { get; set; }
        public string Evaluacion { get; set; }
        public decimal Porcentaje { get; set; }
        public decimal NotaMaxima { get; set; }
        public decimal? Nota { get; set; }
    }

    // Arma los libros de Excel (.xlsx) del reporte de notas
    public static class ReporteNotas
    {
        private const int Normal = 0, Negrita = 1, Encabezado = 2, Titulo = 3;

        public static List<ReporteFila> Obtener(int idDocente, int idGrupo, int idEstudiante)
        {
            DataTable dt = new ConexionBD().ejecutarProcedimiento(
                "SP_GN_Reporte_Notas",
                new[] { "@id_docente", "@id_grupo", "@id_estudiante" },
                new[]
                {
                    idDocente.ToString(CultureInfo.InvariantCulture),
                    idGrupo.ToString(CultureInfo.InvariantCulture),
                    idEstudiante.ToString(CultureInfo.InvariantCulture)
                });

            var lista = new List<ReporteFila>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new ReporteFila
                {
                    IdEstudiante = Convert.ToInt32(r["id_estudiante"]),
                    Identificacion = Txt(r, "identificacion"),
                    Estudiante = Txt(r, "estudiante"),
                    Grupo = Txt(r, "grupo"),
                    Anio = r["anio"] == DBNull.Value ? 0 : Convert.ToInt32(r["anio"]),
                    Docente = Txt(r, "docente"),
                    Asignatura = Txt(r, "asignatura"),
                    Trimestre = r["trimestre"] == DBNull.Value ? 0 : Convert.ToInt32(r["trimestre"]),
                    Evaluacion = Txt(r, "evaluacion"),
                    Porcentaje = r["porcentaje"] == DBNull.Value ? 0m : Convert.ToDecimal(r["porcentaje"]),
                    NotaMaxima = r["nota_maxima"] == DBNull.Value ? 0m : Convert.ToDecimal(r["nota_maxima"]),
                    Nota = r["nota"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["nota"])
                });
            }
            return lista;
        }

        private static string Txt(DataRow r, string c)
        {
            return r[c] == DBNull.Value ? "" : Convert.ToString(r[c]);
        }

        // Nota final (sobre 100) de un trimestre: promedio ponderado de las evaluaciones que ya tienen nota
        private static decimal? Final(IEnumerable<ReporteFila> filas)
        {
            var con = filas.Where(f => f.Evaluacion != "" && f.Nota.HasValue && f.NotaMaxima > 0).ToList();
            if (con.Count == 0) return null;
            decimal pct = con.Sum(f => f.Porcentaje);
            if (pct <= 0) return null;
            decimal puntos = con.Sum(f => f.Nota.Value / f.NotaMaxima * f.Porcentaje);
            return Math.Round(puntos / pct * 100m, 1);
        }

        private static object Num(decimal? v)
        {
            return v.HasValue ? (object)v.Value : "-";
        }

        private static decimal? Promedio(IEnumerable<decimal?> v)
        {
            var l = v.Where(x => x.HasValue).Select(x => x.Value).ToList();
            return l.Count == 0 ? (decimal?)null : Math.Round(l.Average(), 1);
        }

        public static string NombreArchivo(string texto)
        {
            var sb = new StringBuilder();
            foreach (char c in texto ?? "")
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            return sb.Length == 0 ? "Notas" : sb.ToString();
        }

        // ---------- un estudiante ----------
        private static void HojaEstudiante(XlsxHoja h, List<ReporteFila> filas)
        {
            var p = filas[0];
            h.Anchos = new double[] { 30, 14, 28, 14, 12, 14 };

            h.Fila(Titulo, "Reporte de calificaciones");
            h.Fila(Normal);
            h.Fila(Negrita, "Estudiante"); h.Ultima().Poner(1, p.Estudiante);
            h.Fila(Negrita, "Identificación"); h.Ultima().Poner(1, p.Identificacion);
            h.Fila(Negrita, "Grupo"); h.Ultima().Poner(1, p.Grupo + " (" + p.Anio + ")");
            h.Fila(Negrita, "Docente"); h.Ultima().Poner(1, p.Docente);
            h.Fila(Normal);

            h.Fila(Encabezado, "Asignatura", "Trimestre 1", "Trimestre 2", "Trimestre 3", "Promedio");

            var asignaturas = filas.Where(f => f.Asignatura != "").Select(f => f.Asignatura).Distinct().OrderBy(a => a).ToList();
            var promedios = new List<decimal?>();

            foreach (string a in asignaturas)
            {
                var t = new decimal?[3];
                for (int i = 1; i <= 3; i++)
                    t[i - 1] = Final(filas.Where(f => f.Asignatura == a && f.Trimestre == i));

                decimal? prom = Promedio(t);
                promedios.Add(prom);
                h.Fila(Normal, a, Num(t[0]), Num(t[1]), Num(t[2]), Num(prom));
            }

            if (asignaturas.Count == 0)
            {
                h.Fila(Normal, "Sin evaluaciones registradas");
            }
            else
            {
                h.Fila(Negrita, "Promedio general", null, null, null, Num(Promedio(promedios)));
            }

            h.Fila(Normal);
            h.Fila(Negrita, "Detalle de evaluaciones");
            h.Fila(Encabezado, "Asignatura", "Trimestre", "Evaluación", "Valor (%)", "Nota", "Nota máxima");

            foreach (var f in filas.Where(f => f.Evaluacion != "").OrderBy(f => f.Asignatura).ThenBy(f => f.Trimestre))
            {
                h.Fila(Normal, f.Asignatura, f.Trimestre, f.Evaluacion, f.Porcentaje, Num(f.Nota), f.NotaMaxima);
            }
        }

        public static byte[] LibroEstudiante(List<ReporteFila> filas)
        {
            var libro = new XlsxLibro();
            HojaEstudiante(libro.NuevaHoja("Estudiante"), filas);
            return libro.Guardar();
        }

        // ---------- grupo completo ----------
        public static byte[] LibroGrupo(List<ReporteFila> filas)
        {
            var libro = new XlsxLibro();
            var h = libro.NuevaHoja("Resumen");
            var p = filas[0];

            var asignaturas = filas.Where(f => f.Asignatura != "").Select(f => f.Asignatura).Distinct().OrderBy(a => a).ToList();
            var estudiantes = filas.GroupBy(f => f.IdEstudiante).Select(g => g.ToList()).ToList();

            h.Fila(Titulo, "Notas finales por trimestre - " + p.Grupo + " (" + p.Anio + ")");
            h.Fila(Negrita, "Docente"); h.Ultima().Poner(1, p.Docente);
            h.Fila(Normal);

            var enc = new List<object> { "Estudiante", "Identificación" };
            foreach (string a in asignaturas)
                for (int i = 1; i <= 3; i++) enc.Add(a + " T" + i);
            enc.Add("Promedio general");
            h.Fila(Encabezado, enc.ToArray());

            var anchos = new List<double> { 30, 16 };
            for (int i = 0; i < asignaturas.Count * 3; i++) anchos.Add(16);
            anchos.Add(16);
            h.Anchos = anchos.ToArray();

            foreach (var est in estudiantes)
            {
                var fila = new List<object> { est[0].Estudiante, est[0].Identificacion };
                var todos = new List<decimal?>();

                foreach (string a in asignaturas)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        decimal? n = Final(est.Where(f => f.Asignatura == a && f.Trimestre == i));
                        todos.Add(n);
                        fila.Add(Num(n));
                    }
                }

                fila.Add(Num(Promedio(todos)));
                h.Fila(Normal, fila.ToArray());
            }

            int k = 0;
            foreach (var est in estudiantes)
            {
                k++;
                HojaEstudiante(libro.NuevaHoja(k.ToString("00") + " " + est[0].Estudiante), est);
            }

            return libro.Guardar();
        }
    }

    // ---------- escritor mínimo de .xlsx (sin paquetes externos; usa System.IO.Compression) ----------
    public class XlsxHoja
    {
        public string Nombre { get; set; }
        public double[] Anchos { get; set; }
        internal List<List<KeyValuePair<object, int>>> Filas = new List<List<KeyValuePair<object, int>>>();

        public XlsxHoja Fila(int estilo, params object[] valores)
        {
            var f = new List<KeyValuePair<object, int>>();
            foreach (object v in valores) f.Add(new KeyValuePair<object, int>(v, estilo));
            Filas.Add(f);
            return this;
        }

        public XlsxHoja Ultima() { return this; }

        // Pone un valor (estilo normal) en la columna indicada de la última fila
        public void Poner(int columna, object valor)
        {
            var f = Filas[Filas.Count - 1];
            while (f.Count <= columna) f.Add(new KeyValuePair<object, int>(null, 0));
            f[columna] = new KeyValuePair<object, int>(valor, 0);
        }
    }

    public class XlsxLibro
    {
        private readonly List<XlsxHoja> hojas = new List<XlsxHoja>();

        public XlsxHoja NuevaHoja(string nombre)
        {
            var h = new XlsxHoja { Nombre = NombreHoja(nombre) };
            hojas.Add(h);
            return h;
        }

        private string NombreHoja(string n)
        {
            var sb = new StringBuilder();
            foreach (char c in n ?? "Hoja")
            {
                if ("[]:*?/\\".IndexOf(c) < 0) sb.Append(c);
            }
            string b = sb.ToString().Trim();
            if (b.Length == 0) b = "Hoja";
            if (b.Length > 31) b = b.Substring(0, 31);

            string r = b;
            int i = 2;
            while (hojas.Any(x => string.Equals(x.Nombre, r, StringComparison.OrdinalIgnoreCase)))
            {
                string suf = " (" + i++ + ")";
                r = b.Substring(0, Math.Min(b.Length, 31 - suf.Length)) + suf;
            }
            return r;
        }

        private static string Col(int i)
        {
            string s = "";
            i++;
            while (i > 0)
            {
                int m = (i - 1) % 26;
                s = (char)('A' + m) + s;
                i = (i - 1) / 26;
            }
            return s;
        }

        private static string Esc(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "")
            {
                if (c >= 0x20 || c == '\t' || c == '\n' || c == '\r') sb.Append(c);
            }
            return SecurityElement.Escape(sb.ToString());
        }

        private static void Escribir(ZipArchive zip, string ruta, string contenido)
        {
            var e = zip.CreateEntry(ruta);
            using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false)))
            {
                w.Write(contenido);
            }
        }

        public byte[] Guardar()
        {
            const string NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            const string H = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var ct = new StringBuilder(H + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                    for (int i = 1; i <= hojas.Count; i++)
                        ct.Append("<Override PartName=\"/xl/worksheets/sheet" + i + ".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                    ct.Append("</Types>");
                    Escribir(zip, "[Content_Types].xml", ct.ToString());

                    Escribir(zip, "_rels/.rels", H + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"" + R + "/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

                    var wb = new StringBuilder(H + "<workbook xmlns=\"" + NS + "\" xmlns:r=\"" + R + "\"><sheets>");
                    var rel = new StringBuilder(H + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                    for (int i = 1; i <= hojas.Count; i++)
                    {
                        wb.Append("<sheet name=\"" + Esc(hojas[i - 1].Nombre) + "\" sheetId=\"" + i + "\" r:id=\"rId" + i + "\"/>");
                        rel.Append("<Relationship Id=\"rId" + i + "\" Type=\"" + R + "/worksheet\" Target=\"worksheets/sheet" + i + ".xml\"/>");
                    }
                    wb.Append("</sheets></workbook>");
                    rel.Append("<Relationship Id=\"rId" + (hojas.Count + 1) + "\" Type=\"" + R + "/styles\" Target=\"styles.xml\"/></Relationships>");
                    Escribir(zip, "xl/workbook.xml", wb.ToString());
                    Escribir(zip, "xl/_rels/workbook.xml.rels", rel.ToString());

                    Escribir(zip, "xl/styles.xml", H + "<styleSheet xmlns=\"" + NS + "\">" +
                        "<fonts count=\"3\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                        "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                        "<font><b/><sz val=\"14\"/><name val=\"Calibri\"/></font></fonts>" +
                        "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
                        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDCE9F5\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                        "<cellXfs count=\"4\">" +
                        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");

                    for (int i = 1; i <= hojas.Count; i++)
                    {
                        var h = hojas[i - 1];
                        var sb = new StringBuilder(H + "<worksheet xmlns=\"" + NS + "\">");

                        if (h.Anchos != null && h.Anchos.Length > 0)
                        {
                            sb.Append("<cols>");
                            for (int c = 0; c < h.Anchos.Length; c++)
                                sb.Append("<col min=\"" + (c + 1) + "\" max=\"" + (c + 1) + "\" width=\"" +
                                          h.Anchos[c].ToString(CultureInfo.InvariantCulture) + "\" customWidth=\"1\"/>");
                            sb.Append("</cols>");
                        }

                        sb.Append("<sheetData>");
                        for (int r = 0; r < h.Filas.Count; r++)
                        {
                            sb.Append("<row r=\"" + (r + 1) + "\">");
                            for (int c = 0; c < h.Filas[r].Count; c++)
                            {
                                object v = h.Filas[r][c].Key;
                                int st = h.Filas[r][c].Value;
                                if (v == null) continue;

                                string refc = Col(c) + (r + 1);

                                if (v is decimal || v is double || v is int)
                                {
                                    sb.Append("<c r=\"" + refc + "\" s=\"" + st + "\"><v>" +
                                        Convert.ToString(v, CultureInfo.InvariantCulture) + "</v></c>");
                                }
                                else
                                {
                                    sb.Append("<c r=\"" + refc + "\" s=\"" + st + "\" t=\"inlineStr\"><is><t xml:space=\"preserve\">" +
                                        Esc(Convert.ToString(v, CultureInfo.InvariantCulture)) + "</t></is></c>");
                                }
                            }
                            sb.Append("</row>");
                        }
                        sb.Append("</sheetData></worksheet>");
                        Escribir(zip, "xl/worksheets/sheet" + i + ".xml", sb.ToString());
                    }
                }
                return ms.ToArray();
            }
        }
    }
}
