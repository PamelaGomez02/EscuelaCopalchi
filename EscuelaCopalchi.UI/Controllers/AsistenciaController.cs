
using EscuelaCopalchi.UI.Models.GestionAsistencia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class AsistenciaController : Controller
    {
        private readonly AsistenciaRepository repo =
            new AsistenciaRepository();

        // Contexto académico del usuario actual.
        // No realiza comprobaciones de roles aquí.
        private AsistenciaSesion Sesion
        {
            get
            {
                return AsistenciaSesion.Actual(HttpContext);
            }
        }

        private static bool TryFecha(
            string texto,
            out DateTime fecha)
        {
            return DateTime.TryParseExact(
                (texto ?? "").Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha);
        }

        // =============================================
        // CONSULTAR ASISTENCIA
        // =============================================
        public ActionResult Index(int? grupo, string fecha)
        {
            var vm = new AsistenciaIndexVM
            {
                Fecha = DateTime.Today,
                PuedeEditar = Sesion.PuedeRegistrar
            };

            try
            {
                vm.Grupos = repo.ListarGrupos(
                    Sesion.AlcanceLectura);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);

                return View(vm);
            }

            if (Request.QueryString.Count == 0)
                return View(vm);

            DateTime f;

            if (string.IsNullOrWhiteSpace(fecha))
            {
                ViewBag.Error = "La fecha es obligatoria.";
                return View(vm);
            }

            if (!TryFecha(fecha, out f))
            {
                ViewBag.Error = "La fecha no es válida.";
                return View(vm);
            }

            vm.Fecha = f;

            if (!grupo.HasValue || grupo.Value <= 0)
            {
                ViewBag.Error = "Debe seleccionar un grupo.";
                return View(vm);
            }

            vm.IdGrupo = grupo.Value;

            try
            {
                vm.Filas = repo.Consultar(
                    grupo.Value,
                    f,
                    Sesion.AlcanceLectura);

                vm.Resumen =
                    ResumenAsistencia.De(vm.Filas);

                var g = vm.Grupos.FirstOrDefault(
                    x => x.IdGrupo == grupo.Value);

                vm.NombreGrupo =
                    g == null ? "" : g.Grupo;

                vm.Consultado = true;
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);
            }

            return View(vm);
        }

        // =============================================
        // REGISTRAR ASISTENCIA - GET
        // =============================================
        [HttpGet]
        public ActionResult Registrar(
            int? grupo,
            string fecha)
        {
            return FormularioRegistrar(
                grupo,
                fecha,
                Request.QueryString.Count > 0,
                null);
        }

        // =============================================
        // REGISTRAR ASISTENCIA - POST
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(
            int? grupo,
            string fecha,
            List<AsistenciaGuardarItem> filas)
        {
            DateTime f;

            if (!grupo.HasValue ||
                grupo.Value <= 0 ||
                !TryFecha(fecha, out f))
            {
                return FormularioRegistrar(
                    grupo,
                    fecha,
                    true,
                    filas);
            }

            string resultado;

            try
            {
                resultado = repo.Registrar(
                    Sesion.IdUsuario,
                    Sesion.AlcanceEscritura,
                    grupo.Value,
                    f,
                    filas);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);

                return FormularioRegistrar(
                    grupo,
                    fecha,
                    true,
                    filas);
            }

            if (resultado == AsistenciaRepository.Ok)
            {
                int total =
                    filas == null ? 0 : filas.Count;

                TempData["Exito"] =
                    "Asistencia registrada correctamente (" +
                    total + " estudiantes).";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        grupo = grupo.Value,
                        fecha = f.ToString("yyyy-MM-dd")
                    });
            }

            ViewBag.Error = resultado;

            return FormularioRegistrar(
                grupo,
                fecha,
                true,
                filas);
        }

        // =============================================
        // FORMULARIO DE REGISTRO
        // =============================================
        private ActionResult FormularioRegistrar(
            int? grupo,
            string fecha,
            bool cargar,
            List<AsistenciaGuardarItem> enviados)
        {
            var vm = new AsistenciaRegistrarVM
            {
                Fecha = DateTime.Today
            };

            int docente = Sesion.AlcanceEscritura;

            try
            {
                vm.Grupos = repo.ListarGrupos(docente);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);

                return View("Registrar", vm);
            }

            if (!cargar)
                return View("Registrar", vm);

            DateTime f;

            if (string.IsNullOrWhiteSpace(fecha))
            {
                ViewBag.Error = "La fecha es obligatoria.";
                return View("Registrar", vm);
            }

            if (!TryFecha(fecha, out f))
            {
                ViewBag.Error = "La fecha no es válida.";
                return View("Registrar", vm);
            }

            vm.Fecha = f;

            if (!grupo.HasValue || grupo.Value <= 0)
            {
                ViewBag.Error = "Debe seleccionar un grupo.";
                return View("Registrar", vm);
            }

            vm.IdGrupo = grupo.Value;

            try
            {
                vm.Filas = repo.ListaClase(
                    grupo.Value,
                    f,
                    docente);

                vm.YaRegistrada = vm.Filas.Any(
                    x => x.IdAsistencia.HasValue);

                vm.Cargada = true;

                if (enviados != null && !vm.YaRegistrada)
                {
                    foreach (var fila in vm.Filas)
                    {
                        var e = enviados.FirstOrDefault(
                            x => x.IdEstudiante ==
                                 fila.IdEstudiante);

                        if (e == null)
                            continue;

                        fila.Estado = e.Estado;
                        fila.HoraLlegada = e.HoraLlegada;
                        fila.Observacion = e.Observacion;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);
            }

            return View("Registrar", vm);
        }

        // =============================================
        // EDITAR ASISTENCIA - GET
        // =============================================
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un registro de asistencia.";

                return RedirectToAction("Index");
            }

            try
            {
                AsistenciaItem item = repo.Obtener(
                    id.Value,
                    Sesion.AlcanceEscritura);

                if (item == null)
                {
                    TempData["Error"] =
                        "El registro de asistencia no existe.";

                    return RedirectToAction("Index");
                }

                return View(item);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    AsistenciaRepository.Mensaje(ex);

                return RedirectToAction("Index");
            }
        }

        // =============================================
        // EDITAR ASISTENCIA - POST
        // =============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            int? id,
            string estado,
            string hora,
            string observacion)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un registro de asistencia.";

                return RedirectToAction("Index");
            }

            AsistenciaItem item;

            try
            {
                item = repo.Obtener(
                    id.Value,
                    Sesion.AlcanceEscritura);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    AsistenciaRepository.Mensaje(ex);

                return RedirectToAction("Index");
            }

            if (item == null)
            {
                TempData["Error"] =
                    "El registro de asistencia no existe.";

                return RedirectToAction("Index");
            }

            string resultado;

            try
            {
                resultado = repo.Modificar(
                    Sesion.IdUsuario,
                    Sesion.AlcanceEscritura,
                    id.Value,
                    estado,
                    hora,
                    observacion);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);

                return View(item);
            }

            if (resultado == AsistenciaRepository.Ok)
            {
                TempData["Exito"] =
                    "Asistencia actualizada correctamente.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        grupo = item.IdGrupo,
                        fecha = item.Fecha.Value
                            .ToString("yyyy-MM-dd")
                    });
            }

            ViewBag.Error = resultado;

            item.Estado = estado;
            item.HoraLlegada = hora;
            item.Observacion = observacion;

            return View(item);
        }

        // =============================================
        // HISTORIAL DE ASISTENCIA
        // =============================================
        public ActionResult Historial(
            int? grupo,
            int? estudiante,
            string desde,
            string hasta)
        {
            var vm = new AsistenciaHistorialVM
            {
                IdGrupo = grupo ?? 0,
                IdEstudiante = estudiante ?? 0
            };

            int alcance = Sesion.AlcanceLectura;

            try
            {
                vm.Grupos = repo.ListarGrupos(alcance);
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);

                return View(vm);
            }

            DateTime d, h;

            if (!string.IsNullOrWhiteSpace(desde) &&
                TryFecha(desde, out d))
            {
                vm.Desde = d;
            }

            if (!string.IsNullOrWhiteSpace(hasta) &&
                TryFecha(hasta, out h))
            {
                vm.Hasta = h;
            }

            if (Request.QueryString.Count == 0)
                return View(vm);

            if (vm.IdGrupo <= 0)
            {
                ViewBag.Error =
                    "Debe seleccionar un grupo.";

                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(desde) &&
                !vm.Desde.HasValue)
            {
                ViewBag.Error =
                    "La fecha inicial no es válida.";

                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(hasta) &&
                !vm.Hasta.HasValue)
            {
                ViewBag.Error =
                    "La fecha final no es válida.";

                return View(vm);
            }

            try
            {
                vm.Estudiantes = repo.ListaClase(
                    vm.IdGrupo,
                    DateTime.Today,
                    alcance);

                var g = vm.Grupos.FirstOrDefault(
                    x => x.IdGrupo == vm.IdGrupo);

                vm.NombreGrupo =
                    g == null ? "" : g.Grupo;

                var todas = repo.Historial(
                    vm.IdGrupo,
                    vm.IdEstudiante,
                    vm.Desde,
                    vm.Hasta,
                    alcance);

                vm.Resumen =
                    ResumenAsistencia.De(todas);

                vm.TotalFilas = todas.Count;

                vm.Filas = todas
                    .Take(AsistenciaHistorialVM.MaxFilas)
                    .ToList();

                if (vm.IdEstudiante > 0)
                {
                    var e = vm.Estudiantes.FirstOrDefault(
                        x => x.IdEstudiante ==
                             vm.IdEstudiante)
                        ?? todas.FirstOrDefault();

                    vm.NombreEstudiante =
                        e == null ? "" : e.Estudiante;
                }

                vm.Consultado = true;
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    AsistenciaRepository.Mensaje(ex);
            }

            return View(vm);
        }
    }
}
