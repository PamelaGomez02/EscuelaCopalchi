using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Lee el Excel de asignaciones, valida cada fila y guarda las válidas.
    ///
    /// Formato esperado (la primera fila con "Grupo" se toma como encabezado):
    ///   Grupo | Nivel | Periodo | Aula | Identificación docente | Días | Hora inicio | Hora fin
    /// Obligatorias: Grupo, Periodo, Aula, Identificación docente.
    /// </summary>
    public class ImportacionExcelService
    {
        public const int MaximoFilas = 1000;

        public static readonly string[] Encabezados =
        {
            "Grupo", "Nivel", "Periodo", "Aula", "Identificación docente", "Días", "Hora inicio", "Hora fin"
        };

        private readonly GrupoService grupoService = new GrupoService();

        /// <summary>
        /// Lee y valida el archivo. Lanza FormatException con un mensaje claro si el formato es incorrecto.
        /// </summary>
        public List<FilaImportacion> LeerYValidar(Stream archivo)
        {
            List<List<string>> hoja = ExcelHelper.LeerPrimeraHoja(archivo);

            // Buscar la fila de encabezados (primeras 10 filas)
            int filaEncabezado = -1;
            for (int i = 0; i < Math.Min(10, hoja.Count); i++)
            {
                if (hoja[i].Any(c => Normalizar(c) == "grupo"))
                {
                    filaEncabezado = i;
                    break;
                }
            }

            if (filaEncabezado < 0)
                throw new FormatException("Formato incorrecto: no se encontró la fila de encabezados. " +
                                          "La primera fila debe tener las columnas: " + string.Join(", ", Encabezados) + ".");

            List<string> encabezados = hoja[filaEncabezado];

            int cGrupo = Columna(encabezados, "grupo");
            int cNivel = Columna(encabezados, "nivel");
            int cPeriodo = Columna(encabezados, "periodo", "ano", "anio", "curso lectivo");
            int cAula = Columna(encabezados, "aula");
            int cDocente = Columna(encabezados, "identificacion docente", "cedula docente", "docente", "identificacion");
            int cDias = Columna(encabezados, "dias", "dia");
            int cInicio = Columna(encabezados, "hora inicio", "inicio", "hora de inicio");
            int cFin = Columna(encabezados, "hora fin", "hora final", "fin", "hora de salida");

            var faltantes = new List<string>();
            if (cGrupo < 0) faltantes.Add("Grupo");
            if (cPeriodo < 0) faltantes.Add("Periodo");
            if (cAula < 0) faltantes.Add("Aula");
            if (cDocente < 0) faltantes.Add("Identificación docente");

            if (faltantes.Count > 0)
                throw new FormatException("Formato incorrecto: faltan las columnas " + string.Join(", ", faltantes) + ".");

            // Filas de datos
            var filas = new List<FilaImportacion>();

            for (int i = filaEncabezado + 1; i < hoja.Count; i++)
            {
                List<string> r = hoja[i];
                if (r.All(string.IsNullOrWhiteSpace)) continue;

                filas.Add(new FilaImportacion
                {
                    NumeroFila = i + 1,
                    Grupo = Celda(r, cGrupo),
                    Nivel = Celda(r, cNivel),
                    Periodo = Celda(r, cPeriodo),
                    Aula = Celda(r, cAula),
                    IdentificacionDocente = Celda(r, cDocente),
                    DiasTexto = Celda(r, cDias),
                    HoraInicio = Hora(Celda(r, cInicio)),
                    HoraFin = Hora(Celda(r, cFin))
                });
            }

            if (filas.Count == 0)
                throw new FormatException("El archivo no tiene registros debajo de los encabezados.");

            if (filas.Count > MaximoFilas)
                throw new FormatException("El archivo tiene " + filas.Count + " registros. El máximo por importación es " + MaximoFilas + ".");

            Validar(filas);
            return filas;
        }

        private void Validar(List<FilaImportacion> filas)
        {
            // Datos actuales del sistema
            Repositorios datos = Repositorios.Consulta();

            var aulas = datos.Aulas.Listar()
                                   .Where(a => a.Estado)
                                   .GroupBy(a => Normalizar(a.Nombre))
                                   .ToDictionary(g => g.Key, g => g.First());

            var docentes = datos.Usuarios.Listar()
                                      .Where(d => d.Estado && d.EsDocente)
                                      .GroupBy(d => SoloAlfanumerico(d.Identificacion))
                                      .ToDictionary(g => g.Key, g => g.First());

            var gruposExistentes = new HashSet<string>(
                datos.Grupos.Listar().Select(g => Normalizar(g.Nombre) + "|" + g.Periodo));

            var vistos = new Dictionary<string, int>();

            foreach (FilaImportacion f in filas)
            {
                // Grupo
                if (string.IsNullOrWhiteSpace(f.Grupo))
                    f.Errores.Add("El grupo es obligatorio.");
                else if (f.Grupo.Length > 100)
                    f.Errores.Add("El nombre del grupo admite máximo 100 caracteres.");

                if (!string.IsNullOrEmpty(f.Nivel) && f.Nivel.Length > 50)
                    f.Errores.Add("El nivel admite máximo 50 caracteres.");

                // Período
                int periodo;
                decimal periodoDecimal;
                if (decimal.TryParse(f.Periodo, NumberStyles.Any, CultureInfo.InvariantCulture, out periodoDecimal)
                    && periodoDecimal == Math.Floor(periodoDecimal))
                {
                    periodo = (int)periodoDecimal;
                    f.Periodo = periodo.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    periodo = 0;
                }

                if (periodo < 2000 || periodo > 2100)
                    f.Errores.Add("El período debe ser un año válido (ej. 2026).");

                // Aula
                Aula aula;
                if (string.IsNullOrWhiteSpace(f.Aula))
                    f.Errores.Add("El aula es obligatoria.");
                else if (!aulas.TryGetValue(Normalizar(f.Aula), out aula))
                    f.Errores.Add("El aula \"" + f.Aula + "\" no existe o está inactiva.");
                else
                    f.Aula = aula.Nombre; // nombre exacto como está en la base de datos

                // Docente
                UsuarioSistema docente;
                if (string.IsNullOrWhiteSpace(f.IdentificacionDocente))
                    f.Errores.Add("La identificación del docente es obligatoria.");
                else if (!docentes.TryGetValue(SoloAlfanumerico(f.IdentificacionDocente), out docente))
                    f.Errores.Add("No existe un docente activo con identificación \"" + f.IdentificacionDocente + "\".");
                else
                {
                    f.IdentificacionDocente = docente.Identificacion;
                    f.NombreDocente = docente.NombreCompleto;
                }

                // Horario (opcional)
                bool hayHorario = !string.IsNullOrWhiteSpace(f.DiasTexto) || !string.IsNullOrWhiteSpace(f.HoraInicio) || !string.IsNullOrWhiteSpace(f.HoraFin);
                if (hayHorario)
                {
                    string dias = ConvertirDias(f.DiasTexto);
                    TimeSpan hi, hf;
                    bool okInicio = TimeSpan.TryParseExact(f.HoraInicio ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out hi);
                    bool okFin = TimeSpan.TryParseExact(f.HoraFin ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out hf);

                    if (dias == null)
                        f.Errores.Add("Los días \"" + f.DiasTexto + "\" no son válidos. Use por ejemplo: Lunes a Viernes, L-V o 1,3,5.");
                    else
                        f.Dias = dias;

                    if (!okInicio || !okFin)
                        f.Errores.Add("Las horas deben tener formato HH:mm (ej. 07:00).");
                    else if (hf <= hi)
                        f.Errores.Add("La hora final debe ser mayor que la hora de inicio.");
                }

                // Duplicados
                if (!string.IsNullOrWhiteSpace(f.Grupo) && periodo > 0)
                {
                    string clave = Normalizar(f.Grupo) + "|" + periodo;
                    int filaPrevia;

                    if (vistos.TryGetValue(clave, out filaPrevia))
                    {
                        f.EsDuplicado = true;
                        f.Errores.Add("Registro duplicado en el archivo (igual a la fila " + filaPrevia + ").");
                    }
                    else
                    {
                        vistos[clave] = f.NumeroFila;
                    }

                    if (gruposExistentes.Contains(clave))
                    {
                        f.EsDuplicado = true;
                        f.Errores.Add("El grupo " + f.Grupo + " ya existe en el período " + periodo + ".");
                    }
                }
            }
        }

        /// <summary>
        /// Guarda las filas válidas en una sola transacción: si una falla, no se guarda ninguna.
        /// Cada grupo se crea con las mismas reglas que desde la pantalla (bitácora y notificación al docente).
        /// </summary>
        public ResultadoOperacion Importar(List<FilaImportacion> filas, int idResponsable)
        {
            List<FilaImportacion> validas = filas.Where(f => f.Valida).ToList();

            return Operacion.Ejecutar(() =>
            {
                if (validas.Count == 0)
                    throw new ReglaNegocioException("No hay registros válidos para importar.");

                using (var tx = new Transaccion())
                {
                    var datos = new Repositorios(tx);

                    // Aulas y docentes por nombre / identificación (ya validados en la vista previa)
                    var aulas = datos.Aulas.Listar().Where(a => a.Estado)
                                     .GroupBy(a => a.Nombre).ToDictionary(g => g.Key, g => g.First().IdAula);
                    var docentes = datos.Usuarios.Listar().Where(d => d.Estado && d.EsDocente)
                                        .GroupBy(d => d.Identificacion).ToDictionary(g => g.Key, g => g.First().IdDocente);

                    foreach (FilaImportacion f in validas)
                    {
                        int idAula, idDocente;
                        if (!aulas.TryGetValue(f.Aula, out idAula) || !docentes.TryGetValue(f.IdentificacionDocente, out idDocente))
                            throw new ReglaNegocioException("Fila " + f.NumeroFila + ": el aula o el docente ya no existen. Vuelva a cargar el archivo.");

                        var grupo = new Grupo
                        {
                            Nombre = f.Grupo,
                            Nivel = f.Nivel,
                            Periodo = int.Parse(f.Periodo, CultureInfo.InvariantCulture),
                            IdAula = idAula,
                            IdDocente = idDocente,
                            Cupo = 0, // se usa la capacidad del aula
                            Dias = f.Dias,
                            HoraInicio = f.HoraInicio,
                            HoraFin = f.HoraFin
                        };

                        try
                        {
                            grupoService.GuardarEnTransaccion(datos, grupo, idResponsable);
                        }
                        catch (ReglaNegocioException ex)
                        {
                            throw new ReglaNegocioException("Fila " + f.NumeroFila + ": " + ex.Message);
                        }
                    }

                    datos.Auditoria.Registrar(Modulos.GestionAcademica, TiposCambio.Importacion, "GRUPO", 0,
                        "Importación desde Excel: " + validas.Count + " grupos registrados (" +
                        string.Join(", ", validas.Select(f => f.Grupo)) + ").", idResponsable);

                    tx.Confirmar();
                }

                string mensaje = "Importación exitosa: " + validas.Count + " asignaciones registradas.";
                int omitidas = filas.Count - validas.Count;
                return omitidas > 0 ? mensaje + " Se omitieron " + omitidas + " registros con inconsistencias." : mensaje;
            });
        }

        /// <summary>Plantilla de ejemplo para descargar.</summary>
        public static byte[] GenerarPlantilla()
        {
            var ejemplo = new List<string[]>
            {
                new[] { "1-A", "Primero", DateTime.Today.Year.ToString(CultureInfo.InvariantCulture), "Aula 1", "1-1111-1111", "Lunes a Viernes", "07:00", "12:00" },
                new[] { "2-A", "Segundo", DateTime.Today.Year.ToString(CultureInfo.InvariantCulture), "Aula 2", "2-2222-2222", "L,K,M,J,V", "12:30", "17:00" }
            };

            return ExcelHelper.Generar("Asignaciones", null, null, Encabezados, ejemplo);
        }

        // ================================================================
        // Utilidades
        // ================================================================

        private static string Celda(List<string> fila, int columna)
        {
            return columna >= 0 && columna < fila.Count ? (fila[columna] ?? string.Empty).Trim() : string.Empty;
        }

        private static int Columna(List<string> encabezados, params string[] nombres)
        {
            var normalizados = encabezados.Select(Normalizar).ToList();

            foreach (string nombre in nombres)
            {
                int i = normalizados.IndexOf(nombre);
                if (i >= 0) return i;
            }

            return -1;
        }

        /// <summary>Minúsculas, sin tildes y sin espacios repetidos.</summary>
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in descompuesto)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);

            return string.Join(" ", sb.ToString().Normalize(NormalizationForm.FormC)
                                      .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string SoloAlfanumerico(string texto)
        {
            return new string((texto ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        /// <summary>
        /// Convierte horas del Excel a "HH:mm". Excel guarda las horas como fracción del día
        /// (0.2916 = 07:00); también acepta texto como "7:00", "07:00 a.m." o "1:30 pm".
        /// </summary>
        private static string Hora(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

            double fraccion;
            if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out fraccion) && fraccion >= 0 && fraccion < 1)
            {
                TimeSpan t = TimeSpan.FromMinutes(Math.Round(fraccion * 24 * 60));
                return t.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            }

            string texto = Normalizar(valor).Replace(".", "").Replace(" ", "");
            bool pm = texto.EndsWith("pm", StringComparison.Ordinal) || texto.EndsWith("md", StringComparison.Ordinal);
            bool am = texto.EndsWith("am", StringComparison.Ordinal);
            if (pm || am) texto = texto.Substring(0, texto.Length - 2);

            DateTime dt;
            if (DateTime.TryParseExact(texto, new[] { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "H", "HH" },
                                       CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                int horas = dt.Hour;
                if (pm && horas < 12) horas += 12;
                if (am && horas == 12) horas = 0;
                return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", horas, dt.Minute);
            }

            return valor.Trim(); // se reporta como error en la validación
        }

        private static readonly Dictionary<string, int> MapaDias = new Dictionary<string, int>
        {
            { "1", 1 }, { "l", 1 }, { "lu", 1 }, { "lun", 1 }, { "lunes", 1 },
            { "2", 2 }, { "k", 2 }, { "ma", 2 }, { "mar", 2 }, { "martes", 2 },
            { "3", 3 }, { "m", 3 }, { "mi", 3 }, { "mie", 3 }, { "miercoles", 3 },
            { "4", 4 }, { "j", 4 }, { "ju", 4 }, { "jue", 4 }, { "jueves", 4 },
            { "5", 5 }, { "v", 5 }, { "vi", 5 }, { "vie", 5 }, { "viernes", 5 },
            { "6", 6 }, { "s", 6 }, { "sa", 6 }, { "sab", 6 }, { "sabado", 6 },
            { "7", 7 }, { "d", 7 }, { "do", 7 }, { "dom", 7 }, { "domingo", 7 }
        };

        /// <summary>
        /// "Lunes a Viernes", "L-V", "lunes, miércoles", "1,3,5", "L K M J V" → "1,2,3,4,5".
        /// Devuelve null si no se entiende.
        /// </summary>
        public static string ConvertirDias(string texto)
        {
            string t = Normalizar(texto);
            if (t == string.Empty) return null;

            var dias = new SortedSet<int>();

            // Rango: "lunes a viernes" / "l-v"
            string[] rango = t.Split(new[] { " a ", "-", " al " }, StringSplitOptions.RemoveEmptyEntries);
            if (rango.Length == 2)
            {
                int desde, hasta;
                if (MapaDias.TryGetValue(rango[0].Trim(), out desde) && MapaDias.TryGetValue(rango[1].Trim(), out hasta) && desde <= hasta)
                {
                    for (int d = desde; d <= hasta; d++) dias.Add(d);
                    return string.Join(",", dias);
                }
                return null;
            }

            foreach (string parte in t.Split(new[] { ',', ';', '/', ' ', 'y' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int d;
                if (!MapaDias.TryGetValue(parte.Trim(), out d)) return null;
                dias.Add(d);
            }

            return dias.Count == 0 ? null : string.Join(",", dias);
        }
    }
}