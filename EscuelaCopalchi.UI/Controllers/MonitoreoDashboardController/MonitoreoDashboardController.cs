using EscuelaCopalchi.UI.Models.MonitoreoNuevo;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class MonitoreoDashboardController : Controller
    {
        private readonly MonitoreoDashboardRepository repo = new MonitoreoDashboardRepository();
        private readonly string cs = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;

        public ActionResult Index(int? anio, int? trimestre, int? grupo)
        {
            if (Session["IdUsuario"] == null) return RedirectToAction("Login", "Account");
            int id = Convert.ToInt32(Session["IdUsuario"]);
            try { return View(repo.Obtener(anio, trimestre, grupo, id)); }
            catch (UnauthorizedAccessException) { return new HttpStatusCodeResult(403, "Sin alcance académico autorizado."); }
            catch (Exception) { return new HttpStatusCodeResult(500, "No se pudo cargar el monitoreo académico. Revise el registro de errores del servidor."); }
        }

    }
}
