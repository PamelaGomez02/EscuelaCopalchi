
using EscuelaCopalchi.UI.Models.GestionAsistencia;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class AsistenciaController : Controller
    {
        private readonly AsistenciaRepository repo =
            new AsistenciaRepository();
        private static bool TryFecha(string texto, out DateTime fecha)
        {
            fecha = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            string[] formatos =
            {
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d/M/yyyy"
    };

            return DateTime.TryParseExact(
                texto.Trim(),
                formatos,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha);
        }
        // Los permisos provienen del login general y de Roles -> Asignar.
        // No se utiliza el permiso general como sustituto de los individuales.
        private bool TienePermiso(string nombre)
        {
            var usuario = SesionUsuario.Obtener(Session);
            return usuario != null && usuario.TienePermiso(nombre);
        }

        private ActionResult SinPermiso()
        {
            return RedirectToAction("AccesoDenegado", "Error");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            PermisosVista();
        }

        private void PermisosVista()
        {
            ViewBag.PuedeConsultarAsistencia = TienePermiso(PermisosAsistencia.Consultar);
            ViewBag.PuedeRegistrarAsistencia = TienePermiso(PermisosAsistencia.Registrar);
            ViewBag.PuedeEditarAsistencia = TienePermiso(PermisosAsistencia.Editar);
            ViewBag.PuedeVerHistorialAsistencia = TienePermiso(PermisosAsistencia.Historial);
            ViewBag.PuedeVerPorcentajeAsistencia = TienePermiso(PermisosAsistencia.Porcentaje);
        }

        // Identificador del usuario autenticado por el sistema general.
        private int IdUsuarioActual
        {
            get
            {
                var usuario = SesionUsuario.Obtener(Session);
                return usuario == null ? 0 : usuario.IdUsuario;
            }
        }

        // 0 significa todos los grupos en el repositorio existente.
        // Si la cuenta tiene docente asociado, se restringe a ese docente.
        // El permiso de la acción sigue validándose por separado.
        private int AlcanceLectura
        {
            get { return ObtenerAlcanceDocente(); }
        }

        private int AlcanceEscritura
        {
            get { return ObtenerAlcanceDocente(); }
        }

        private int ObtenerAlcanceDocente()
        {
            if (IdUsuarioActual <= 0)
                throw new UnauthorizedAccessException("La sesión no es válida.");

            var servicio = new UsuarioService();
            var usuario = servicio.Obtener(IdUsuarioActual);
            if (usuario == null || !usuario.Estado)
                throw new UnauthorizedAccessException("El usuario no está activo.");

            return usuario.IdDocente > 0 ? usuario.IdDocente : 0;
        }

        // Sesión general y docente asociado, sin AsistenciaSesion.


        private bool PuedeAccederAsistencia()
        {
            string[] permisos =
            {
        "CONSULTAR_ASISTENCIA",
        "REGISTRAR_ASISTENCIA",
        "EDITAR_ASISTENCIA",
        "VER_HISTORIAL_ASISTENCIA",
        "VER_PORCENTAJE_ASISTENCIA",
        "GESTIONAR_ASISTENCIA"
    };

            return permisos.Any(p => TienePermiso(p));
        }
        // =============================================
        // CONSULTAR ASISTENCIA
        // =============================================
        public ActionResult Index(int? grupo, string fecha)
        {
            if (!PuedeAccederAsistencia())
                return SinPermiso();
            var vm = new AsistenciaIndexVM
            {
                Fecha = DateTime.Today,
                PuedeEditar = TienePermiso(PermisosAsistencia.Editar)
            };

            try
            {
                vm.Grupos = repo.ListarGrupos(
                    AlcanceLectura);
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
                    AlcanceLectura);

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
            if (!TienePermiso(PermisosAsistencia.Registrar)) return SinPermiso();
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
            if (!TienePermiso(PermisosAsistencia.Registrar)) return SinPermiso();
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
                    IdUsuarioActual,
                    AlcanceEscritura,
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

            int docente = AlcanceEscritura;

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
            if (!TienePermiso(PermisosAsistencia.Editar)) return SinPermiso();
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
                    AlcanceEscritura);

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
            if (!TienePermiso(PermisosAsistencia.Editar)) return SinPermiso();
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
                    AlcanceEscritura);
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
                    IdUsuarioActual,
                    AlcanceEscritura,
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
            if (!TienePermiso(PermisosAsistencia.Historial)) return SinPermiso();
            var vm = new AsistenciaHistorialVM
            {
                IdGrupo = grupo ?? 0,
                IdEstudiante = estudiante ?? 0
            };

            int alcance = AlcanceLectura;

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

        // PORCENTAJE DE ASISTENCIA
        [HttpGet]
        public ActionResult Porcentaje(int? grupo, int? estudiante, string desde, string hasta)
        {
            if (!TienePermiso(PermisosAsistencia.Porcentaje)) return SinPermiso();

            var vm = new AsistenciaPorcentajeVM { IdGrupo = grupo ?? 0 };
            int alcance = AlcanceLectura;

            try
            {
                vm.Grupos = repo.ListarGrupos(alcance);
                DateTime fecha;
                if (!string.IsNullOrWhiteSpace(desde))
                {
                    if (!TryFecha(desde, out fecha))
                    {
                        ViewBag.Error = "La fecha inicial no es válida.";
                        return View(vm);
                    }
                    vm.Desde = fecha;
                }
                if (!string.IsNullOrWhiteSpace(hasta))
                {
                    if (!TryFecha(hasta, out fecha))
                    {
                        ViewBag.Error = "La fecha final no es válida.";
                        return View(vm);
                    }
                    vm.Hasta = fecha;
                }

                if (Request.QueryString.Count == 0) return View(vm);
                if (vm.IdGrupo <= 0)
                {
                    ViewBag.Error = "Debe seleccionar un grupo.";
                    return View(vm);
                }
                if (vm.Desde.HasValue && vm.Hasta.HasValue && vm.Desde > vm.Hasta)
                {
                    ViewBag.Error = "La fecha inicial no puede superar la final.";
                    return View(vm);
                }

                vm.Filas = repo.Porcentaje(vm.IdGrupo, estudiante ?? 0,
                    vm.Desde, vm.Hasta, alcance);
                vm.Resumen = ResumenAsistencia.De(vm.Filas);
                var seleccionado = vm.Grupos.FirstOrDefault(g => g.IdGrupo == vm.IdGrupo);
                vm.NombreGrupo = seleccionado == null ? "" : seleccionado.Grupo;
                vm.Consultado = true;
            }
            catch (Exception ex)
            {
                ViewBag.Error = AsistenciaRepository.Mensaje(ex);
            }
            return View(vm);
        }
    }
}
