using EscuelaCopalchi.UI.Models.MonitoreoNuevo;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class MonitoreoDashboardController : Controller
    {
        private readonly MonitoreoDashboardRepository repo = new MonitoreoDashboardRepository();
        private int UsuarioActual { get { return Session["IdUsuario"] == null ? 0 : Convert.ToInt32(Session["IdUsuario"]); } }
        private ActionResult LoginSiFalta() { return RedirectToAction("Login", "Account"); }
        public ActionResult Index(int? anio, int? trimestre, int? grupo, string buscar = "", string estado = "", int pagina = 1)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            try { return View(repo.Obtener(anio, trimestre, grupo, UsuarioActual, buscar, estado, pagina)); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError(ex.ToString()); return new HttpStatusCodeResult(500, "Error al consultar monitoreo académico. Revise el registro del servidor."); }
        }
        // Exporta todos los estudiantes del alcance filtrado, no solo la página visible.
        public ActionResult ExportarExcel(int? anio, int? trimestre, int? grupo)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            var vm = repo.Obtener(anio, trimestre, grupo, UsuarioActual);
            return File(MonitoreoExportador.Excel(vm),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Monitoreo_Academico_" + vm.Anio + ".xlsx");
        }
        public ActionResult ExportarPdf(int? anio, int? trimestre, int? grupo)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            var vm = repo.Obtener(anio, trimestre, grupo, UsuarioActual);
            return File(MonitoreoExportador.Pdf(vm), "application/pdf",
                "Monitoreo_Academico_" + vm.Anio + ".pdf");
        }
        public ActionResult Seguimiento(int? anio, int? grupo, int pagina = 1)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            var d = repo.Obtener(anio, null, grupo, UsuarioActual);
            var casos = repo.Seguimientos(d.Anio, d.IdGrupo);
            int paginas = Math.Max(1, (int)Math.Ceiling(casos.Count / 10.0));
            pagina = Math.Max(1, Math.Min(pagina, paginas));
            return View(new MonitoreoSeguimientoVM { Dashboard = d, Casos = casos.Skip((pagina - 1) * 10).Take(10).ToList(), Pagina = pagina, TotalPaginas = paginas });
        }
        public ActionResult Detalle(int id, int grupo, int anio)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            var vm = repo.Detalle(id, grupo, anio);
            return vm == null ? (ActionResult)HttpNotFound() : View(vm);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RegistrarSeguimiento(int idEstudiante, int idGrupo, int anio, byte? trimestre, string nivelRiesgo, string estado, string observacion, string accionRealizada)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            if (trimestre.HasValue && (trimestre.Value < 1 || trimestre.Value > 3)) return new HttpStatusCodeResult(400);
            if (!(new[] { "En evaluación", "Bajo", "Medio", "Alto", "Crítico" }).Contains(nivelRiesgo)) return new HttpStatusCodeResult(400);
            if (!(new[] { "Pendiente", "En seguimiento", "Mejorando", "Resuelto" }).Contains(estado)) return new HttpStatusCodeResult(400);
            if (string.IsNullOrWhiteSpace(observacion) || observacion.Length > 2000 || (accionRealizada ?? "").Length > 2000) return new HttpStatusCodeResult(400);
            try { repo.GuardarSeguimiento(idEstudiante, idGrupo, anio, trimestre, nivelRiesgo, estado, observacion, accionRealizada, UsuarioActual); TempData["Ok"] = "Seguimiento registrado correctamente."; }
            catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("Detalle", new { id = idEstudiante, grupo = idGrupo, anio });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int id, int idEstudiante, int grupo, int anio, string estado)
        {
            if (UsuarioActual <= 0) return LoginSiFalta();
            if (!(new[] { "Pendiente", "En seguimiento", "Mejorando", "Resuelto" }).Contains(estado)) return new HttpStatusCodeResult(400);
            repo.ActualizarEstado(id, estado);
            TempData["Ok"] = "Estado actualizado.";
            return RedirectToAction("Detalle", new { id = idEstudiante, grupo, anio });
        }
    }
}
