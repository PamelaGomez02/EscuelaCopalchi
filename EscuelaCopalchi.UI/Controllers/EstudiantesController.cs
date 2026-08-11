using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    public class EstudiantesController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Crear()
        {
            return View();
        }

        public ActionResult ImportarExcel()
        {
            return View();
        }

        public ActionResult Editar(int? id)
        {
            return View();
        }

        public ActionResult Detalle(int? id)
        {
            return View();
        }

        public ActionResult Expediente(int? id)
        {
            return View();
        }

        public ActionResult HistorialBajas()
        {
            return View();
        }
    }
}