using System;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Models.Seguridad
{
    // Aplicar a controladores/acciones, no únicamente a los enlaces del Home.
    public class PermisoAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] permisos;
        public PermisoAuthorizeAttribute(params string[] permisosRequeridos)
        {
            permisos = permisosRequeridos ?? new string[0];
        }
        protected override bool AuthorizeCore(HttpContextBase context)
        {
            if (!base.AuthorizeCore(context)) return false;
            int id;
            if (!int.TryParse(Convert.ToString(context.Session["IdUsuario"]), out id) || id <= 0)
                return false;
            try
            {
                var asignados = new PermisosService().ObtenerPermisos(id);
                foreach (string permiso in permisos)
                    if (asignados.Contains(permiso)) return true;
                return false;
            }
            catch (Exception)
            {
                return false; // No conceder acceso si falla la consulta.
            }
        }
    }
}
