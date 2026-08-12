using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class UsuariosController : Controller
    {
        public ActionResult Index() => View();
        public ActionResult Crear() => View();
        public ActionResult Editar() => View();
    }
}
