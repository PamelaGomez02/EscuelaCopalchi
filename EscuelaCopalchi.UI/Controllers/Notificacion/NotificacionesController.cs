using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Notificaciones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    namespace EscuelaCopalchi.UI.Controllers
    {
        public class NotificacionesController : Controller
        {

            private readonly NotificacionRepository repository;

            public NotificacionesController()
            {
                repository = new NotificacionRepository();
            }


            public ActionResult Index()
            {
                int idUsuario =
                    Convert.ToInt32(
                        Session["IdUsuario"]);

                var lista =
                    repository.ObtenerNotificaciones(
                        idUsuario);

                ViewBag.Notificaciones =
                    lista;

                ViewBag.Total =
                    lista.Count;

                ViewBag.Alta =
                    lista.Count(x =>
                        x.Tipo == "RiesgoAcademico");

                ViewBag.Pendientes =
                    lista.Count(x =>
                        !x.Leida);

                return View(
                    "~/Views/Notificaciones/Index.cshtml");
            }

            public ActionResult Riesgo()
            {
                var lista =
                    repository.ObtenerEstudiantesRiesgo();

                ViewBag.TotalRiesgo =
                    lista.Count;

                ViewBag.BajoRendimiento =
                    lista.Count(x =>
                        x.Motivo ==
                        "Bajo rendimiento");

                ViewBag.Inasistencia =
                    lista.Count(x =>
                        x.Motivo ==
                        "Inasistencia");

                return View(
                    "~/Views/Notificaciones/Riesgo.cshtml",
                    lista);
            }
        }
    }
}