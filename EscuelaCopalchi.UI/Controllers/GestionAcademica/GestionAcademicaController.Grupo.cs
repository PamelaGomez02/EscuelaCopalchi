using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// Aulas, grupos, horarios y docente responsable.
    /// HU "Docente consulta horario y aula" (se definen aquí) y HU "Notificaciones al docente"
    /// (nueva asignación, cambio de aula y de horario se generan al guardar).
    /// </summary>
    public partial class GestionAcademicaController
    {
        private readonly AulaService aulas = new AulaService();

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult Aulas()
        {
            var lista = new List<Aula>();

            try { lista = aulas.Listar(); }
            catch (Exception ex) { ViewBag.ErrorCarga = ex.Message; }

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult GuardarAula(int idAula, string nombre, int? capacidad, string ubicacion, bool estado = true)
        {
            var aula = new Aula
            {
                IdAula = idAula,
                Nombre = nombre,
                Capacidad = capacidad ?? 0,
                Ubicacion = ubicacion,
                Estado = estado
            };

            MostrarResultado(aulas.Guardar(aula));
            return RedirectToAction("Aulas");
        }

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult Docentes(int periodo = 0)
        {
            var modelo = new DocentesViewModel { Periodo = periodo };

            try
            {
                modelo.Periodos = grupos.ListarPeriodos();
                modelo.Grupos = grupos.Listar(periodo);
                modelo.Docentes = usuarios.ListarDocentes();
                modelo.Aulas = aulas.ListarActivas();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult GuardarGrupo(int idGrupo, string nombre, string nivel, int periodo, int cupo = 0,
                                         int idAula = 0, int idDocente = 0, int[] dias = null,
                                         string horaInicio = "", string horaFin = "", int periodoFiltro = 0)
        {
            var grupo = new Grupo
            {
                IdGrupo = idGrupo,
                Nombre = nombre,
                Nivel = nivel,
                Periodo = periodo,
                Cupo = cupo,
                IdAula = idAula,
                IdDocente = idDocente,
                Dias = dias == null ? "" : string.Join(",", dias.Where(d => d >= 1 && d <= 7).Distinct().OrderBy(d => d)),
                HoraInicio = horaInicio ?? "",
                HoraFin = horaFin ?? ""
            };

            MostrarResultado(grupos.Guardar(grupo, IdUsuarioActual));
            return RedirectToAction("Docentes", new { periodo = periodoFiltro });
        }

        /// <param name="idDocente">Id de la tabla DOCENTE; 0 quita el docente.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult AsignarDocente(int idGrupo, int idDocente = 0, int periodoFiltro = 0)
        {
            MostrarResultado(grupos.AsignarDocente(idGrupo, idDocente, IdUsuarioActual));
            return RedirectToAction("Docentes", new { periodo = periodoFiltro });
        }
    }
}