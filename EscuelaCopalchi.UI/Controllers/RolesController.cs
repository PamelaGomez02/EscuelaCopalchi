using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class RolesController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }

        public ActionResult Editar(int? id)
        {
            return View();
        }

        public ActionResult Asignar(int? id)
        {
            return View();
        }

        public ActionResult Permisos(int? id)
        {
            return View();
        }
    }
}