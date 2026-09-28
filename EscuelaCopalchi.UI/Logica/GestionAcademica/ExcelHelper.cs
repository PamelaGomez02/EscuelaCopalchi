using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Lectura y escritura básica de archivos Excel (.xlsx) sin librerías externas.
    /// Un .xlsx es un ZIP con archivos XML (formato Office Open XML).
    /// </summary>
    public static class ExcelHelper
    {
        private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace NsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace NsPkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        // ================================================================
        // LECTURA
        // ================================================================

        /// <summary>
        /// Lee la primera hoja del archivo y devuelve sus filas como texto.
        /// Lanza FormatException si el archivo no es un .xlsx válido.
        /// </summary>
        public static List<List<string>> LeerPrimeraHoja(Stream archivo)
        {
            try
            {
                using (var zip = new ZipArchive(archivo, ZipArchiveMode.Read, true))
                {
                    List<string> compartidos = LeerTextosCompartidos(zip);
                    string rutaHoja = RutaPrimeraHoja(zip);

                    ZipArchiveEntry entrada = zip.GetEntry(rutaHoja);
                    if (entrada == null)
                        throw new FormatException("El archivo no contiene hojas de cálculo.");

                    XDocument hoja;
                    using (Stream s = entrada.Open())
                        hoja = XDocument.Load(s);

                    var filas = new List<List<string>>();

                    foreach (XElement row in hoja.Descendants(Ns + "row"))
                    {
                        var celdas = new List<string>();

                        foreach (XElement c in row.Elements(Ns + "c"))
                        {
                            int columna = IndiceColumna((string)c.Attribute("r"), celdas.Count);

                            while (celdas.Count < columna)
                                celdas.Add(string.Empty);

                            celdas.Add(ValorCelda(c, compartidos));
                        }

                        // Respeta el número de fila del Excel (las filas vacías no vienen en el XML)
                        int numeroFila;
                        if (int.TryParse((string)row.Attribute("r"), out numeroFila))
                        {
                            while (filas.Count < numeroFila - 1)
                                filas.Add(new List<string>());
                        }

                        filas.Add(celdas);
                    }

                    return filas;
                }
            }
            catch (InvalidDataException)
            {
                throw new FormatException("El archivo no es un Excel .xlsx válido.");
            }
            catch (System.Xml.XmlException)
            {
                throw new FormatException("El archivo Excel está dañado o no tiene el formato esperado.");
            }
        }

        private static List<string> LeerTextosCompartidos(ZipArchive zip)
        {
            var lista = new List<string>();
            ZipArchiveEntry entrada = zip.GetEntry("xl/sharedStrings.xml");
            if (entrada == null) return lista;

            using (Stream s = entrada.Open())
            {
                XDocument doc = XDocument.Load(s);
                foreach (XElement si in doc.Root.Elements(Ns + "si"))
                    lista.Add(string.Concat(si.Descendants(Ns + "t").Select(t => t.Value)));
            }

            return lista;
        }

        private static string RutaPrimeraHoja(ZipArchive zip)
        {
            try
            {
                XDocument libro, rels;
                using (Stream s = zip.GetEntry("xl/workbook.xml").Open()) libro = XDocument.Load(s);
                using (Stream s = zip.GetEntry("xl/_rels/workbook.xml.rels").Open()) rels = XDocument.Load(s);

                XElement hoja = libro.Descendants(Ns + "sheet").First();
                string idRel = (string)hoja.Attribute(NsRel + "id");

                string destino = rels.Root.Elements(NsPkgRel + "Relationship")
                                     .First(r => (string)r.Attribute("Id") == idRel)
                                     .Attribute("Target").Value;

                destino = destino.TrimStart('/');
                return destino.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? destino : "xl/" + destino;
            }
            catch (Exception)
            {
                return "xl/worksheets/sheet1.xml";
            }
        }

        private static string ValorCelda(XElement c, List<string> compartidos)
        {
            string tipo = (string)c.Attribute("t");

            if (tipo == "inlineStr")
                return string.Concat(c.Descendants(Ns + "t").Select(t => t.Value)).Trim();

            string valor = (string)c.Element(Ns + "v") ?? string.Empty;

            if (tipo == "s")
            {
                int i;
                return int.TryParse(valor, out i) && i >= 0 && i < compartidos.Count ? compartidos[i].Trim() : string.Empty;
            }

            if (tipo == "b")
                return valor == "1" ? "VERDADERO" : "FALSO";

            return valor.Trim();
        }

        /// <summary>Convierte "C5" en 2 (índice base cero).</summary>
        private static int IndiceColumna(string referencia, int porDefecto)
        {
            if (string.IsNullOrEmpty(referencia)) return porDefecto;

            int indice = 0;
            foreach (char ch in referencia)
            {
                if (!char.IsLetter(ch)) break;
                indice = indice * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            }

            return indice == 0 ? porDefecto : indice - 1;
        }

        // ================================================================
        // ESCRITURA
        // ================================================================

        /// <summary>
        /// Genera un .xlsx de una hoja: título (opcional), subtítulo (opcional),
        /// encabezados en negrita y filas de datos.
        /// </summary>
        public static byte[] Generar(string nombreHoja, string titulo, string subtitulo,
                                     IList<string> columnas, IEnumerable<string[]> filas)
        {
            var hoja = new StringBuilder();
            var anchos = columnas.Select(c => Math.Max(10, c.Length + 2)).ToArray();
            var listaFilas = filas.ToList();

            foreach (string[] f in listaFilas)
                for (int i = 0; i < f.Length && i < anchos.Length; i++)
                    anchos[i] = Math.Min(60, Math.Max(anchos[i], (f[i] ?? "").Length + 2));

            hoja.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            hoja.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            hoja.Append("<cols>");
            for (int i = 0; i < anchos.Length; i++)
                hoja.AppendFormat(CultureInfo.InvariantCulture, "<col min=\"{0}\" max=\"{0}\" width=\"{1}\" customWidth=\"1\"/>", i + 1, anchos[i]);
            hoja.Append("</cols><sheetData>");

            int fila = 1;

            if (!string.IsNullOrEmpty(titulo))
                hoja.Append(Fila(fila++, new[] { titulo }, 2));

            if (!string.IsNullOrEmpty(subtitulo))
                hoja.Append(Fila(fila++, new[] { subtitulo }, 0));

            if (!string.IsNullOrEmpty(titulo) || !string.IsNullOrEmpty(subtitulo))
                fila++; // fila en blanco

            hoja.Append(Fila(fila++, columnas.ToArray(), 1));

            foreach (string[] f in listaFilas)
                hoja.Append(Fila(fila++, f, 0));

            hoja.Append("</sheetData></worksheet>");

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    Escribir(zip, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                        "</Types>");

                    Escribir(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                        "</Relationships>");

                    Escribir(zip, "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                        "<sheets><sheet name=\"" + Xml(NombreHojaValido(nombreHoja)) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                        "</workbook>");

                    Escribir(zip, "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                        "</Relationships>");

                    // Estilos: 0 = normal, 1 = encabezado (negrita, fondo), 2 = título (negrita, grande)
                    Escribir(zip, "xl/styles.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                        "<fonts count=\"3\">" +
                        "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                        "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>" +
                        "<font><b/><sz val=\"14\"/><color rgb=\"FF133C55\"/><name val=\"Calibri\"/></font>" +
                        "</fonts>" +
                        "<fills count=\"3\">" +
                        "<fill><patternFill patternType=\"none\"/></fill>" +
                        "<fill><patternFill patternType=\"gray125\"/></fill>" +
                        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF133C55\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
                        "</fills>" +
                        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                        "<cellXfs count=\"3\">" +
                        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                        "</cellXfs>" +
                        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                        "</styleSheet>");

                    Escribir(zip, "xl/worksheets/sheet1.xml", hoja.ToString());
                }

                return ms.ToArray();
            }
        }

        private static string Fila(int numero, string[] valores, int estilo)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "<row r=\"{0}\">", numero);

            for (int i = 0; i < valores.Length; i++)
            {
                string referencia = LetraColumna(i) + numero.ToString(CultureInfo.InvariantCulture);
                string valor = valores[i] ?? string.Empty;
                string atributoEstilo = estilo > 0 ? " s=\"" + estilo + "\"" : string.Empty;

                decimal numeroCelda;
                bool esNumero = estilo == 0
                                && valor.Length > 0 && valor.Length < 15
                                && !valor.StartsWith("0", StringComparison.Ordinal)
                                && decimal.TryParse(valor, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                                    CultureInfo.InvariantCulture, out numeroCelda);

                if (esNumero)
                    sb.AppendFormat("<c r=\"{0}\"{1}><v>{2}</v></c>", referencia, atributoEstilo, valor);
                else
                    sb.AppendFormat("<c r=\"{0}\"{1} t=\"inlineStr\"><is><t xml:space=\"preserve\">{2}</t></is></c>",
                                    referencia, atributoEstilo, Xml(valor));
            }

            sb.Append("</row>");
            return sb.ToString();
        }

        private static string LetraColumna(int indice)
        {
            string letra = string.Empty;
            indice++;
            while (indice > 0)
            {
                int resto = (indice - 1) % 26;
                letra = (char)('A' + resto) + letra;
                indice = (indice - 1) / 26;
            }
            return letra;
        }

        private static string NombreHojaValido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "Hoja1";
            foreach (char c in new[] { '\\', '/', '?', '*', '[', ']', ':' })
                nombre = nombre.Replace(c, ' ');
            return nombre.Length > 31 ? nombre.Substring(0, 31) : nombre;
        }

        private static string Xml(string texto)
        {
            // Quita caracteres de control no válidos en XML
            var limpio = new string((texto ?? string.Empty).Where(ch => ch == '\t' || ch == '\n' || ch == '\r' || ch >= ' ').ToArray());
            return SecurityElement.Escape(limpio);
        }

        private static void Escribir(ZipArchive zip, string ruta, string contenido)
        {
            ZipArchiveEntry entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
            using (var w = new StreamWriter(entrada.Open(), new UTF8Encoding(false)))
                w.Write(contenido);
        }
    }
}