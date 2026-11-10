using System;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Models.Seguridad
{
    // Autorización por permisos reales de ROL_PERMISO.
    // SoloDocente significa permiso para ESCRIBIR notas;
    // el alcance de grupos se comprueba en el controlador y repositorio.
    public sealed class NotasPermisoAttribute : AuthorizeAttribute
    {
        public bool SoloDocente { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!base.AuthorizeCore(httpContext)) return false;

            int id;
            if (httpContext.Session == null ||
                !int.TryParse(Convert.ToString(httpContext.Session["IdUsuario"]), out id) || id <= 0)
                return false;

            try
            {
                var permisos = new PermisosService().ObtenerPermisos(id);
                if (SoloDocente) return permisos.Contains("REGISTRAR_NOTAS");
                return permisos.Contains("REGISTRAR_NOTAS") ||
                       permisos.Contains("VER_TODAS_NOTAS");
            }
            catch
            {
                return false; // Ante fallos de BD, no conceder acceso.
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            int id;
            bool sesion = ctx.Session != null &&
                int.TryParse(Convert.ToString(ctx.Session["IdUsuario"]), out id) && id > 0;

            if (!sesion || ctx.User == null || ctx.User.Identity == null ||
                !ctx.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(new
                    {
                        controller = "Account", action = "Login"
                    }));
            }
            else
            {
                filterContext.Result = new HttpStatusCodeResult(403,
                    "No tiene permisos para acceder a esta función de Gestión de Notas.");
            }
        }
    }
}
