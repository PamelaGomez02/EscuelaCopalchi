using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    public class AsistenciaController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Registrar()
        {
            return View();
        }

        public ActionResult Editar(int? id)
        {
            return View();
        }

        public ActionResult Historial()
        {
            return View();
        }
    }
}