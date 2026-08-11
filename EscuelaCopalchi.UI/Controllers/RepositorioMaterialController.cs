using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    public class RepositorioMaterialController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }

        public ActionResult Detalle(int? id)
        {
            return View();
        }

        public ActionResult Editar(int? id)
        {
            return View();
        }
    }
}