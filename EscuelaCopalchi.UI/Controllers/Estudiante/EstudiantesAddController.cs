using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Estudiantes;

namespace EscuelaCopalchi.UI.Controllers
{
    public class EstudiantesAddController : Controller
    {
        private readonly EstudianteRepository repository;

        public EstudiantesAddController()
        {
            repository = new EstudianteRepository();
        }

        [HttpGet]
        public ActionResult Registrar()
        {
            return View("~/Views/Estudiantes/Crear.cshtml");
        }

        [HttpPost]
        public ActionResult Registrar(Estudiante estudiante)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = string.Join(
                        "<br>",
                        ModelState.Values
                                  .SelectMany(v => v.Errors)
                                  .Select(e => e.ErrorMessage));

                    ViewBag.Error = errores;

                    return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
                }

                string resultado =
                    repository.Guardar(estudiante);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Estudiante registrado correctamente.";

                    return RedirectToAction("Registrar");
                }

                ViewBag.Error = resultado;

                return View("~/Views/Estudiantes/Crear.cshtml", estudiante);


                return RedirectToAction("Registrar");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
            }
        }

        public ActionResult Index(
            string filtroEstado = "Todos",
            string busqueda = "")
        {
            var estudiantes = repository.ObtenerTodos();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                estudiantes = estudiantes
                    .Where(x =>
                        (x.Nombre + " " + x.Apellido1 + " " + x.Apellido2)
                        .ToLower()
                        .Contains(busqueda.ToLower())
                        ||
                        x.Identificacion.ToLower()
                        .Contains(busqueda.ToLower())
                        ||
                        x.NombreEncargado.ToLower()
                        .Contains(busqueda.ToLower()))
                    .ToList();
            }

            if (filtroEstado == "Activos")
            {
                estudiantes = estudiantes
                    .Where(x => x.Estado)
                    .ToList();
            }
            else if (filtroEstado == "Inactivos")
            {
                estudiantes = estudiantes
                    .Where(x => !x.Estado)
                    .ToList();
            }

            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.Busqueda = busqueda;

            return View(
                "~/Views/Estudiantes/Index.cshtml",
                estudiantes);
        }
    }
}


