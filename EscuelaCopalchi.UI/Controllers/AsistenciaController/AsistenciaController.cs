
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAsistencia;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
        // Los permisos se consultan desde las asignaciones reales del rol en SQL Server.
        // No se utiliza una copia posiblemente desactualizada de los permisos de sesión.
        private HashSet<string> ObtenerPermisosActuales()
        {
            const string claveCache = "Asistencia.PermisosActuales";
            var cache = HttpContext.Items[claveCache] as HashSet<string>;
            if (cache != null) return cache;

            var permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int idUsuario = IdUsuarioActual;
            if (idUsuario <= 0) return permisos;

            var conexion = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"];
            if (conexion == null)
                throw new ConfigurationErrorsException(
                    "No existe la cadena de conexión 'AulaVirtualCopalchi' en Web.config. " +
                    "Utilice aquí el nombre real de la conexión del proyecto.");

            const string sql = @"
                SELECT p.nombre
                FROM dbo.USUARIO AS u
                INNER JOIN dbo.ROL AS r ON r.id_rol = u.id_rol
                INNER JOIN dbo.ROL_PERMISO AS rp ON rp.id_rol = r.id_rol
                INNER JOIN dbo.PERMISO AS p ON p.id_permiso = rp.id_permiso
                WHERE u.id_usuario = @IdUsuario
                  AND u.estado = 1
                  AND r.estado = 1
                  AND p.estado = 1";

            using (var cn = new SqlConnection(conexion.ConnectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                cn.Open();
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        if (!lector.IsDBNull(0))
                            permisos.Add(lector.GetString(0).Trim());
                    }
                }
            }

            HttpContext.Items[claveCache] = permisos;
            return permisos;
        }

        private bool TienePermiso(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;
            return ObtenerPermisosActuales().Contains(nombre.Trim());
        }

        private ActionResult SinPermiso()
        {
            return RedirectToAction("AccesoDenegado", "Error");
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (filterContext.Result != null) return;
            try { PermisosVista(); }
            catch (Exception)
            {
                filterContext.Result = new HttpStatusCodeResult(500, "No se pudieron consultar los permisos de Asistencia.");
            }
        }

        private void PermisosVista()
        {
            ViewBag.PuedeConsultarAsistencia = PuedeLeerAsistencia();
            ViewBag.AlcanceGeneralAsistencia = TienePermiso("VER_ASISTENCIA_GENERAL");
            ViewBag.PuedeRegistrarAsistencia = TienePermiso(PermisosAsistencia.Registrar) && PuedeLeerAsistencia();
            ViewBag.PuedeEditarAsistencia = TienePermiso(PermisosAsistencia.Editar) && PuedeLeerAsistencia();
            ViewBag.PuedeVerHistorialAsistencia = TienePermiso(PermisosAsistencia.Historial);
            ViewBag.PuedeVerPorcentajeAsistencia = TienePermiso(PermisosAsistencia.Porcentaje);
            ViewBag.PuedeGestionarAsistencia = TienePermiso("GESTIONAR_ASISTENCIA");
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

        // El permiso general prevalece sobre el propio. -1 nunca debe llegar
        // al repositorio, pues su comportamiento para ese valor no está garantizado.
        private bool PuedeLeerAsistencia()
        {
            return TienePermiso("VER_ASISTENCIA_GENERAL") ||
                   TienePermiso("VER_MI_ASISTENCIA");
        }

        private int AlcanceLectura
        {
            get
            {
                if (TienePermiso("VER_ASISTENCIA_GENERAL")) return 0;
                if (!TienePermiso("VER_MI_ASISTENCIA")) return -1;
                return ObtenerAlcanceDocente();
            }
        }

        // La consulta global NO concede escritura global. Solo un docente
        // asociado puede registrar o editar, sujeto a sus permisos de acción.
        private int AlcanceEscritura
        {
            get { return ObtenerAlcanceDocente(); }
        }

        private int ObtenerAlcanceDocente()
        {
            if (IdUsuarioActual <= 0) return -1;
            var usuario = new UsuarioService().Obtener(IdUsuarioActual);
            if (usuario == null || !usuario.Estado) return -1;
            return usuario.IdDocente > 0 ? usuario.IdDocente : -1;
        }

        private bool PuedeAccederAsistencia()
        {
            return PuedeLeerAsistencia();
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
                PuedeEditar = TienePermiso(PermisosAsistencia.Editar) && AlcanceEscritura > 0
            };

            try
            {
                if (AlcanceLectura < 0)
                {
                    ViewBag.Error = "No tiene grupos asignados en este momento.";
                    return View(vm);
                }
                vm.Grupos = repo.ListarGrupos(AlcanceLectura);
                if (vm.Grupos.Count == 0)
                    ViewBag.Error = "No tiene grupos asignados en este momento.";
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
            if (!TienePermiso(PermisosAsistencia.Registrar) || AlcanceEscritura <= 0) return SinPermiso();
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
            if (!TienePermiso(PermisosAsistencia.Registrar) || AlcanceEscritura <= 0) return SinPermiso();
            if (AlcanceEscritura <= 0) return SinPermiso();
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
            if (docente <= 0)
            {
                ViewBag.Error = "No tiene grupos asignados en este momento.";
                return View("Registrar", vm);
            }

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
            if (!TienePermiso(PermisosAsistencia.Editar) || AlcanceEscritura <= 0) return SinPermiso();
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
            if (!TienePermiso(PermisosAsistencia.Editar) || AlcanceEscritura <= 0) return SinPermiso();
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
            if (!TienePermiso(PermisosAsistencia.Historial) || !PuedeLeerAsistencia()) return SinPermiso();
            var vm = new AsistenciaHistorialVM
            {
                IdGrupo = grupo ?? 0,
                IdEstudiante = estudiante ?? 0
            };

            int alcance = AlcanceLectura;
            if (alcance < 0)
            {
                ViewBag.Error = "No tiene grupos asignados en este momento.";
                return View(vm);
            }

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
            if (!TienePermiso(PermisosAsistencia.Porcentaje) || !PuedeLeerAsistencia()) return SinPermiso();

            var vm = new AsistenciaPorcentajeVM { IdGrupo = grupo ?? 0 };
            int alcance = AlcanceLectura;
            if (alcance < 0)
            {
                ViewBag.Error = "No tiene grupos asignados en este momento.";
                return View(vm);
            }

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
