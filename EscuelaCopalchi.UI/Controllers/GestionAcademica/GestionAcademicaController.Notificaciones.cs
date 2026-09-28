using System;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// HU "Como docente, quiero recibir notificaciones cuando se me asigne un nuevo grupo o existan cambios"
    /// y la bitácora de la HU "Como director, quiero modificar la asignación de estudiantes" (escenario 5).
    /// </summary>
    public partial class GestionAcademicaController
    {
        private readonly BitacoraService bitacora = new BitacoraService();

        [PermisoRequerido(Permisos.VerMiGrupo, Permisos.GestionarAcademica, Permisos.VerReportes)]
        public ActionResult Notificaciones(bool soloNoLeidas = false)
        {
            var modelo = new NotificacionesViewModel { SoloNoLeidas = soloNoLeidas };

            try { modelo.Notificaciones = notificaciones.Listar(IdUsuarioActual, soloNoLeidas); }
            catch (Exception ex) { ViewBag.ErrorCarga = ex.Message; }

            return View(modelo);
        }

        /// <param name="id">0 = marcar todas como leídas.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.VerMiGrupo, Permisos.GestionarAcademica, Permisos.VerReportes)]
        public ActionResult MarcarLeida(int id = 0, bool soloNoLeidas = false)
        {
            ResultadoOperacion r = notificaciones.MarcarLeida(id, IdUsuarioActual);
            if (!r.Exito) TempData["Error"] = r.Mensaje;

            return RedirectToAction("Notificaciones", new { soloNoLeidas });
        }

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult Bitacora(string desde = "", string hasta = "", string tipo = "")
        {
            var modelo = new BitacoraViewModel { Desde = desde ?? "", Hasta = hasta ?? "", Tipo = tipo ?? "" };

            try
            {
                modelo.Registros = bitacora.Listar(modelo.Desde, modelo.Hasta, modelo.Tipo);
                modelo.Incidentes = notificaciones.ListarIncidentes();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        /// <summary>Escenario 5 de notificaciones: reintentar los envíos que fallaron.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult ReintentarNotificaciones()
        {
            try
            {
                int errores = notificaciones.ReintentarConError();
                TempData[errores == 0 ? "Exito" : "Advertencia"] = errores == 0
                    ? "Las notificaciones pendientes se enviaron correctamente."
                    : errores + " notificaciones volvieron a fallar. Revise los incidentes.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Bitacora");
        }
    }
}