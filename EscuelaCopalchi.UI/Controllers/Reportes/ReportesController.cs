using global::EscuelaCopalchi.UI.Models.GestionNotas;
using global::EscuelaCopalchi.UI.Models.Reportes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace EscuelaCopalchi.UI.Controllers
{
    // Reportes usa el inicio de sesión general (Account/Login).
    // Docente: solo sus grupos. Director y Evaluación: todos los grupos.
    public class ReportesController : Controller
    {
        private readonly ReportesRepository repo = new ReportesRepository();
        private readonly NotasRepository notas = new NotasRepository();
        private readonly FichaRepository ficha = new FichaRepository();

        private const string TipoDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const int MaxCarta = 4 * 1024 * 1024; // 4 MB (límite por defecto de ASP.NET)
        private const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var s = ReportesSesion.Obtener(Session);

            if (s == null)
            {
                // la sesión expiró aunque la cookie siga vigente: se vuelve a iniciar sesión
                FormsAuthentication.SignOut();
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Login" },
                    { "returnUrl", Request.RawUrl }
                });
                return;
            }

            if (!(s.EsDocente || s.EsDirector || s.EsEvaluacion))
            {
                TempData["Error"] = "Su rol no tiene permiso para generar reportes.";
                filterContext.Result = RedirectToAction("Index", "Home");
                return;
            }

            // El permiso se comprueba en el servidor, no solamente en la tarjeta del Home.
            bool autorizado = false;
            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString))
            using (var cmd = new SqlCommand(@"SELECT COUNT(1) FROM dbo.USUARIO u
                INNER JOIN dbo.ROL r ON r.id_rol=u.id_rol
                INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
                INNER JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
                WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1 AND p.estado=1
                  AND p.nombre=N'VER_REPORTES_ACADEMICOS'", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = s.IdUsuario;
                cn.Open();
                autorizado = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            if (!autorizado)
            {
                TempData["Error"] = "No tiene asignado el permiso de Reportes académicos.";
                filterContext.Result = RedirectToAction("Index", "Home");
                return;
            }

            ViewBag.ReportesSesion = s;
            base.OnActionExecuting(filterContext);
        }

        // 0 = todos los grupos, > 0 = solo ese docente, -1 = docente sin registro
        private int Alcance()
        {
            var s = ReportesSesion.Obtener(Session);
            if (s.EsDirector || s.EsEvaluacion) return 0;
            return s.IdDocente > 0 ? s.IdDocente : -1;
        }

        // ---------------- PANEL ----------------
        public ActionResult Index(int grupo = 0, string buscar = "")
        {
            var vm = new ReportesViewModel { Grupo = grupo, Buscar = (buscar ?? "").Trim() };
            int alcance = Alcance();

            if (alcance < 0)
            {
                ViewBag.Error = "Su usuario no tiene un registro de docente asociado.";
                return View(vm);
            }

            try
            {
                vm.Grupos = notas.ListarGrupos(alcance);

                // un docente no puede pedir un grupo ajeno
                if (grupo > 0 && !vm.Grupos.Any(g => g.IdGrupo == grupo)) vm.Grupo = 0;

                vm.Estudiantes = repo.Rendimiento(alcance, vm.Grupo, vm.Buscar)
                    .OrderByDescending(e => e.EnRiesgo)
                    .ThenBy(e => e.Promedio ?? 101m)
                    .ThenBy(e => e.Estudiante)
                    .ToList();

                vm.Asignaturas = repo.PorAsignatura(alcance, vm.Grupo, vm.Buscar);

                // Comparación institucional dentro del alcance académico autorizado.
                // Independiente del grupo seleccionado para el detalle.
                vm.ComparacionGrupos = repo.Rendimiento(alcance, 0, "");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "No se pudo cargar el reporte: " + ex.Message;
            }

            return View(vm);
        }

        // ---------------- EXPORTAR A EXCEL ----------------
        public ActionResult ExportarRendimiento(int grupo = 0, string buscar = "")
        {
            int alcance = Alcance();

            if (alcance < 0)
            {
                TempData["Error"] = "Su usuario no tiene un registro de docente asociado.";
                return RedirectToAction("Index");
            }

            try
            {
                var grupos = notas.ListarGrupos(alcance);
                if (grupo > 0 && !grupos.Any(g => g.IdGrupo == grupo)) grupo = 0;

                var estudiantes = repo.Rendimiento(alcance, grupo, buscar)
                    .OrderByDescending(e => e.EnRiesgo).ThenBy(e => e.Promedio ?? 101m).ThenBy(e => e.Estudiante).ToList();

                if (estudiantes.Count == 0)
                {
                    TempData["Error"] = "No hay estudiantes para exportar con los filtros seleccionados.";
                    return RedirectToAction("Index", new { grupo = grupo, buscar = buscar });
                }

                var asignaturas = repo.PorAsignatura(alcance, grupo, buscar);

                var g1 = grupos.FirstOrDefault(g => g.IdGrupo == grupo);
                string titulo = g1 != null ? g1.Grupo + " (" + g1.Anio + ")" : "Todos los grupos";
                string archivo = "Reporte_Rendimiento_" + FichaPdf.NombreArchivo(titulo).Replace("Ficha_", "").Replace(".pdf", "") + ".xlsx";

                return File(RendimientoXlsx.Generar(titulo, estudiantes, asignaturas), TipoXlsx, archivo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo generar el Excel: " + ex.Message;
                return RedirectToAction("Index", new { grupo = grupo });
            }
        }

        // ---------------- FICHA ACADÉMICA (PDF) ----------------
        public ActionResult Ficha(int estudiante = 0, bool descargar = false)
        {
            int alcance = Alcance();

            if (alcance < 0 || estudiante <= 0)
            {
                TempData["Error"] = "No se pudo identificar al estudiante.";
                return RedirectToAction("Index");
            }

            try
            {
                var datos = ficha.Datos(alcance, estudiante);

                if (datos == null)
                {
                    TempData["Error"] = "El estudiante no existe o no pertenece a sus grupos.";
                    return RedirectToAction("Index");
                }

                var s = ReportesSesion.Obtener(Session);
                byte[] pdf = FichaPdf.Generar(datos, ficha.Notas(estudiante), ficha.Asistencia(estudiante),
                                              ficha.Externas(estudiante), s.Nombre);

                string nombre = FichaPdf.NombreArchivo(datos.NombreCompleto);

                // por defecto se abre en el navegador; con ?descargar=true se descarga
                Response.AddHeader("Content-Disposition", (descargar ? "attachment" : "inline") + "; filename=" + nombre);
                return File(pdf, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo generar la ficha: " + ex.Message;
                return RedirectToAction("Index");
            }
        }


        // Exporta TODOS los registros que coinciden con el grupo, búsqueda y filtro, no solo la página visible.
        [HttpGet]
        public ActionResult ExportarSeleccion(int grupo = 0, string buscar = "", string filtro = "todos", string formato = "pdf")
        {
            int alcance = Alcance();
            if (alcance < 0) return new HttpStatusCodeResult(403);
            var grupos = notas.ListarGrupos(alcance);
            if (grupo > 0 && !grupos.Any(g => g.IdGrupo == grupo)) grupo = 0;
            var datos = repo.Rendimiento(alcance, grupo, buscar ?? "");
            switch ((filtro ?? "todos").ToLowerInvariant())
            {
                case "riesgo": datos = datos.Where(x => x.EnRiesgo).ToList(); break;
                case "nota": datos = datos.Where(x => x.BajoRendimiento).ToList(); break;
                case "asis": datos = datos.Where(x => x.BajaAsistencia).ToList(); break;
                case "todos": break;
                default: return new HttpStatusCodeResult(400, "Filtro inválido");
            }
            if (!datos.Any()) { TempData["Error"] = "No hay datos para exportar."; return RedirectToAction("Index", new { grupo, buscar }); }
            string titulo = "Reporte de estudiantes - " + (grupos.FirstOrDefault(g => g.IdGrupo == grupo)?.Grupo ?? "Todos los grupos");
            if (string.Equals(formato, "excel", StringComparison.OrdinalIgnoreCase))
            {
                var asignaturas = repo.PorAsignatura(alcance, grupo, buscar ?? "");
                // La hoja por asignatura corresponde al grupo/búsqueda, no al filtro de alertas.
                return File(RendimientoXlsx.Generar(titulo, datos, asignaturas), TipoXlsx, "Reporte_Estudiantes.xlsx");
            }
            if (!string.Equals(formato, "pdf", StringComparison.OrdinalIgnoreCase)) return new HttpStatusCodeResult(400);
            var pdf = new PdfDocumento();
            double y = 0;
            Action cabecera = () =>
            {
                pdf.Rect(0, 0, PdfDocumento.Ancho, 74, 19, 60, 85);
                pdf.Texto(40, 33, "ESCUELA COPALCHI", 17, true, 255, 255, 255);
                pdf.Texto(40, 54, "Reporte de rendimiento y asistencia", 10, false, 235, 241, 247);
                y = 97;
                pdf.Texto(40, y, titulo, 11, true, 19, 60, 85); y += 24;
                pdf.Rect(40, y - 13, 515, 20, 232, 239, 246);
                pdf.Texto(46, y, "Estudiante", 9, true);
                pdf.Texto(280, y, "Prom.", 9, true);
                pdf.Texto(340, y, "Asist.", 9, true);
                pdf.Texto(405, y, "Aus.", 9, true);
                pdf.Texto(455, y, "Alerta", 9, true); y += 24;
            };
            cabecera();
            foreach (var e in datos)
            {
                if (y > PdfDocumento.Alto - 65) { pdf.NuevaPagina(); cabecera(); }
                pdf.Texto(46, y, pdf.Recortar(e.Estudiante, 226, 9), 9);
                pdf.Texto(280, y, e.Promedio.HasValue ? e.Promedio.Value.ToString("0.#") : "-", 9);
                pdf.Texto(340, y, e.PctAsistencia.HasValue ? e.PctAsistencia.Value.ToString("0.#") + "%" : "-", 9);
                pdf.Texto(405, y, e.Clases > 0 ? e.Ausentes.ToString() : "-", 9);
                pdf.Texto(455, y, e.EnRiesgo ? "Seguimiento" : "Sin alerta", 8);
                pdf.Linea(40, y + 7, 555, y + 7, .35, 224, 230, 236); y += 24;
            }
            return File(pdf.Guardar("Generado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm")), "application/pdf", "Reporte_Estudiantes.pdf");
        }
    }
}
