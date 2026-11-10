
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.Monitoreo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class MonitoreoController : Controller
    {
        private const string TipoXlsx =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly MonitoreoRepository repo =
            new MonitoreoRepository();

        // Contexto académico.
        // No contiene filtros específicos de roles.
        private MonitoreoSesion Sesion
        {
            get
            {
                return MonitoreoSesion.Actual(HttpContext);
            }
        }

        private bool TryAlcance(out int alcance, out string error)
        {
            alcance = -1;
            error = null;

            try
            {
                var sesion = MonitoreoSesion.Actual(HttpContext);

                if (sesion == null)
                {
                    error = "No se pudo identificar el contexto académico del usuario autenticado.";
                    return false;
                }

                if (sesion.IdDocente <= 0)
                {
                    error = "El usuario no tiene un docente asociado. No es posible determinar sus grupos académicos.";
                    return false;
                }

                alcance = sesion.IdDocente;
                return true;
            }
            catch (Exception ex)
            {
                error = MonitoreoRepository.Mensaje(ex);
                return false;
            }
        }

        // ==========================================
        // CONSULTAR RENDIMIENTO ACADÉMICO
        // ==========================================
        public ActionResult Index(
            int? anio,
            int? grupo,
            int? trimestre,
            string materia,
            string estado,
            string q)
        {
            var vm = new MonitoreoIndexVM
            {
                Umbrales = UmbralesRendimiento.Actual()
            };

            int alcance;
            string error;

            if (!TryAlcance(out alcance, out error))
            {
                vm.Error = error;
                vm.SinInformacion = true;
                return View(vm);
            }

            try
            {
                vm.Anios = repo.ListarAnios(alcance);

                if (vm.Anios.Count == 0)
                {
                    vm.SinInformacion = true;
                    return View(vm);
                }

                vm.Anio =
                    anio.HasValue &&
                    vm.Anios.Contains(anio.Value)
                    ? anio.Value
                    : vm.Anios[0];

                vm.Grupos = repo.ListarGrupos(
                    alcance,
                    vm.Anio);

                vm.IdGrupo =
                    grupo.HasValue &&
                    vm.Grupos.Any(g => g.IdGrupo == grupo.Value)
                    ? grupo.Value
                    : 0;

                vm.Trimestre =
                    trimestre.HasValue &&
                    trimestre.Value >= 1 &&
                    trimestre.Value <= 3
                    ? trimestre.Value
                    : 0;

                List<NotaFinalFila> filas =
                    repo.ObtenerNotasFinales(
                        alcance,
                        vm.IdGrupo,
                        0,
                        vm.Anio,
                        true);

                vm.Materias = MateriasDe(filas);

                vm.Materia = MateriaValida(
                    vm.Materias,
                    materia);

                List<RendimientoEstudiante> todos =
                    MonitoreoCalculo.ArmarRendimiento(
                        filas,
                        vm.Umbrales,
                        vm.Trimestre,
                        vm.Materia);

                vm.Resumen =
                    MonitoreoCalculo.Resumir("", todos);

                vm.Estado = EstadoValido(estado);
                vm.Buscar = (q ?? "").Trim();

                IEnumerable<RendimientoEstudiante> lista =
                    todos;

                if (vm.Estado != "")
                {
                    lista = lista.Where(
                        e => e.Estado == vm.Estado);
                }

                if (vm.Buscar != "")
                {
                    lista = lista.Where(e =>
                        (e.Estudiante ?? "").IndexOf(
                            vm.Buscar,
                            StringComparison.CurrentCultureIgnoreCase
                        ) >= 0
                        ||
                        (e.Identificacion ?? "").IndexOf(
                            vm.Buscar,
                            StringComparison.CurrentCultureIgnoreCase
                        ) >= 0);
                }

                vm.Estudiantes = lista.ToList();
            }
            catch (Exception ex)
            {
                vm.Error = MonitoreoRepository.Mensaje(ex);
            }

            return View(vm);
        }

        // ==========================================
        // REPORTE ACADÉMICO
        // ==========================================
        public ActionResult Reporte(
            int? anio,
            int? grupo,
            int? trimestre,
            string materia,
            bool? generar)
        {
            MonitoreoReporteVM vm = ConstruirReporte(
                anio,
                grupo,
                trimestre,
                materia,
                generar == true);

            return View(vm);
        }

        // ==========================================
        // EXPORTAR REPORTE A PDF O EXCEL
        // ==========================================
        public ActionResult ExportarReporte(
            int? anio,
            int? grupo,
            int? trimestre,
            string materia,
            string formato)
        {
            MonitoreoReporteVM vm = ConstruirReporte(
                anio,
                grupo,
                trimestre,
                materia,
                true);

            if (!vm.Generado)
            {
                TempData["Error"] =
                    vm.Error ??
                    vm.MensajeVacio ??
                    EstadosRendimiento.MensajeSinInformacion;

                return RedirectToAction(
                    "Reporte",
                    new
                    {
                        anio,
                        grupo,
                        trimestre,
                        materia,
                        generar = true
                    });
            }

            ReporteMonitoreo r = vm.Reporte;

            if (string.Equals(
                formato,
                "pdf",
                StringComparison.OrdinalIgnoreCase))
            {
                byte[] pdf = PdfHelper.GenerarTabla(
                    r.Titulo,
                    r.Subtitulo,
                    r.Columnas,
                    r.Filas);

                return File(
                    pdf,
                    "application/pdf",
                    r.NombreArchivo + ".pdf");
            }

            byte[] xlsx = ExcelHelper.Generar(
                "Reporte",
                r.Titulo,
                r.Subtitulo,
                r.Columnas,
                r.Filas);

            return File(
                xlsx,
                TipoXlsx,
                r.NombreArchivo + ".xlsx");
        }

        // ==========================================
        // ESTADÍSTICAS INSTITUCIONALES
        // ==========================================
        public ActionResult Estadisticas()
        {
            return View();
        }

        // ==========================================
        // CONSTRUIR REPORTE
        // ==========================================
        private MonitoreoReporteVM ConstruirReporte(
            int? anio,
            int? grupo,
            int? trimestre,
            string materia,
            bool generar)
        {
            var vm = new MonitoreoReporteVM
            {
                Umbrales = UmbralesRendimiento.Actual()
            };

            int alcance;
            string error;

            if (!TryAlcance(out alcance, out error))
            {
                vm.Error = error;
                vm.SinGrupos = true;
                return vm;
            }

            try
            {
                vm.Anios = repo.ListarAnios(alcance);

                if (vm.Anios.Count == 0)
                {
                    vm.SinGrupos = true;
                    return vm;
                }

                vm.Anio =
                    anio.HasValue &&
                    vm.Anios.Contains(anio.Value)
                    ? anio.Value
                    : vm.Anios[0];

                vm.Grupos = repo.ListarGrupos(
                    alcance,
                    vm.Anio);

                vm.Trimestre =
                    trimestre.HasValue &&
                    trimestre.Value >= 1 &&
                    trimestre.Value <= 3
                    ? trimestre.Value
                    : 0;

                if (vm.Grupos.Count == 0)
                {
                    vm.SinGrupos = true;
                    return vm;
                }

                GrupoMonitoreoItem g =
                    grupo.HasValue
                    ? vm.Grupos.FirstOrDefault(
                        x => x.IdGrupo == grupo.Value)
                    : null;

                if (g == null && vm.Grupos.Count == 1)
                {
                    g = vm.Grupos[0];
                }

                if (g == null)
                {
                    return vm;
                }

                vm.IdGrupo = g.IdGrupo;

                List<NotaFinalFila> filas =
                    repo.ObtenerNotasFinales(
                        alcance,
                        g.IdGrupo,
                        0,
                        vm.Anio,
                        true);

                vm.Materias = MateriasDe(filas);

                vm.Materia = MateriaValida(
                    vm.Materias,
                    materia);

                if (!generar)
                {
                    return vm;
                }

                if (vm.Materia == "")
                {
                    if (filas.Count == 0)
                    {
                        vm.MensajeVacio =
                            EstadosRendimiento.MensajeSinInformacion;

                        return vm;
                    }

                    vm.Reporte =
                        MonitoreoReporteBuilder.PorGrupo(
                            g,
                            filas,
                            vm.Umbrales,
                            vm.Trimestre);
                }
                else
                {
                    ReporteMonitoreo r =
                        MonitoreoReporteBuilder.PorMateria(
                            g,
                            filas,
                            vm.Umbrales,
                            vm.Materia,
                            vm.Trimestre);

                    if (r.Resumen.ConNotas == 0)
                    {
                        vm.MensajeVacio =
                            EstadosRendimiento.MensajeSinInformacion;

                        return vm;
                    }

                    vm.Reporte = r;
                }

                vm.Generado = true;
            }
            catch (Exception ex)
            {
                vm.Error = MonitoreoRepository.Mensaje(ex);
            }

            return vm;
        }

        // ==========================================
        // OBTENER MATERIAS
        // ==========================================
        private static List<string> MateriasDe(
            IEnumerable<NotaFinalFila> filas)
        {
            return filas
                .Where(f => f.TieneAsignatura)
                .Select(f => f.Asignatura)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    a => a,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        // ==========================================
        // VALIDAR MATERIA
        // ==========================================
        private static string MateriaValida(
            List<string> materias,
            string materia)
        {
            if (string.IsNullOrWhiteSpace(materia))
            {
                return "";
            }

            return materias.FirstOrDefault(m =>
                string.Equals(
                    m,
                    materia.Trim(),
                    StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        // ==========================================
        // VALIDAR ESTADO
        // ==========================================
        private static string EstadoValido(
            string estado)
        {
            switch (estado)
            {
                case EstadosRendimiento.Aprobado:
                case EstadosRendimiento.EnRiesgo:
                case EstadosRendimiento.Reprobado:
                case EstadosRendimiento.SinNotas:
                    return estado;

                default:
                    return "";
            }
        }
    }
}
