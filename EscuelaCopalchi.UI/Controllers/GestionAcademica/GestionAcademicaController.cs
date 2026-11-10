
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using EscuelaCopalchi.UI.Models.Usuarios;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public partial class GestionAcademicaController : Controller
    {
        private readonly UsuarioService usuarios =
            new UsuarioService();

        private readonly GrupoService grupos =
            new GrupoService();

        private readonly NotificacionService notificaciones =
            new NotificacionService();

        // =====================================================
        // USUARIO AUTENTICADO
        // =====================================================

        private UsuarioSesion UsuarioActual
        {
            get
            {
                return SesionUsuario.Obtener(Session);
            }
        }

        private int IdUsuarioActual
        {
            get
            {
                return UsuarioActual == null
                    ? 0
                    : UsuarioActual.IdUsuario;
            }
        }

        // =====================================================
        // VALIDACIÓN GENERAL DE ACCESO
        // =====================================================

        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // Obtener el usuario desde la sesión general.
            UsuarioSesion sesion =
                SesionUsuario.Obtener(Session);

            if (sesion == null || sesion.IdUsuario <= 0)
            {
                filterContext.Result = RedirectToAction(
                    "Login", "Account");

                return;
            }

            int idUsuario = sesion.IdUsuario;

            try
            {
                // Consultar los datos y permisos actuales.
                UsuarioSistema usuario =
                    usuarios.Obtener(idUsuario);

                if (usuario == null || !usuario.Estado)
                {
                    SesionUsuario.Cerrar(Session);

                    filterContext.Result = RedirectToAction(
                        "AccesoDenegado", "Error");

                    return;
                }

                // Actualizar los permisos de la sesión.
                SesionUsuario.Guardar(Session, usuario);

                UsuarioSesion usuarioSesion =
                    SesionUsuario.Obtener(Session);

                if (usuarioSesion == null ||
                    usuarioSesion.IdUsuario != idUsuario)
                {
                    filterContext.Result = RedirectToAction(
                        "AccesoDenegado", "Error");

                    return;
                }

                // =================================================
                // PERMISOS DE GESTIÓN ACADÉMICA
                // =================================================

                // Permitir el acceso al panel si el usuario
                // tiene al menos un permiso del módulo.

                bool tieneAcceso =
                    Permisos.DelModulo.Any(
                        permiso => usuarioSesion.TienePermiso(
                            permiso));

                if (!tieneAcceso)
                {
                    filterContext.Result = RedirectToAction(
                        "AccesoDenegado", "Error");

                    return;
                }

                // Datos utilizados por las vistas.
                ViewBag.UsuarioActual = usuarioSesion;

                ViewBag.NotificacionesNoLeidas = 0;

                try
                {
                    ViewBag.NotificacionesNoLeidas =
                        notificaciones.ContarNoLeidas(
                            idUsuario);
                }
                catch (Exception)
                {
                    // Un error en las notificaciones
                    // no debe bloquear el módulo.
                }
            }
            catch (Exception)
            {
                filterContext.Result =
                    new HttpStatusCodeResult(
                        500,
                        "No se pudieron verificar los permisos");

                return;
            }
        }

        // =====================================================
        // INICIO DE GESTIÓN ACADÉMICA
        // =====================================================

        public ActionResult Index()
        {
            var modelo =
                new GestionAcademicaIndexViewModel();

            try
            {
                modelo.Resumen =
                    grupos.ObtenerResumen();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        // =====================================================
        // RESULTADOS DE OPERACIONES
        // =====================================================

        private void MostrarResultado(
            ResultadoOperacion resultado)
        {
            if (!resultado.Exito)
            {
                TempData["Error"] =
                    resultado.Mensaje;

                return;
            }

            TempData["Exito"] =
                resultado.Mensaje;

            try
            {
                if (notificaciones.ProcesarPendientes() > 0)
                {
                    TempData["Advertencia"] =
                        "La operación se guardó, pero algunas " +
                        "notificaciones no se pudieron enviar " +
                        "por correo. El incidente quedó " +
                        "registrado en la bitácora.";
                }
            }
            catch (Exception)
            {
                TempData["Advertencia"] =
                    "La operación se guardó, pero no fue " +
                    "posible procesar las notificaciones.";
            }
        }

        // =====================================================
        // DISTRIBUCIÓN AUTOMÁTICA
        // =====================================================

        // Se conserva para no afectar las referencias
        // existentes, aunque ya no aparezca en el menú.

        public ActionResult Distribucion()
        {
            // Esta funcionalidad todavía es una maqueta.
            // No se permite acceder directamente hasta
            // que se implemente.

            return RedirectToAction(
                "Index",
                "GestionAcademica");
        }
    }
}
