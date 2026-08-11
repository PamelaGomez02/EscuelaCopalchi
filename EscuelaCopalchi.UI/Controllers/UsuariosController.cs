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
        public ActionResult Create() => View();
        public ActionResult Edit() => View();
    }
}
