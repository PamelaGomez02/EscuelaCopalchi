using System;
using System.Linq;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// HU "Como docente, quiero ver mi grupo con datos completos" y
    /// HU "Como docente, quiero consultar el horario y aula asignada para cada grupo que tengo a cargo".
    /// </summary>
    public partial class GestionAcademicaController
    {
        [PermisoRequerido(Permisos.VerMiGrupo)]
        public ActionResult MiGrupo(int? id)
        {
            var modelo = new MiGrupoViewModel();

            try
            {
                modelo.Docente = usuarios.Obtener(IdUsuarioActual);
                modelo.Grupos = grupos.ListarDelDocente(modelo.Docente != null ? modelo.Docente.IdDocente : 0);

                if (modelo.TieneGrupos)
                {
                    // Solo puede consultar sus propios grupos
                    modelo.GrupoSeleccionado = modelo.Grupos.FirstOrDefault(g => g.IdGrupo == id) ?? modelo.Grupos.First();

                    if (id.HasValue && id.Value != modelo.GrupoSeleccionado.IdGrupo)
                        TempData["Advertencia"] = "El grupo solicitado no está asignado a usted. Se muestra su primer grupo.";

                    modelo.Estudiantes = asignaciones.ListarEstudiantesDelGrupo(modelo.GrupoSeleccionado.IdGrupo);
                    modelo.Horario = modelo.GrupoSeleccionado.HorarioDias;
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorCarga = ex.Message;
            }

            return View(modelo);
        }
    }
}