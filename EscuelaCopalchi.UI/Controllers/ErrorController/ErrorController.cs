
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class ErrorController : Controller
    {
        [AllowAnonymous]
        public ActionResult AccesoDenegado()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;

            return View(
                "~/Views/Error/AccesoDenegado.cshtml");
        }
    }
}
