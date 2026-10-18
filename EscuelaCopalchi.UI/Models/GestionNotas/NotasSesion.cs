using System;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace EscuelaCopalchi.UI.Models.GestionNotas
{
    // Usuario que trabaja en Gestión de notas (sesión propia del módulo, independiente de Gestión Académica)
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

        private const string K = "GN_";

        public static NotasSesion Obtener(HttpSessionStateBase session)
        {
            if (session == null || session[K + "Id"] == null) return null;

            return new NotasSesion
            {
                IdUsuario = Convert.ToInt32(session[K + "Id"]),
                Nombre = Convert.ToString(session[K + "Nombre"]),
                Rol = Convert.ToString(session[K + "Rol"]),
                IdDocente = Convert.ToInt32(session[K + "Docente"])
            };
        }

        public static void Guardar(HttpSessionStateBase session, UsuarioAccesoItem u)
        {
            session[K + "Id"] = u.IdUsuario;
            session[K + "Nombre"] = u.NombreCompleto;
            session[K + "Rol"] = u.Rol;
            session[K + "Docente"] = u.IdDocente;
        }

        public static void Cerrar(HttpSessionStateBase session)
        {
            session.Remove(K + "Id");
            session.Remove(K + "Nombre");
            session.Remove(K + "Rol");
            session.Remove(K + "Docente");
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

    // Exige haber iniciado sesión en Gestión de notas. SoloDocente = true: además debe ser docente.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class NotasAccesoAttribute : ActionFilterAttribute
    {
        public bool SoloDocente { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string accion = filterContext.ActionDescriptor.ActionName;
            if (string.Equals(accion, "Acceso", StringComparison.OrdinalIgnoreCase)
                || string.Equals(accion, "Salir", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            NotasSesion s = NotasSesion.Obtener(filterContext.HttpContext.Session);

            if (s == null)
            {
                filterContext.Controller.TempData["Error"] = "Seleccione su usuario e ingrese su contraseña para usar Gestión de notas.";
                filterContext.Result = Ir("Acceso");
                return;
            }

            filterContext.Controller.ViewBag.NotasSesion = s;

            if (SoloDocente && !s.PuedeRegistrar)
            {
                filterContext.Controller.TempData["Error"] = "Su rol no tiene permiso para registrar, importar o modificar calificaciones.";
                filterContext.Result = Ir("Index");
            }
        }

        private static RedirectToRouteResult Ir(string accion)
        {
            return new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "GestionNotas" },
                { "action", accion }
            });
        }
    }
}
