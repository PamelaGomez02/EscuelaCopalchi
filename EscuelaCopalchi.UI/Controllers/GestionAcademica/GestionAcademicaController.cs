using System;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// Módulo de Gestión Académica. El controlador está dividido en archivos parciales,
    /// uno por historia de usuario (carpeta Controllers/GestionAcademica):
    ///   GestionAcademicaController.cs                → inicio y usuario actual
    ///   GestionAcademicaController.Grupos.cs         → aulas, grupos, horarios y docentes
    ///   GestionAcademicaController.Asignaciones.cs   → asignación y traslado de estudiantes
    ///   GestionAcademicaController.MiGrupo.cs        → grupos del docente
    ///   GestionAcademicaController.Notificaciones.cs → notificaciones y bitácora
    ///   GestionAcademicaController.Reportes.cs       → reportes y exportación
    ///   GestionAcademicaController.Importacion.cs    → importación desde Excel
    /// Solo recibe los datos de la pantalla y llama a la capa lógica (Logica/GestionAcademica).
    /// </summary>
    public partial class GestionAcademicaController : Controller
    {
        private readonly UsuarioService usuarios = new UsuarioService();
        private readonly GrupoService grupos = new GrupoService();
        private readonly NotificacionService notificaciones = new NotificacionService();

        private UsuarioSesion UsuarioActual
        {
            get { return SesionUsuario.Obtener(Session); }
        }

        private int IdUsuarioActual
        {
            get { return UsuarioActual == null ? 0 : UsuarioActual.IdUsuario; }
        }

        /// <summary>Datos de la barra de usuario (_BarraUsuario) para todas las vistas del módulo.</summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            UsuarioSesion usuario = UsuarioActual;
            ViewBag.UsuarioActual = usuario;
            ViewBag.NotificacionesNoLeidas = 0;

            if (usuario != null)
            {
                try { ViewBag.NotificacionesNoLeidas = notificaciones.ContarNoLeidas(usuario.IdUsuario); }
                catch (Exception) { /* si falla la base, cada vista muestra su propio error */ }
            }
        }

        /// <summary>Muestra el resultado de una operación y entrega las notificaciones que generó.</summary>
        private void MostrarResultado(ResultadoOperacion resultado)
        {
            if (!resultado.Exito)
            {
                TempData["Error"] = resultado.Mensaje;
                return;
            }

            TempData["Exito"] = resultado.Mensaje;

            if (notificaciones.ProcesarPendientes() > 0)
                TempData["Advertencia"] = "La operación se guardó, pero algunas notificaciones no se pudieron enviar por correo. " +
                                          "El incidente quedó registrado en la bitácora.";
        }

        // ================================================================
        // INICIO
        // ================================================================

        public ActionResult Index()
        {
            var modelo = new GestionAcademicaIndexViewModel();

            try
            {
                modelo.Resumen = grupos.ObtenerResumen();
                modelo.Usuarios = usuarios.ListarUsuariosDelModulo();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        /// <summary>
        /// Selecciona con qué usuario se trabaja (temporal hasta que exista el login).
        /// idUsuario = 0 cierra la sesión.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarUsuario(int idUsuario = 0)
        {
            if (idUsuario <= 0)
            {
                SesionUsuario.Cerrar(Session);
                return RedirectToAction("Index");
            }

            try
            {
                UsuarioSistema usuario = usuarios.Obtener(idUsuario);

                if (usuario == null || !usuario.Estado || !usuario.UsaModulo)
                {
                    TempData["Error"] = "El usuario no existe, está inactivo o su rol no tiene permisos de Gestión Académica.";
                    return RedirectToAction("Index");
                }

                SesionUsuario.Guardar(Session, usuario);
                TempData["Exito"] = "Ahora está trabajando como " + usuario.NombreCompleto + " (" + usuario.Rol + ").";

                bool soloDocente = usuario.TienePermiso(Permisos.VerMiGrupo) && !usuario.TienePermiso(Permisos.GestionarAcademica);
                return soloDocente ? RedirectToAction("MiGrupo") : RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        /// <summary>Maqueta: no forma parte de las historias de usuario.</summary>
        public ActionResult Distribucion()
        {
            return View();
        }
    }
}
