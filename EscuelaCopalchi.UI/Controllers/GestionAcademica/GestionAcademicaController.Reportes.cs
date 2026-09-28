using System;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// HU "Como director, quiero generar reportes de distribución de estudiantes por aula, grupo y docente".
    /// Usa el permiso VER_REPORTES que ya existe en la base (lo puede tener también el rol Evaluación).
    /// </summary>
    public partial class GestionAcademicaController
    {
        private readonly ReporteService reportes = new ReporteService();

        [PermisoRequerido(Permisos.VerReportes)]
        public ActionResult Reportes(string tipo = "AULA", int? periodo = null)
        {
            var modelo = new ReportesViewModel { Tipo = ReporteService.TipoValido(tipo), Periodo = periodo ?? DateTime.Today.Year };

            try
            {
                modelo.Periodos = grupos.ListarPeriodos();
                modelo.Resultado = reportes.Generar(modelo.Tipo, modelo.Periodo);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        /// <summary>Escenario 4: exportar a Excel o PDF. Escenario 5: sin información no se exporta.</summary>
        [PermisoRequerido(Permisos.VerReportes)]
        public ActionResult ExportarReporte(string tipo = "AULA", int periodo = 0, string formato = "excel")
        {
            tipo = ReporteService.TipoValido(tipo);

            try
            {
                ReporteResultado reporte = reportes.Generar(tipo, periodo);

                if (!reporte.TieneDatos)
                {
                    TempData["Advertencia"] = "No hay información para exportar con los filtros seleccionados.";
                    return RedirectToAction("Reportes", new { tipo, periodo });
                }

                string nombre = "Reporte_" + tipo.ToLowerInvariant() + "_" + (periodo > 0 ? periodo.ToString() : "todos")
                                + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm");

                if ((formato ?? "").ToLowerInvariant() == "pdf")
                    return File(reportes.ExportarPdf(reporte), "application/pdf", nombre + ".pdf");

                return File(reportes.ExportarExcel(reporte),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre + ".xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo generar el archivo: " + ex.Message;
                return RedirectToAction("Reportes", new { tipo, periodo });
            }
        }
    }
}