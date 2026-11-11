using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Estudiantes;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class EstudiantesController : Controller
    {
        private readonly EstudianteRepository repository;

        public EstudiantesController()
        {
            repository = new EstudianteRepository();
        }



        // Permisos administrados desde Roles -> Asignar permisos.
        // El identificador se obtiene de la sesión, nunca del navegador.
        private bool TienePermiso(string permiso)
        {
            var sesion = SesionUsuario.Obtener(Session);
            if (sesion == null || sesion.IdUsuario <= 0) return false;
            var db = new ConexionBD();
            int id = sesion.IdUsuario;
            string nombre = permiso.Replace("'", "''");
            var ds = db.EjecutarConsulta(@"SELECT TOP (1) 1 AS permitido
                FROM dbo.USUARIO u
                INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol = u.id_rol
                INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                WHERE u.id_usuario = " + id + @"
                  AND p.nombre = '" + nombre + @"' AND p.estado = 1");
            return ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0;
        }

        private ActionResult SinPermiso()
        {
            return RedirectToAction("AccesoDenegado", "Error");
        }

        private void PermisosVista()
        {
            ViewBag.PuedeVer = TienePermiso("VER_ESTUDIANTES");
            ViewBag.PuedeCrear = TienePermiso("CREAR_ESTUDIANTES");
            ViewBag.PuedeEditar = TienePermiso("EDITAR_ESTUDIANTES");
            ViewBag.PuedeImportar = TienePermiso("IMPORTAR_ESTUDIANTES");
            ViewBag.PuedeExpediente = TienePermiso("VER_EXPEDIENTE_ESTUDIANTES");
            ViewBag.PuedeBajas = TienePermiso("GESTIONAR_BAJAS_ESTUDIANTES");
            ViewBag.PuedeGestionarExpediente = TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES");
        }

        [HttpGet]
        public ActionResult Registrar()
        {
            if (!TienePermiso("CREAR_ESTUDIANTES")) return SinPermiso();
            return View("~/Views/Estudiantes/Crear.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(Estudiante estudiante)
        {
            if (!TienePermiso("CREAR_ESTUDIANTES")) return SinPermiso();
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = string.Join(
                        "<br>",
                        ModelState.Values
                                  .SelectMany(v => v.Errors)
                                  .Select(e => e.ErrorMessage));

                    ViewBag.Error = errores;

                    return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
                }

                string resultado =
                    repository.Guardar(estudiante);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Estudiante registrado correctamente.";

                    return RedirectToAction("Registrar");
                }

                ViewBag.Error = resultado;

                return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
            }
        }

        public ActionResult Index(string filtroEstado = "Todos", string busqueda = "", int pagina = 1)
        {
            if (!TienePermiso("VER_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            var todos = repository.ObtenerTodos() ?? new List<Estudiante>();
            ViewBag.TotalEstudiantes = todos.Count;
            ViewBag.TotalActivos = todos.Count(x => x.Estado);
            ViewBag.TotalInactivos = todos.Count(x => !x.Estado);

            var filtrosValidos = new[] { "Todos", "Activos", "Inactivos", "Adecuaciones", "Alergias" };
            if (!filtrosValidos.Contains(filtroEstado)) filtroEstado = "Todos";
            if ((filtroEstado == "Adecuaciones" || filtroEstado == "Alergias") &&
                !TienePermiso("VER_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();

            IEnumerable<Estudiante> estudiantes = todos;
            if (filtroEstado == "Activos") estudiantes = estudiantes.Where(x => x.Estado);
            else if (filtroEstado == "Inactivos") estudiantes = estudiantes.Where(x => !x.Estado);
            else if (filtroEstado == "Adecuaciones")
            {
                var ids = new HashSet<int>();
                foreach (var e in todos)
                    if ((repository.ObtenerAdecuaciones(e.IdEstudiante) ?? new List<AdecuacionAcademica>()).Any())
                        ids.Add(e.IdEstudiante);
                estudiantes = estudiantes.Where(x => ids.Contains(x.IdEstudiante));
            }
            else if (filtroEstado == "Alergias")
            {
                var ids = new HashSet<int>();
                foreach (var e in todos)
                    if ((repository.ObtenerAlergias(e.IdEstudiante) ?? new List<Alergia>()).Any())
                        ids.Add(e.IdEstudiante);
                estudiantes = estudiantes.Where(x => ids.Contains(x.IdEstudiante));
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string q = busqueda.Trim();
                estudiantes = estudiantes.Where(x =>
                    ((x.Nombre ?? "") + " " + (x.Apellido1 ?? "") + " " + (x.Apellido2 ?? ""))
                        .IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (x.Identificacion ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (x.NombreEncargado ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var ordenados = estudiantes.OrderBy(x => x.Apellido1)
                .ThenBy(x => x.Apellido2).ThenBy(x => x.Nombre).ToList();
            int total = ordenados.Count;
            int paginas = Math.Max(1, (int)Math.Ceiling(total / 10.0));
            pagina = Math.Max(1, Math.Min(pagina, paginas));
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = paginas;
            ViewBag.TotalFiltrados = total;
            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.Busqueda = busqueda ?? "";
            return View("~/Views/Estudiantes/Index.cshtml",
                ordenados.Skip((pagina - 1) * 10).Take(10).ToList());
        }


        // Consulta de estudiantes que tienen al menos una adecuación registrada.
        // Reutiliza los procedimientos existentes y respeta los permisos del expediente.
        [HttpGet]
        public ActionResult Adecuaciones(string busqueda = "", string filtroEstado = "Todos", int? tipo = null, int pagina = 1)
        {
            if (!TienePermiso("VER_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            PermisosVista();

            var estudiantes = repository.ObtenerTodos();
            var mapa = new Dictionary<int, List<AdecuacionAcademica>>();
            foreach (var estudiante in estudiantes)
            {
                var adecuaciones = repository.ObtenerAdecuaciones(estudiante.IdEstudiante);
                if (adecuaciones != null && adecuaciones.Count > 0)
                    mapa[estudiante.IdEstudiante] = adecuaciones;
            }

            var conAdecuaciones = estudiantes
                .Where(e => mapa.ContainsKey(e.IdEstudiante)).ToList();
            ViewBag.TotalConAdecuaciones = conAdecuaciones.Count;
            ViewBag.TotalActivos = conAdecuaciones.Count(e => e.Estado);
            ViewBag.TotalInactivos = conAdecuaciones.Count(e => !e.Estado);
            ViewBag.TiposAdecuacion = repository.ObtenerTiposAdecuacion();
            ViewBag.AdecuacionesPorEstudiante = mapa;
            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.FiltroEstado = filtroEstado ?? "Todos";
            ViewBag.TipoSeleccionado = tipo;

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var q = busqueda.Trim();
                conAdecuaciones = conAdecuaciones.Where(e =>
                    ((e.Nombre ?? "") + " " + (e.Apellido1 ?? "") + " " + (e.Apellido2 ?? ""))
                        .IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.Identificacion ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }
            if (string.Equals(filtroEstado, "Activos", StringComparison.OrdinalIgnoreCase))
                conAdecuaciones = conAdecuaciones.Where(e => e.Estado).ToList();
            else if (string.Equals(filtroEstado, "Inactivos", StringComparison.OrdinalIgnoreCase))
                conAdecuaciones = conAdecuaciones.Where(e => !e.Estado).ToList();
            if (tipo.HasValue && tipo.Value > 0)
                conAdecuaciones = conAdecuaciones
                    .Where(e => mapa[e.IdEstudiante].Any(a => a.TipoAdecuacion == tipo.Value)).ToList();

            conAdecuaciones = conAdecuaciones.OrderBy(x => x.Apellido1).ThenBy(x => x.Apellido2).ThenBy(x => x.Nombre).ToList();
            int totalFiltrados = conAdecuaciones.Count;
            int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrados / 10.0));
            pagina = Math.Max(1, Math.Min(pagina, totalPaginas));
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalFiltrados = totalFiltrados;
            conAdecuaciones = conAdecuaciones.Skip((pagina - 1) * 10).Take(10).ToList();

            return View("~/Views/Estudiantes/Adecuaciones.cshtml", conAdecuaciones);
        }

        // Listado de estudiantes con alergias registradas en el expediente.
        [HttpGet]
        public ActionResult Alergias(string busqueda = "", string filtroEstado = "Todos", int pagina = 1)
        {
            if (!TienePermiso("VER_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            PermisosVista();

            var estudiantes = repository.ObtenerTodos();
            var mapa = new Dictionary<int, List<Alergia>>();
            foreach (var estudiante in estudiantes)
            {
                var alergias = repository.ObtenerAlergias(estudiante.IdEstudiante);
                if (alergias != null && alergias.Count > 0)
                    mapa[estudiante.IdEstudiante] = alergias;
            }

            var conAlergias = estudiantes.Where(e => mapa.ContainsKey(e.IdEstudiante)).ToList();
            ViewBag.TotalConAlergias = conAlergias.Count;
            ViewBag.TotalActivos = conAlergias.Count(e => e.Estado);
            ViewBag.TotalInactivos = conAlergias.Count(e => !e.Estado);
            ViewBag.AlergiasPorEstudiante = mapa;
            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.FiltroEstado = filtroEstado ?? "Todos";

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var q = busqueda.Trim();
                conAlergias = conAlergias.Where(e =>
                    ((e.Nombre ?? "") + " " + (e.Apellido1 ?? "") + " " + (e.Apellido2 ?? ""))
                        .IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (e.Identificacion ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }
            if (string.Equals(filtroEstado, "Activos", StringComparison.OrdinalIgnoreCase))
                conAlergias = conAlergias.Where(e => e.Estado).ToList();
            else if (string.Equals(filtroEstado, "Inactivos", StringComparison.OrdinalIgnoreCase))
                conAlergias = conAlergias.Where(e => !e.Estado).ToList();

            conAlergias = conAlergias.OrderBy(x => x.Apellido1).ThenBy(x => x.Apellido2).ThenBy(x => x.Nombre).ToList();
            int totalFiltrados = conAlergias.Count;
            int totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrados / 10.0));
            pagina = Math.Max(1, Math.Min(pagina, totalPaginas));
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalFiltrados = totalFiltrados;
            conAlergias = conAlergias.Skip((pagina - 1) * 10).Take(10).ToList();

            return View("~/Views/Estudiantes/Alergias.cshtml", conAlergias);
        }

        public ActionResult Detalle(int id)
        {
            if (!TienePermiso("VER_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            return View(
                "~/Views/Estudiantes/Detalle.cshtml",
                estudiante);
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            if (!TienePermiso("EDITAR_ESTUDIANTES")) return SinPermiso();
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            return View(
                "~/Views/Estudiantes/Editar.cshtml",
                estudiante);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Estudiante estudiante)
        {
            if (!TienePermiso("EDITAR_ESTUDIANTES")) return SinPermiso();
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(
                        "~/Views/Estudiantes/Editar.cshtml",
                        estudiante);
                }

                string resultado =
                    repository.Actualizar(estudiante);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Estudiante actualizado correctamente.";

                    return RedirectToAction("Index");
                }

                ViewBag.Error = resultado;

                return View(
                    "~/Views/Estudiantes/Editar.cshtml",
                    estudiante);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;

                return View(
                    "~/Views/Estudiantes/Editar.cshtml",
                    estudiante);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DarBaja(int id, string observacionBaja)
        {
            if (!TienePermiso("GESTIONAR_BAJAS_ESTUDIANTES")) return SinPermiso();
            try
            {
                string resultado = repository.DarBaja(id, observacionBaja);
                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] = "El estudiante fue dado de baja correctamente. Ahora aparece en Inactivos.";
                    return RedirectToAction("Index", new { filtroEstado = "Inactivos" });
                }
                TempData["Error"] = string.IsNullOrWhiteSpace(resultado)
                    ? "No se pudo confirmar la baja del estudiante." : resultado;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo dar de baja al estudiante: " + ex.Message;
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reactivar(int id)
        {
            if (!TienePermiso("GESTIONAR_BAJAS_ESTUDIANTES")) return SinPermiso();
            try
            {
                var estudiante = repository.ObtenerPorId(id);
                if (estudiante == null)
                {
                    TempData["Error"] = "No se encontró al estudiante.";
                    return RedirectToAction("Index", new { filtroEstado = "Inactivos" });
                }
                if (estudiante.Estado)
                {
                    TempData["Error"] = "El estudiante ya se encuentra activo.";
                    return RedirectToAction("Index", new { filtroEstado = "Activos" });
                }
                string resultado = repository.Reactivar(id);
                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] = "El estudiante fue reactivado correctamente. Se conserva su historial de bajas.";
                    return RedirectToAction("Index", new { filtroEstado = "Activos" });
                }
                TempData["Error"] = string.IsNullOrWhiteSpace(resultado)
                    ? "No se pudo confirmar la reactivación." : resultado;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo reactivar al estudiante: " + ex.Message;
            }
            return RedirectToAction("Index", new { filtroEstado = "Inactivos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAdecuacion(
        AdecuacionAcademica adecuacion)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            try
            {
                string resultado =
                    repository.GuardarAdecuacion(
                        adecuacion);
                //throw new Exception(resultado);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Adecuación registrada correctamente.";
                }
                else
                {
                    TempData["Error"] =
                        resultado;
                }

                return RedirectToAction(
                    "Expediente",
                    new
                    {
                        id = adecuacion.IdEstudiante
                    });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;

                return RedirectToAction(
                    "Expediente",
                    new
                    {
                        id = adecuacion.IdEstudiante
                    });
            }
        }


        [HttpGet]
        public ActionResult Expediente(int id)
        {
            if (!TienePermiso("VER_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            ViewBag.TiposAdecuacion =
                repository.ObtenerTiposAdecuacion();

            ViewBag.Adecuaciones =
                repository.ObtenerAdecuaciones(id);

            ViewBag.Alergias =
                repository.ObtenerAlergias(id);

            ViewBag.Observaciones =
                repository.ObtenerObservaciones(id);

            return View(
                "~/Views/Estudiantes/Expediente.cshtml",
                estudiante);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAdecuacion(
    AdecuacionAcademica adecuacion)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            string resultado =
                repository.ActualizarAdecuacion(
                    adecuacion);

            if (resultado == "Se ha guardado correctamente")
            {
                TempData["Success"] =
                    "Adecuación actualizada correctamente.";
            }
            else
            {
                TempData["Error"] =
                    resultado;
            }

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = adecuacion.IdEstudiante
                });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAlergia(
        Alergia alergia)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            string resultado =
                repository.GuardarAlergia(
                    alergia);

            if (resultado == "Se ha guardado correctamente")
            {
                TempData["Success"] =
                    "Alergia registrada correctamente.";
            }
            else
            {
                TempData["Error"] =
                    resultado;
            }

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = alergia.IdEstudiante
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarAlergia(Alergia alergia)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            string resultado =
                repository.ActualizarAlergia(alergia);

            TempData["Success"] =
                "Alergia actualizada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = alergia.IdEstudiante
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarAlergia(int idAlergia, int idEstudiante)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            repository.EliminarAlergia(idAlergia);

            TempData["Success"] =
                "Alergia eliminada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = idEstudiante
                });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarObservacion()
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            Observaciones observacion =
                new Observaciones();

            observacion.IdEstudiante =
                Convert.ToInt32(
                    Request.Form["IdEstudiante"]);

            observacion.Titulo =
                Request.Form["Titulo"];

            observacion.Observacion =
                Request.Form["Observacion"];

            string resultado =
                repository.GuardarObservacion(
                    observacion);

            TempData["Success"] =
                "Observación registrada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = observacion.IdEstudiante
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarObservacion(
    FormCollection form)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            Observaciones observacion =
                new Observaciones();

            observacion.IdObservacion =
                Convert.ToInt32(
                    form["IdObservacion"]);

            observacion.IdEstudiante =
                Convert.ToInt32(
                    form["IdEstudiante"]);

            observacion.Titulo =
                form["Titulo"];

            observacion.Observacion =
                form["Observacion"];

            repository.ActualizarObservacion(
                observacion);

            TempData["Success"] =
                "Observación actualizada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = observacion.IdEstudiante
                });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarObservacion(
        int idObservacion,
        int idEstudiante)
        {
            if (!TienePermiso("EDITAR_EXPEDIENTE_ESTUDIANTES")) return SinPermiso();
            repository.EliminarObservacion(
                idObservacion);

            TempData["Success"] =
                "Observación eliminada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = idEstudiante
                });
        }


        public ActionResult HistorialBajas()
        {
            if (!TienePermiso("GESTIONAR_BAJAS_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            var lista =
                repository.ObtenerHistorialBajas();

            if (lista == null)
            {
                throw new Exception(
                    "ObtenerHistorialBajas devolvió NULL");
            }

            ViewBag.HistorialBajas = lista;
            ViewBag.TotalBajas = lista.Count;

            return View(
                "~/Views/Estudiantes/HistorialBajas.cshtml");
        }


        [HttpGet]
        public ActionResult ImportarExcel()
        {
            if (!TienePermiso("IMPORTAR_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            ViewBag.Preview =
                Session["EstudiantesPreview"];

            return View(
                "~/Views/Estudiantes/ImportarExcel.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportarExcel(HttpPostedFileBase archivoExcel, string accion)
        {
            if (!TienePermiso("IMPORTAR_ESTUDIANTES")) return SinPermiso();
            PermisosVista();
            if (accion == "Cargar")
            {
                if (archivoExcel == null)
                {
                    TempData["Error"] = "Seleccione un archivo Excel.";

                    return View("~/Views/Estudiantes/ImportarExcel.cshtml");
                }

                List<Estudiante> preview = new List<Estudiante>();

                using (var stream = archivoExcel.InputStream)
                {
                    using (var reader =
                        ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet ds = reader.AsDataSet();

                        DataTable tabla = ds.Tables[0];

                        for (int fila = 1; fila < tabla.Rows.Count; fila++)
                        {
                            DataRow row = tabla.Rows[fila];

                            Estudiante estudiante = new Estudiante();

                            estudiante.Nombre = row[0].ToString();
                            estudiante.Apellido1 = row[1].ToString();
                            estudiante.Apellido2 = row[2].ToString();
                            estudiante.Identificacion = row[3].ToString();
                            estudiante.FechaNacimiento = Convert.ToDateTime(row[4]);
                            estudiante.NombreEncargado = row[5].ToString();
                            estudiante.Parentesco = row[6].ToString();
                            estudiante.TelefonoEncargado = row[7].ToString();
                            estudiante.CorreoEncargado = row[8].ToString();
                            estudiante.Direccion = row[9].ToString();
                            estudiante.Estado = true;

                            string errores = "";

                            if (string.IsNullOrWhiteSpace(estudiante.Identificacion))
                            {
                                errores += "Identificación requerida. ";
                            }

                            if (repository.ExisteIdentificacion(estudiante.Identificacion))
                            {
                                errores += "El estudiante ya existe en el sistema. ";
                            }

                            if (string.IsNullOrWhiteSpace(estudiante.Nombre))
                            {
                                errores += "Nombre requerido. ";
                            }

                            if (string.IsNullOrWhiteSpace(estudiante.Apellido1))
                            {
                                errores += "Primer apellido requerido. ";
                            }

                            if (string.IsNullOrWhiteSpace(estudiante.NombreEncargado))
                            {
                                errores += "Encargado requerido. ";
                            }

                            if (string.IsNullOrWhiteSpace(estudiante.TelefonoEncargado))
                            {
                                errores += "Teléfono requerido. ";
                            }

                            if (string.IsNullOrEmpty(errores))
                            {
                                estudiante.EsValido = true;
                                estudiante.EstadoImportacion = "Válido";
                            }
                            else
                            {
                                estudiante.EsValido = false;
                                estudiante.EstadoImportacion = errores;
                            }

                            preview.Add(estudiante);
                        }
                    }
                }

                Session["EstudiantesPreview"] = preview;

                ViewBag.Preview = preview;

                return View("~/Views/Estudiantes/ImportarExcel.cshtml");
            }

            if (accion == "Importar")
            {
                var estudiantes =
                    Session["EstudiantesPreview"] as List<Estudiante>;

                if (estudiantes == null || estudiantes.Count == 0)
                {
                    TempData["Error"] =
                        "Debe cargar un archivo Excel y visualizar los datos antes de importarlos.";

                    return RedirectToAction(
                        "ImportarExcel");
                }

                if (estudiantes == null)
                {
                    TempData["Error"] =
                        "Debe cargar un archivo antes de importar.";

                    return RedirectToAction("ImportarExcel");
                }

                int guardados = 0;

                foreach (var estudiante in estudiantes)
                {
                    if (!estudiante.EsValido)
                    {
                        continue;
                    }

                    if (repository.ExisteIdentificacion(
                        estudiante.Identificacion))
                    {
                        continue;
                    }

                    string resultado = repository.Guardar(estudiante);
                    if (resultado == "Se ha guardado correctamente") guardados++;
                }

                Session.Remove("EstudiantesPreview");

                TempData["Success"] =
                    guardados + " estudiantes importados correctamente.";

                return RedirectToAction("Index");
            }

            return View("~/Views/Estudiantes/ImportarExcel.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarImportacion()
        {
            if (!TienePermiso("IMPORTAR_ESTUDIANTES")) return SinPermiso();
            try
            {
                var lista = Session["EstudiantesPreview"]
                    as List<Estudiante>;

                if (lista == null || !lista.Any())
                {
                    TempData["Error"] =
                        "No hay estudiantes para importar.";

                    return RedirectToAction("ImportarExcel");
                }

                int guardados = 0;

                foreach (var estudiante in lista)
                {
                    if (!estudiante.EsValido || repository.ExisteIdentificacion(estudiante.Identificacion))
                        continue;
                    string resultado = repository.Guardar(estudiante);

                    if (resultado == "Se ha guardado correctamente")
                    {
                        guardados++;
                    }
                }

                Session.Remove("EstudiantesPreview");

                TempData["Success"] =
                    $"{guardados} estudiantes fueron importados correctamente.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ImportarExcel");
            }
        }
    }
}


