using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    public class PlanesEstudioController : Controller
    {
        // GET: PlanesEstudio
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

        public ActionResult Revisar(int? id)
        {
            return View();
        }
    }
}