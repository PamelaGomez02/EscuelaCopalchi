using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class InventarioCocinaController : Controller
    {
        // GET: InventarioCocina
        public ActionResult Index() {return View();}
        public ActionResult Create() {return View();}
    }
}