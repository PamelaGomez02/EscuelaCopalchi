
using System.Web.Mvc;

namespace EscuelaCopalchi.UI
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(
            GlobalFilterCollection filters)
        {
            // Manejo general de errores
            filters.Add(new HandleErrorAttribute());

            // Exigir inicio de sesión en el sistema
            filters.Add(new AuthorizeAttribute());
        }
    }
}
