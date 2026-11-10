using System;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace EscuelaCopalchi.UI.Models.GestionNotas
{
    // Usuario que trabaja en Gestión de notas: se toma del inicio de sesión general (Account/Login)
    public class NotasSesion
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public int IdDocente { get; set; }

        private static string Norm(string s)
        {
            string d = (s ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString();
        }

        public bool EsDocente { get { return Norm(Rol) == "docente"; } }
        public bool EsDirector { get { return Norm(Rol) == "director"; } }
        public bool EsEvaluacion { get { return Norm(Rol).StartsWith("evaluacion"); } }

        // Registran, importan y modifican notas el docente (sus grupos) y el director (todos los grupos)
        public bool PuedeRegistrar { get { return EsDocente || EsDirector; } }

        // Director y Evaluación consultan las notas de todos los grupos
        public bool VeTodo { get { return EsDirector || EsEvaluacion; } }

        // Lee al usuario de la sesión que crea Account/Login (IdUsuario, NombreUsuario, RolUsuario)
        public static NotasSesion Obtener(HttpSessionStateBase session)
        {
            if (session == null || session["IdUsuario"] == null) return null;

            int idUsuario = Convert.ToInt32(session["IdUsuario"]);
            string rol = Convert.ToString(session["RolUsuario"]);

            var s = new NotasSesion
            {
                IdUsuario = idUsuario,
                Nombre = Convert.ToString(session["NombreUsuario"]),
                Rol = rol
            };

            if (s.EsDocente)
            {
                // el id de docente se consulta una vez por usuario y se guarda en la sesión
                if (session["GN_DocenteUsuario"] == null || Convert.ToInt32(session["GN_DocenteUsuario"]) != idUsuario)
                {
                    int idDocente = 0;
                    try { idDocente = new NotasRepository().ObtenerIdDocente(idUsuario); } catch (Exception) { }
                    session["GN_DocenteUsuario"] = idUsuario;
                    session["GN_Docente"] = idDocente;
                }
                s.IdDocente = Convert.ToInt32(session["GN_Docente"]);
            }

            return s;
        }
    }

    public class UsuarioAccesoItem
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Identificacion { get; set; }
        public string Rol { get; set; }
        public int IdDocente { get; set; }
    }

    // Exige haber iniciado sesión (Account/Login) con un rol de Gestión de notas.
    // SoloDocente = true: además debe poder registrar notas (Docente o Director).
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class NotasAccesoAttribute : ActionFilterAttribute
    {
        public bool SoloDocente { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            NotasSesion s = NotasSesion.Obtener(filterContext.HttpContext.Session);

            if (s == null)
            {
                // la sesión expiró aunque la cookie siga vigente: se vuelve a iniciar sesión
                FormsAuthentication.SignOut();
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Login" },
                    { "returnUrl", filterContext.HttpContext.Request.RawUrl }
                });
                return;
            }

            if (!(s.EsDocente || s.EsDirector || s.EsEvaluacion))
            {
                filterContext.Controller.TempData["Error"] = "Su rol no tiene acceso a Gestión de notas.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                });
                return;
            }

            filterContext.Controller.ViewBag.NotasSesion = s;

            if (SoloDocente && !s.PuedeRegistrar)
            {
                filterContext.Controller.TempData["Error"] = "Su rol no tiene permiso para registrar, importar o modificar calificaciones.";
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "GestionNotas" },
                    { "action", "Index" }
                });
            }
        }
    }
}
