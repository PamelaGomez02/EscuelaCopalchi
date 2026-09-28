using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// Usuario con el que se trabaja, guardado en Session["IdUsuario"], Session["NombreUsuario"],
    /// Session["Rol"] y Session["Permisos"]. Cuando exista el login, basta con llamar a
    /// SesionUsuario.Guardar(Session, usuario) al iniciar sesión.
    /// </summary>
    public static class SesionUsuario
    {
        public const string ClaveId = "IdUsuario";
        public const string ClaveNombre = "NombreUsuario";
        public const string ClaveRol = "Rol";
        public const string ClavePermisos = "Permisos";

        public static UsuarioSesion Obtener(HttpSessionStateBase session)
        {
            if (session == null || session[ClaveId] == null) return null;

            int id;
            if (!int.TryParse(Convert.ToString(session[ClaveId]), out id) || id <= 0) return null;

            return new UsuarioSesion
            {
                IdUsuario = id,
                Nombre = Convert.ToString(session[ClaveNombre]),
                Rol = Convert.ToString(session[ClaveRol]),
                Permisos = session[ClavePermisos] as List<string> ?? new List<string>()
            };
        }

        public static void Guardar(HttpSessionStateBase session, UsuarioSistema usuario)
        {
            session[ClaveId] = usuario.IdUsuario;
            session[ClaveNombre] = usuario.NombreCompleto;
            session[ClaveRol] = usuario.Rol;
            session[ClavePermisos] = usuario.Permisos;
        }

        public static void Cerrar(HttpSessionStateBase session)
        {
            session.Remove(ClaveId);
            session.Remove(ClaveNombre);
            session.Remove(ClaveRol);
            session.Remove(ClavePermisos);
        }
    }

    /// <summary>
    /// Exige que el rol del usuario tenga al menos uno de los permisos indicados (tabla ROL_PERMISO).
    /// Los permisos se vuelven a leer de la base en cada solicitud, así un cambio hecho en
    /// Roles → Permisos se aplica de inmediato. Uso: [PermisoRequerido(Permisos.GestionarAcademica)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class PermisoRequeridoAttribute : ActionFilterAttribute
    {
        private readonly string[] permisos;

        public PermisoRequeridoAttribute(params string[] permisos)
        {
            this.permisos = permisos ?? new string[0];
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpSessionStateBase session = filterContext.HttpContext.Session;
            UsuarioSesion sesion = SesionUsuario.Obtener(session);
            string mensaje = null;

            if (sesion == null)
            {
                mensaje = "Seleccione con qué usuario desea trabajar para usar esta opción.";
            }
            else
            {
                UsuarioSistema usuario = null;
                try { usuario = new UsuarioService().Obtener(sesion.IdUsuario); }
                catch (Exception) { /* sin base de datos: se deja pasar y la acción mostrará el error */ return; }

                if (usuario == null || !usuario.Estado)
                {
                    SesionUsuario.Cerrar(session);
                    mensaje = "El usuario ya no existe o está inactivo.";
                }
                else
                {
                    SesionUsuario.Guardar(session, usuario);   // refresca rol y permisos

                    if (permisos.Length > 0 && !permisos.Any(usuario.TienePermiso))
                        mensaje = "Su rol (" + usuario.Rol + ") no tiene permiso para esta opción. " +
                                  "Se asigna en Roles → Permisos.";
                }
            }

            if (mensaje == null) return;

            filterContext.Controller.TempData["Error"] = mensaje;
            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "GestionAcademica" },
                { "action", "Index" }
            });
        }
    }
}