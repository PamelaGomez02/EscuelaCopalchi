
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Notificaciones;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly NotificacionRepository repository;

        public NotificacionesController()
        {
            repository = new NotificacionRepository();
        }

        private int? ObtenerIdUsuario()
        {
            int id;

            if (Session["IdUsuario"] == null ||
                !int.TryParse(
                    Convert.ToString(Session["IdUsuario"]),
                    out id) || id <= 0)
            {
                return null;
            }

            return id;
        }

        // PÁGINA DE NOTIFICACIONES
        [HttpGet]
        public ActionResult Index()
        {
            var id = ObtenerIdUsuario();

            if (!id.HasValue)
                return new HttpStatusCodeResult(401);

            var lista = repository.ObtenerNotificaciones(id.Value);

            ViewBag.Notificaciones = lista;
            ViewBag.Total = lista.Count;
            ViewBag.Alta = lista.Count(x =>
                x.Tipo == "RiesgoAcademico");
            ViewBag.Pendientes = lista.Count(x => !x.Leida);

            return View("~/Views/Notificaciones/Index.cshtml");
        }

        // RESUMEN PARA LA CAMPANA
        [HttpGet]
        public ActionResult Resumen()
        {
            var id = ObtenerIdUsuario();

            if (!id.HasValue)
                return new HttpStatusCodeResult(401);

            var lista = repository.ObtenerNotificaciones(id.Value);

            var ultimas = lista
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.IdNotificacion)
                .Take(5)
                .Select(x => new
                {
                    id = x.IdNotificacion,
                    titulo = x.Titulo,
                    mensaje = x.Mensaje,
                    fecha = x.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    leida = x.Leida
                })
                .ToList();

            return Json(new
            {
                pendientes = lista.Count(x => !x.Leida),
                notificaciones = ultimas
            }, JsonRequestBehavior.AllowGet);
        }

        // MARCAR UNA NOTIFICACIÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarLeida(int id)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                repository.MarcarComoLeida(id, usuario.Value);

                var lista = repository.ObtenerNotificaciones(
                    usuario.Value);

                bool actualizada = lista.Any(x =>
                    x.IdNotificacion == id && x.Leida);

                return Json(new
                {
                    ok = actualizada,
                    pendientes = lista.Count(x => !x.Leida)
                });
            }
            catch
            {
                Response.StatusCode = 500;

                return Json(new
                {
                    ok = false,
                    mensaje = "No se pudo actualizar la notificación."
                });
            }
        }

        // MARCAR TODAS COMO LEÍDAS

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarTodasLeidas()
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                var antes = repository.ObtenerNotificaciones(
                    usuario.Value);

                int pendientesAntes = antes.Count(x => !x.Leida);

                string respuestaBD =
                    repository.MarcarTodasComoLeidas(usuario.Value);

                var despues = repository.ObtenerNotificaciones(
                    usuario.Value);

                int pendientesDespues =
                    despues.Count(x => !x.Leida);

                bool actualizado = pendientesDespues == 0;

                return Json(new
                {
                    ok = actualizado,
                    pendientes = pendientesDespues,
                    mensaje = actualizado
                        ? "Notificaciones actualizadas correctamente."
                        : "La actualización no se reflejó en la consulta.",
                    pendientesAntes = pendientesAntes,
                    respuestaBD = respuestaBD
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al marcar notificaciones: " + ex);

                Response.StatusCode = 500;

                return Json(new
                {
                    ok = false,
                    mensaje = "Error al actualizar las notificaciones."
                });
            }
        }


        // RIESGO ACADÉMICO
        [HttpGet]
        public ActionResult Riesgo()
        {
            var lista = repository.ObtenerEstudiantesRiesgo();

            ViewBag.TotalRiesgo = lista.Count;

            ViewBag.BajoRendimiento = lista.Count(x =>
                x.Motivo == "Bajo rendimiento");

            ViewBag.Inasistencia = lista.Count(x =>
                x.Motivo == "Inasistencia");

            return View(
                "~/Views/Notificaciones/Riesgo.cshtml",
                lista);
        }
    }
}
