using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Estudiantes;
using ExcelDataReader;
using System.Data;
using System.IO;

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
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Estudiantes/Crear.cshtml", estudiante);
            }
        }

        public ActionResult Index(
            string filtroEstado = "Activos",
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


        public ActionResult Detalle(int id)
        {
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            return View(
                "~/Views/Estudiantes/Detalle.cshtml",
                estudiante);
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            return View(
                "~/Views/Estudiantes/Editar.cshtml",
                estudiante);
        }

        [HttpPost]
        public ActionResult Editar(Estudiante estudiante)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(
                        "~/Views/Estudiantes/Editar.cshtml",
                        estudiante);
                }

                string resultado =
                    repository.Actualizar(estudiante);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Estudiante actualizado correctamente.";

                    return RedirectToAction("Index");
                }

                ViewBag.Error = resultado;

                return View(
                    "~/Views/Estudiantes/Editar.cshtml",
                    estudiante);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;

                return View(
                    "~/Views/Estudiantes/Editar.cshtml",
                    estudiante);
            }
        }

        [HttpPost]
        public ActionResult DarBaja(int id, string observacionBaja)
        {
            string resultado = repository.DarBaja(id, observacionBaja);

            if (resultado == "Se ha guardado correctamente")
            {
                TempData["Success"] = "El estudiante fue dado de baja correctamente.";
            }
            else
            {
                TempData["Error"] =
                    resultado;
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult RegistrarAdecuacion(
        AdecuacionAcademica adecuacion)
        {
            try
            {
                string resultado =
                    repository.GuardarAdecuacion(
                        adecuacion);
                //throw new Exception(resultado);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] =
                        "Adecuación registrada correctamente.";
                }
                else
                {
                    TempData["Error"] =
                        resultado;
                }

                return RedirectToAction(
                    "Expediente",
                    new
                    {
                        id = adecuacion.IdEstudiante
                    });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;

                return RedirectToAction(
                    "Expediente",
                    new
                    {
                        id = adecuacion.IdEstudiante
                    });
            }
        }


        [HttpGet]
        public ActionResult Expediente(int id)
        {
            Estudiante estudiante =
                repository.ObtenerPorId(id);

            if (estudiante == null)
            {
                return HttpNotFound();
            }

            ViewBag.TiposAdecuacion =
                repository.ObtenerTiposAdecuacion();

            ViewBag.Adecuaciones =
                repository.ObtenerAdecuaciones(id);

            ViewBag.Alergias =
                repository.ObtenerAlergias(id);

            ViewBag.Observaciones =
                repository.ObtenerObservaciones(id);

            return View(
                "~/Views/Estudiantes/Expediente.cshtml",
                estudiante);
        }

        [HttpPost]
        public ActionResult EditarAdecuacion(
    AdecuacionAcademica adecuacion)
        {
            string resultado =
                repository.ActualizarAdecuacion(
                    adecuacion);

            if (resultado == "Se ha guardado correctamente")
            {
                TempData["Success"] =
                    "Adecuación actualizada correctamente.";
            }
            else
            {
                TempData["Error"] =
                    resultado;
            }

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = adecuacion.IdEstudiante
                });
        }



        [HttpPost]
        public ActionResult RegistrarAlergia(
        Alergia alergia)
        {
            string resultado =
                repository.GuardarAlergia(
                    alergia);

            if (resultado == "Se ha guardado correctamente")
            {
                TempData["Success"] =
                    "Alergia registrada correctamente.";
            }
            else
            {
                TempData["Error"] =
                    resultado;
            }

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = alergia.IdEstudiante
                });
        }

        [HttpPost]
        public ActionResult EditarAlergia(Alergia alergia)
        {
            string resultado =
                repository.ActualizarAlergia(alergia);

            TempData["Success"] =
                "Alergia actualizada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = alergia.IdEstudiante
                });
        }

        [HttpPost]
        public ActionResult EliminarAlergia(int idAlergia, int idEstudiante)
        {
            repository.EliminarAlergia(idAlergia);

            TempData["Success"] =
                "Alergia eliminada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = idEstudiante
                });
        }



        [HttpPost]
        public ActionResult RegistrarObservacion()
        {
            Observaciones observacion =
                new Observaciones();

            observacion.IdEstudiante =
                Convert.ToInt32(
                    Request.Form["IdEstudiante"]);

            observacion.Titulo =
                Request.Form["Titulo"];

            observacion.Observacion =
                Request.Form["Observacion"];

            string resultado =
                repository.GuardarObservacion(
                    observacion);

            TempData["Success"] =
                "Observación registrada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = observacion.IdEstudiante
                });
        }

        [HttpPost]
        public ActionResult EditarObservacion(
    FormCollection form)
        {
            Observaciones observacion =
                new Observaciones();

            observacion.IdObservacion =
                Convert.ToInt32(
                    form["IdObservacion"]);

            observacion.IdEstudiante =
                Convert.ToInt32(
                    form["IdEstudiante"]);

            observacion.Titulo =
                form["Titulo"];

            observacion.Observacion =
                form["Observacion"];

            repository.ActualizarObservacion(
                observacion);

            TempData["Success"] =
                "Observación actualizada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = observacion.IdEstudiante
                });
        }


        [HttpPost]
        public ActionResult EliminarObservacion(
        int idObservacion,
        int idEstudiante)
        {
            repository.EliminarObservacion(
                idObservacion);

            TempData["Success"] =
                "Observación eliminada correctamente.";

            return RedirectToAction(
                "Expediente",
                new
                {
                    id = idEstudiante
                });
        }


        public ActionResult HistorialBajas()
        {
            var lista =
                repository.ObtenerHistorialBajas();

            if (lista == null)
            {
                throw new Exception(
                    "ObtenerHistorialBajas devolvió NULL");
            }

            ViewBag.HistorialBajas = lista;
            ViewBag.TotalBajas = lista.Count;

            return View(
                "~/Views/Estudiantes/HistorialBajas.cshtml");
        }


        [HttpGet]
        public ActionResult ImportarExcel()
        {
            ViewBag.Preview =
                Session["EstudiantesPreview"];

            return View(
                "~/Views/Estudiantes/ImportarExcel.cshtml");
        }

        [HttpPost]
        public ActionResult ImportarExcel(
    HttpPostedFileBase archivoExcel,
    string accion)
        {
            if (archivoExcel == null)
            {
                TempData["Error"] =
                    "Seleccione un archivo Excel.";

                return View(
                    "~/Views/Estudiantes/ImportarExcel.cshtml");
            }

            List<Estudiante> preview =
                new List<Estudiante>();

            using (var stream =
                archivoExcel.InputStream)
            {
                using (var reader =
                    ExcelDataReader.ExcelReaderFactory
                    .CreateReader(stream))
                {
                    DataSet ds =
                        reader.AsDataSet();

                    DataTable tabla =
                        ds.Tables[0];

                    for (int fila = 1;
                         fila < tabla.Rows.Count;
                         fila++)
                    {
                        DataRow row =
                            tabla.Rows[fila];

                        Estudiante estudiante =
                            new Estudiante();

                        estudiante.Nombre =
                            row[0].ToString();

                        estudiante.Apellido1 =
                            row[1].ToString();

                        estudiante.Apellido2 =
                            row[2].ToString();

                        estudiante.Identificacion =
                            row[3].ToString();

                        estudiante.FechaNacimiento =
                            Convert.ToDateTime(row[4]);

                        estudiante.NombreEncargado =
                            row[5].ToString();

                        estudiante.Parentesco =
                            row[6].ToString();

                        estudiante.TelefonoEncargado =
                            row[7].ToString();

                        estudiante.CorreoEncargado =
                            row[8].ToString();

                        estudiante.Direccion =
                            row[9].ToString();

                        preview.Add(estudiante);
                    }
                }
            }

            if (accion == "Cargar")
            {
                Session["EstudiantesPreview"] =
                    preview;

                ViewBag.Preview =
                    preview;

                return View(
                    "~/Views/Estudiantes/ImportarExcel.cshtml");
            }
            if (accion == "Importar")
            {
                var estudiantes =
                    Session["EstudiantesPreview"]
                    as List<Estudiante>;

                if (estudiantes == null)
                {
                    TempData["Error"] =
                        "Debe cargar un archivo antes de importar.";

                    return RedirectToAction(
                        "ImportarExcel");
                }

                int guardados = 0;

                foreach (var estudiante in estudiantes)
                {
                    if (repository.ExisteIdentificacion(
                        estudiante.Identificacion))
                    {
                        continue;
                    }

                    string resultado =
                        repository.Guardar(
                            estudiante);

                    if (resultado ==
                        "Se ha guardado correctamente")
                    {
                        guardados++;
                    }
                }

                Session.Remove(
                    "EstudiantesPreview");

                TempData["Success"] =
                    guardados +
                    " estudiantes importados correctamente.";

                return RedirectToAction(
                    "Index");
            }

            TempData["Success"] =
                preview.Count +
                " estudiantes importados correctamente.";

            return RedirectToAction("Index");
        }
    }
}


