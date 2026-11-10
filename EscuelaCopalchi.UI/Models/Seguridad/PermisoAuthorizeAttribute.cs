
using EscuelaCopalchi.UI.Controllers;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Models.Seguridad
{
    public class PermisoAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] permisos;

        public PermisoAuthorizeAttribute(
            params string[] permisosRequeridos)
        {
            permisos = permisosRequeridos
                ?? new string[0];
        }

        protected override bool AuthorizeCore(
            HttpContextBase context)
        {
            if (!base.AuthorizeCore(context))
                return false;

            try
            {
                var usuario =
                    SesionUsuario.Obtener(context.Session);

                if (usuario == null ||
                    usuario.IdUsuario <= 0)
                {
                    return false;
                }

                if (permisos.Length == 0)
                    return false;

                return permisos.Any(
                    permiso => usuario.TienePermiso(permiso));
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(
            AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User == null ||
                !filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
                return;
            }

            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(
                    new
                    {
                        controller = "Error",
                        action = "AccesoDenegado"
                    }));
        }
    }
}
