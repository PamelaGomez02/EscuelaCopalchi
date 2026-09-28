using System;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// HU "Como director, quiero modificar la asignación de estudiantes en caso de traslados o cambios de aula".
    /// </summary>
    public partial class GestionAcademicaController
    {
        private readonly AsignacionService asignaciones = new AsignacionService();

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult Asignaciones(int grupo = 0, string busqueda = "")
        {
            var modelo = new AsignacionesViewModel { FiltroGrupo = grupo, Busqueda = busqueda ?? "" };

            try
            {
                modelo.Grupos = grupos.ListarActivos();
                modelo.Estudiantes = asignaciones.Listar(grupo, modelo.Busqueda);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        /// <summary>Escenarios 1 a 5: reasignar, validar capacidad, cambiar de grupo, notificar y registrar en bitácora.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult AsignarEstudiante(int idEstudiante, int idGrupo, string motivo = "",
                                              int filtroGrupo = 0, string busqueda = "")
        {
            if (idEstudiante <= 0 || idGrupo <= 0)
                TempData["Error"] = "Seleccione el estudiante y el grupo.";
            else
                MostrarResultado(asignaciones.Asignar(idEstudiante, idGrupo, motivo, IdUsuarioActual));

            return RedirectToAction("Asignaciones", new { grupo = filtroGrupo, busqueda });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult RetirarEstudiante(int idEstudiante, string motivo = "", int filtroGrupo = 0, string busqueda = "")
        {
            MostrarResultado(asignaciones.Retirar(idEstudiante, motivo, IdUsuarioActual));
            return RedirectToAction("Asignaciones", new { grupo = filtroGrupo, busqueda });
        }
    }
}