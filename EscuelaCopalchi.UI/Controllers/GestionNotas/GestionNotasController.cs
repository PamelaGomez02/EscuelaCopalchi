using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExcelDataReader;
using EscuelaCopalchi.UI.Models.GestionNotas;

namespace EscuelaCopalchi.UI.Controllers
{
    // Entra quien tenga REGISTRAR_NOTAS (docente) o VER_TODAS_NOTAS (director / administrador).
    // Registrar, crear evaluaciones y editar exigen REGISTRAR_NOTAS y además que el usuario
    // sea el docente del grupo (el procedimiento almacenado lo vuelve a validar).
    [NotasAcceso]
    public class GestionNotasController : Controller
    {
        private readonly NotasRepository repo = new NotasRepository();

        // ---------------------------------------------------------------
        // Utilidades de sesión (sesión propia de Gestión de notas)
        // ---------------------------------------------------------------
        private NotasSesion Sesion
        {
            get { return NotasSesion.Obtener(Session); }
        }

        private int IdUsuario()
        {
            var s = Sesion;
            return s == null ? 0 : s.IdUsuario;
        }

        private bool Tiene(string permiso)
        {
            var s = Sesion;
            if (s == null) return false;
            if (permiso == "REGISTRAR_NOTAS") return s.PuedeRegistrar;
            if (permiso == "VER_TODAS_NOTAS") return s.VeTodo;
            return false;
        }

        // Alcance para escribir (registrar, importar, modificar):
        // > 0 = solo los grupos de ese docente, 0 = todos los grupos (Director), -1 = no puede escribir.
        private int IdDocente()
        {
            var s = Sesion;
            if (s == null) return -1;
            if (s.EsDirector) return 0;
            if (s.EsDocente) return s.IdDocente > 0 ? s.IdDocente : -1;
            return -1;
        }

        // Alcance para consultar: 0 = todos los grupos (VER_TODAS_NOTAS),
        // > 0 = solo los grupos de ese docente, -1 = no puede consultar nada.
        private int AlcanceLectura()
        {
            if (Tiene("VER_TODAS_NOTAS")) return 0;
            int d = IdDocente();
            return d > 0 ? d : -1;
        }

        private static bool TryParseNumero(string texto, out decimal valor)
        {
            valor = 0m;
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return decimal.TryParse(texto.Trim().Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private ActionResult VolverARegistrar(int idGrupo, string asignatura, int trimestre)
        {
            return RedirectToAction("Registrar", new { grupo = idGrupo, asignatura = asignatura, trimestre = trimestre });
        }

        // ---------------------------------------------------------------
        // ACCESO: elegir usuario e ingresar contraseña
        // ---------------------------------------------------------------
        [HttpGet]
        public ActionResult Acceso()
        {
            var lista = new List<UsuarioAccesoItem>();

            try { lista = repo.ListarUsuariosAcceso(); }
            catch (Exception ex) { ViewBag.Error = ex.Message; }

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Acceso(int idUsuario = 0, string contrasena = "")
        {
            try
            {
                var usuario = idUsuario > 0 ? repo.ObtenerUsuarioAcceso(idUsuario) : null;

                if (usuario == null || !repo.VerificarContrasena(idUsuario, contrasena))
                {
                    TempData["Error"] = "Usuario o contraseña incorrectos.";
                    return RedirectToAction("Acceso");
                }

                NotasSesion.Guardar(Session, usuario);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Acceso");
            }
        }

        public ActionResult Salir()
        {
            NotasSesion.Cerrar(Session);
            return RedirectToAction("Acceso");
        }

        // ---------------------------------------------------------------
        // INDEX: mis grupos
        // ---------------------------------------------------------------
        public ActionResult Index()
        {
            var grupos = new List<GrupoNotasItem>();

            try
            {
                int alcance = AlcanceLectura();

                if (alcance >= 0)
                {
                    grupos = repo.ListarGrupos(alcance);
                }
                else
                {
                    ViewBag.Error = "Su usuario no tiene un registro de docente, por eso no hay grupos para mostrar.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            ViewBag.VeTodo = Tiene("VER_TODAS_NOTAS");
            ViewBag.PuedeRegistrar = Tiene("REGISTRAR_NOTAS");

            return View(grupos);
        }

        // ---------------------------------------------------------------
        // REGISTRAR notas
        // ---------------------------------------------------------------
        [HttpGet]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult Registrar(int? grupo, string asignatura, int trimestre = 1)
        {
            int idDocente = IdDocente();

            if (idDocente < 0)
            {
                TempData["Error"] = "Su usuario no tiene un registro de docente ni permiso para registrar notas.";
                return RedirectToAction("Index");
            }

            var vm = new RegistrarNotasViewModel
            {
                Trimestre = (trimestre < 1 || trimestre > 3) ? 1 : trimestre
            };

            try
            {
                vm.Grupos = repo.ListarGrupos(idDocente);

                if (vm.Grupos.Count == 0)
                {
                    ViewBag.Error = "No tiene grupos asignados.";
                    return View(vm);
                }

                var actual = vm.Grupos.FirstOrDefault(g => g.IdGrupo == grupo) ?? vm.Grupos[0];

                vm.IdGrupo = actual.IdGrupo;
                vm.Asignaturas = repo.ListarAsignaturas(vm.IdGrupo);
                vm.Asignatura = !string.IsNullOrWhiteSpace(asignatura)
                    ? asignatura
                    : (vm.Asignaturas.FirstOrDefault() ?? "");
                vm.Evaluaciones = repo.ListarEvaluaciones(vm.IdGrupo, vm.Asignatura, vm.Trimestre);
                vm.Estudiantes = repo.ObtenerMatriz(vm.IdGrupo, vm.Asignatura, vm.Trimestre);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult Registrar(int idGrupo, string asignatura, int trimestre, List<NotaInput> notas)
        {
            int idDocente = IdDocente();

            if (idDocente < 0)
            {
                TempData["Error"] = "Su usuario no tiene un registro de docente ni permiso para registrar notas.";
                return RedirectToAction("Index");
            }

            var lista = new List<NotaGuardar>();

            foreach (var n in notas ?? new List<NotaInput>())
            {
                if (string.IsNullOrWhiteSpace(n.Nota)) continue;

                decimal valor;
                if (!TryParseNumero(n.Nota, out valor))
                {
                    TempData["Error"] = "Hay una nota con formato inválido: " + n.Nota;
                    return VolverARegistrar(idGrupo, asignatura, trimestre);
                }

                lista.Add(new NotaGuardar
                {
                    IdEvaluacion = n.IdEvaluacion,
                    IdEstudiante = n.IdEstudiante,
                    Nota = valor
                });
            }

            if (lista.Count == 0)
            {
                TempData["Error"] = "No hay notas para guardar.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            string resultado = repo.GuardarLote(IdUsuario(), idDocente, lista);

            if (resultado == NotasRepository.Ok)
            {
                TempData["Success"] = "Calificaciones guardadas correctamente.";
            }
            else
            {
                TempData["Error"] = resultado;
            }

            return VolverARegistrar(idGrupo, asignatura, trimestre);
        }

        // ---------------------------------------------------------------
        // CREAR EVALUACIÓN (desde la pantalla Registrar)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult CrearEvaluacion(
            int idGrupo, string asignatura, int trimestre, string nombre,
            string porcentaje, string fecha, string notaMaxima)
        {
            int idDocente = IdDocente();

            if (idDocente < 0)
            {
                TempData["Error"] = "Su usuario no tiene un registro de docente ni permiso para registrar notas.";
                return RedirectToAction("Index");
            }

            asignatura = (asignatura ?? "").Trim();
            nombre = (nombre ?? "").Trim();

            if (asignatura == "" || nombre == "")
            {
                TempData["Error"] = "La asignatura y el nombre de la evaluación son obligatorios.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            decimal pct;
            if (!TryParseNumero(porcentaje, out pct))
            {
                TempData["Error"] = "El porcentaje no es válido.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            decimal max = 100m;
            if (!string.IsNullOrWhiteSpace(notaMaxima) && !TryParseNumero(notaMaxima, out max))
            {
                TempData["Error"] = "La nota máxima no es válida.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            DateTime? fechaEval = null;
            DateTime f;
            if (!string.IsNullOrWhiteSpace(fecha)
                && DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out f))
            {
                fechaEval = f;
            }

            string resultado = repo.CrearEvaluacion(
                idGrupo, asignatura, trimestre, nombre, pct, fechaEval, max, IdUsuario(), idDocente);

            if (resultado == NotasRepository.Ok)
            {
                TempData["Success"] = "Evaluación creada correctamente.";
            }
            else
            {
                TempData["Error"] = resultado;
            }

            return VolverARegistrar(idGrupo, asignatura, trimestre);
        }

        // ---------------------------------------------------------------
        // EDITAR / ELIMINAR EVALUACIÓN (desde la pantalla Registrar)
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult EditarEvaluacion(
            int idEvaluacion, int idGrupo, string asignatura, int trimestre, string nombre,
            string porcentaje, string fecha, string notaMaxima)
        {
            nombre = (nombre ?? "").Trim();

            decimal pct, max = 100m;
            if (nombre == "" || !TryParseNumero(porcentaje, out pct))
            {
                TempData["Error"] = "El nombre y el porcentaje son obligatorios y deben ser válidos.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            if (!string.IsNullOrWhiteSpace(notaMaxima) && !TryParseNumero(notaMaxima, out max))
            {
                TempData["Error"] = "La nota máxima no es válida.";
                return VolverARegistrar(idGrupo, asignatura, trimestre);
            }

            DateTime? fechaEval = null;
            DateTime f;
            if (!string.IsNullOrWhiteSpace(fecha)
                && DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out f))
            {
                fechaEval = f;
            }

            string resultado = repo.ActualizarEvaluacion(
                idEvaluacion, nombre, pct, fechaEval, max, IdUsuario(), IdDocente());

            if (resultado == NotasRepository.Ok) TempData["Success"] = "Evaluación actualizada correctamente.";
            else TempData["Error"] = resultado;

            return VolverARegistrar(idGrupo, asignatura, trimestre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult EliminarEvaluacion(int idEvaluacion, int idGrupo, string asignatura, int trimestre)
        {
            string resultado = repo.EliminarEvaluacion(idEvaluacion, IdUsuario(), IdDocente());

            if (resultado == NotasRepository.Ok) TempData["Success"] = "Evaluación eliminada correctamente.";
            else TempData["Error"] = resultado;

            return VolverARegistrar(idGrupo, asignatura, trimestre);
        }

        // ---------------------------------------------------------------
        // EDITAR una nota (se llega desde el historial)
        // ---------------------------------------------------------------
        private bool EsDelDocente(CalificacionDetalle d)
        {
            int doc = IdDocente();
            return doc == 0 || (doc > 0 && d.IdDocente.HasValue && d.IdDocente.Value == doc);
        }

        [HttpGet]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue) return RedirectToAction("Historial");

            try
            {
                var detalle = repo.ObtenerCalificacion(id.Value);

                if (detalle == null) return HttpNotFound();

                if (!EsDelDocente(detalle))
                {
                    TempData["Error"] = "Solo el docente del grupo (o el director) puede modificar esta calificación.";
                    return RedirectToAction("Historial");
                }

                return View(detalle);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Historial");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult Editar(int idCalificacion, string nota, string observacion)
        {
            try
            {
                var detalle = repo.ObtenerCalificacion(idCalificacion);

                if (detalle == null) return HttpNotFound();

                if (!EsDelDocente(detalle))
                {
                    TempData["Error"] = "Solo el docente del grupo (o el director) puede modificar esta calificación.";
                    return RedirectToAction("Historial");
                }

                decimal valor;
                if (!TryParseNumero(nota, out valor))
                {
                    ViewBag.Error = "La nota no es válida.";
                    return View(detalle);
                }

                string resultado = repo.GuardarLote(IdUsuario(), IdDocente(), new List<NotaGuardar>
                {
                    new NotaGuardar
                    {
                        IdEvaluacion = detalle.IdEvaluacion,
                        IdEstudiante = detalle.IdEstudiante,
                        Nota = valor,
                        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim()
                    }
                });

                if (resultado == NotasRepository.Ok)
                {
                    TempData["Success"] = "Calificación actualizada correctamente.";
                    return RedirectToAction("Historial", new { grupo = detalle.IdGrupo });
                }

                ViewBag.Error = resultado;
                return View(detalle);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Historial");
            }
        }

        // ---------------------------------------------------------------
        // HISTORIAL
        // ---------------------------------------------------------------
        public ActionResult Historial(int grupo = 0, int estudiante = 0, string asignatura = "", int trimestre = 0)
        {
            var vm = new HistorialViewModel
            {
                Grupo = grupo,
                Estudiante = estudiante,
                Asignatura = asignatura ?? "",
                Trimestre = trimestre
            };

            try
            {
                int alcance = AlcanceLectura();

                if (alcance < 0)
                {
                    ViewBag.Error = "Su usuario no tiene un registro de docente, por eso no hay notas para mostrar.";
                    return View(vm);
                }

                vm.Grupos = repo.ListarGrupos(alcance);

                // Un grupo que no esté en su alcance se ignora
                if (grupo > 0 && !vm.Grupos.Any(g => g.IdGrupo == grupo))
                {
                    grupo = 0;
                    estudiante = 0;
                    vm.Grupo = 0;
                    vm.Estudiante = 0;
                }

                if (grupo > 0)
                {
                    vm.Estudiantes = repo.ListarEstudiantesGrupo(grupo);
                }

                vm.Items = repo.Historial(alcance, grupo, estudiante, vm.Asignatura, trimestre);
                vm.IdDocenteActual = IdDocente();
                vm.PuedeEditar = Tiene("REGISTRAR_NOTAS");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            return View(vm);
        }

        // ---------------------------------------------------------------
        // EXPORTAR a Excel
        // ---------------------------------------------------------------
        private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Todos los estudiantes del grupo: hoja resumen + una hoja por estudiante
        public ActionResult ExportarGrupo(int grupo = 0)
        {
            try
            {
                int alcance = AlcanceLectura();

                if (alcance < 0 || grupo <= 0)
                {
                    TempData["Error"] = "Seleccione un grupo para exportar.";
                    return RedirectToAction("Historial");
                }

                var filas = ReporteNotas.Obtener(alcance, grupo, 0);

                if (filas.Count == 0)
                {
                    TempData["Error"] = "No hay estudiantes matriculados en el grupo (o no tiene acceso a él).";
                    return RedirectToAction("Historial", new { grupo = grupo });
                }

                string nombre = "Notas_" + ReporteNotas.NombreArchivo(filas[0].Grupo) + "_" + filas[0].Anio + ".xlsx";
                return File(ReporteNotas.LibroGrupo(filas), TipoXlsx, nombre);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo exportar: " + ex.Message;
                return RedirectToAction("Historial", new { grupo = grupo });
            }
        }

        // Un estudiante: sus datos, nota final de cada materia por trimestre y el detalle de evaluaciones
        public ActionResult ExportarEstudiante(int grupo = 0, int estudiante = 0)
        {
            try
            {
                int alcance = AlcanceLectura();

                if (alcance < 0 || grupo <= 0 || estudiante <= 0)
                {
                    TempData["Error"] = "Seleccione un grupo y un estudiante para exportar.";
                    return RedirectToAction("Historial", new { grupo = grupo });
                }

                var filas = ReporteNotas.Obtener(alcance, grupo, estudiante);

                if (filas.Count == 0)
                {
                    TempData["Error"] = "No se encontró al estudiante en el grupo (o no tiene acceso a él).";
                    return RedirectToAction("Historial", new { grupo = grupo });
                }

                string nombre = "Notas_" + ReporteNotas.NombreArchivo(filas[0].Estudiante) + ".xlsx";
                return File(ReporteNotas.LibroEstudiante(filas), TipoXlsx, nombre);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo exportar: " + ex.Message;
                return RedirectToAction("Historial", new { grupo = grupo });
            }
        }

        // ---------------------------------------------------------------
        // IMPORTAR EXCEL
        // ---------------------------------------------------------------
        private static string Celda(object v)
        {
            if (v == null || v == DBNull.Value) return "";
            if (v is double) return ((double)v).ToString("0.########", CultureInfo.InvariantCulture);
            return Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
        }

        private static string Clave(string texto)
        {
            string n = (texto ?? "").Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in n)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static DataTable LeerExcel(HttpPostedFileBase archivo)
        {
            using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(archivo.InputStream))
            {
                var ds = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataReader.ExcelDataTableConfiguration { UseHeaderRow = true }
                });
                return ds.Tables.Count > 0 ? ds.Tables[0] : null;
            }
        }

        private static int Columna(DataTable t, string nombre)
        {
            for (int i = 0; i < t.Columns.Count; i++)
            {
                if (Clave(t.Columns[i].ColumnName) == Clave(nombre)) return i;
            }
            return -1;
        }

        private ImportarViewModel CargarImportar(int? grupo, string asignatura, int trimestre)
        {
            var vm = new ImportarViewModel { Trimestre = (trimestre < 1 || trimestre > 3) ? 1 : trimestre };
            int idDocente = IdDocente();
            vm.Grupos = repo.ListarGrupos(idDocente);

            if (vm.Grupos.Count > 0)
            {
                var actual = vm.Grupos.FirstOrDefault(g => g.IdGrupo == grupo) ?? vm.Grupos[0];
                vm.IdGrupo = actual.IdGrupo;
                vm.Asignaturas = repo.ListarAsignaturas(vm.IdGrupo);
                vm.Asignatura = !string.IsNullOrWhiteSpace(asignatura) ? asignatura : (vm.Asignaturas.FirstOrDefault() ?? "");
                vm.Evaluaciones = repo.ListarEvaluaciones(vm.IdGrupo, vm.Asignatura, vm.Trimestre);
            }
            return vm;
        }

        [HttpGet]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult ImportarExcel(int? grupo, string asignatura, int trimestre = 1)
        {
            if (IdDocente() < 0)
            {
                TempData["Error"] = "Su usuario no tiene un registro de docente ni permiso para registrar notas.";
                return RedirectToAction("Index");
            }

            try { return View(CargarImportar(grupo, asignatura, trimestre)); }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new ImportarViewModel());
            }
        }

        // Plantilla: Identificacion | <una columna por evaluación del grupo, con el nombre exacto>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult ImportarEscuela(int idGrupo, string asignatura, int trimestre, HttpPostedFileBase archivo)
        {
            int idDocente = IdDocente();
            var vm = new ImportarViewModel();

            try
            {
                vm = CargarImportar(idGrupo, asignatura, trimestre);

                if (idDocente < 0 || archivo == null || archivo.ContentLength == 0)
                {
                    vm.Errores.Add("Seleccione un archivo .xlsx.");
                    return View("ImportarExcel", vm);
                }

                var tabla = LeerExcel(archivo);
                int cId = tabla == null ? -1 : Columna(tabla, "Identificacion");

                if (cId < 0)
                {
                    vm.Errores.Add("El archivo debe tener una columna llamada Identificacion en la primera fila.");
                    return View("ImportarExcel", vm);
                }

                // columna -> evaluación (por nombre)
                var cols = new Dictionary<int, EvaluacionItem>();
                foreach (var ev in vm.Evaluaciones)
                {
                    int c = Columna(tabla, ev.Nombre);
                    if (c >= 0) cols[c] = ev;
                }

                if (cols.Count == 0)
                {
                    vm.Errores.Add("Ninguna columna coincide con las evaluaciones del grupo (" +
                        string.Join(", ", vm.Evaluaciones.Select(e => e.Nombre)) + ").");
                    return View("ImportarExcel", vm);
                }

                var estudiantes = repo.BuscarEstudiantes(idGrupo);
                var lote = new List<NotaGuardar>();

                for (int f = 0; f < tabla.Rows.Count; f++)
                {
                    var fila = tabla.Rows[f];
                    string ident = Celda(fila[cId]);
                    if (ident == "") continue;

                    int idEst;
                    if (!estudiantes.TryGetValue(NotasRepository.NormalizarId(ident), out idEst))
                    {
                        vm.Errores.Add("Fila " + (f + 2) + ": la identificación " + ident + " no está matriculada en este grupo.");
                        continue;
                    }

                    foreach (var par in cols)
                    {
                        string txt = Celda(fila[par.Key]);
                        if (txt == "") continue;

                        decimal valor;
                        if (!TryParseNumero(txt, out valor) || valor < 0 || valor > par.Value.NotaMaxima)
                        {
                            vm.Errores.Add("Fila " + (f + 2) + ", " + par.Value.Nombre + ": nota inválida (" + txt + "), debe estar entre 0 y " + par.Value.NotaMaxima.ToString("0.##", CultureInfo.InvariantCulture) + ".");
                            continue;
                        }

                        lote.Add(new NotaGuardar { IdEvaluacion = par.Value.IdEvaluacion, IdEstudiante = idEst, Nota = valor });
                    }
                }

                if (vm.Errores.Count > 0)
                {
                    vm.Errores.Insert(0, "No se guardó nada. Corrija el archivo y vuelva a subirlo:");
                    return View("ImportarExcel", vm);
                }

                string resultado = repo.GuardarLote(IdUsuario(), idDocente, lote);

                if (resultado == NotasRepository.Ok)
                {
                    TempData["Success"] = "Se importaron " + lote.Count + " notas.";
                    return RedirectToAction("Registrar", new { grupo = idGrupo, asignatura = asignatura, trimestre = trimestre });
                }

                vm.Errores.Add(resultado);
                return View("ImportarExcel", vm);
            }
            catch (Exception ex)
            {
                vm.Errores.Add("No se pudo leer el archivo: " + ex.Message);
                return View("ImportarExcel", vm);
            }
        }

        // Plantilla: Identificacion | Escuela | Asignatura | Periodo | Anio | Nota   (estudiantes que vienen de otra escuela)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [NotasAcceso(SoloDocente = true)]
        public ActionResult ImportarExterna(HttpPostedFileBase archivo)
        {
            int idDocente = IdDocente();
            var vm = new ImportarViewModel();

            try
            {
                vm = CargarImportar(null, null, 1);

                if (idDocente < 0 || archivo == null || archivo.ContentLength == 0)
                {
                    vm.Errores.Add("Seleccione un archivo .xlsx.");
                    return View("ImportarExcel", vm);
                }

                var tabla = LeerExcel(archivo);
                string[] req = { "Identificacion", "Escuela", "Asignatura", "Periodo", "Anio", "Nota" };
                int[] c = new int[req.Length];

                for (int i = 0; i < req.Length; i++)
                {
                    c[i] = tabla == null ? -1 : Columna(tabla, req[i]);
                    if (c[i] < 0) vm.Errores.Add("Falta la columna " + req[i] + ".");
                }

                if (vm.Errores.Count > 0) return View("ImportarExcel", vm);

                var estudiantes = repo.BuscarEstudiantes(0);
                var lote = new List<NotaExterna>();

                for (int f = 0; f < tabla.Rows.Count; f++)
                {
                    var fila = tabla.Rows[f];
                    string ident = Celda(fila[c[0]]);
                    if (ident == "") continue;

                    string pre = "Fila " + (f + 2) + ": ";
                    int idEst, anio;
                    decimal nota;
                    string escuela = Celda(fila[c[1]]), asig = Celda(fila[c[2]]), periodo = Celda(fila[c[3]]);

                    if (!estudiantes.TryGetValue(NotasRepository.NormalizarId(ident), out idEst))
                        vm.Errores.Add(pre + "la identificación " + ident + " no existe en el sistema.");
                    else if (escuela == "" || asig == "" || periodo == "")
                        vm.Errores.Add(pre + "Escuela, Asignatura y Periodo son obligatorios.");
                    else if (!int.TryParse(Celda(fila[c[4]]), NumberStyles.Integer, CultureInfo.InvariantCulture, out anio) || anio < 1990 || anio > DateTime.Now.Year)
                        vm.Errores.Add(pre + "el año no es válido.");
                    else if (!TryParseNumero(Celda(fila[c[5]]), out nota) || nota < 0 || nota > 100)
                        vm.Errores.Add(pre + "la nota debe estar entre 0 y 100.");
                    else
                        lote.Add(new NotaExterna { IdEstudiante = idEst, Escuela = escuela, Asignatura = asig, Periodo = periodo, Anio = anio, Nota = nota });
                }

                if (vm.Errores.Count > 0)
                {
                    vm.Errores.Insert(0, "No se guardó nada. Corrija el archivo y vuelva a subirlo:");
                    return View("ImportarExcel", vm);
                }

                string resultado = repo.GuardarExternas(IdUsuario(), idDocente, lote);

                if (resultado == NotasRepository.Ok)
                {
                    TempData["Success"] = "Se importaron " + lote.Count + " notas de escuelas anteriores.";
                    return RedirectToAction("Historial");
                }

                vm.Errores.Add(resultado);
                return View("ImportarExcel", vm);
            }
            catch (Exception ex)
            {
                vm.Errores.Add("No se pudo leer el archivo: " + ex.Message);
                return View("ImportarExcel", vm);
            }
        }
    }
}
