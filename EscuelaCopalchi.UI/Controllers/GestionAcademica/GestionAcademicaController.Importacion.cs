using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Controllers
{
    /// <summary>
    /// HU "Como director, quiero importar asignaciones desde Excel para agilizar la carga masiva de grupos y docentes".
    /// Paso 1: VistaPreviaImportacion (lee y valida, no guarda). Paso 2: ConfirmarImportacion o CancelarImportacion.
    /// </summary>
    public partial class GestionAcademicaController
    {
        private const string SesionImportacion = "GA_Importacion";
        private const string SesionArchivoImportacion = "GA_ImportacionArchivo";

        private readonly ImportacionExcelService importacion = new ImportacionExcelService();

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult ImportarExcel()
        {
            var modelo = new ImportacionViewModel
            {
                Filas = Session[SesionImportacion] as List<FilaImportacion> ?? new List<FilaImportacion>(),
                NombreArchivo = Session[SesionArchivoImportacion] as string
            };

            return View(modelo);
        }

        /// <summary>Paso 1: leer y validar el archivo; se muestra la vista previa sin guardar nada.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult VistaPreviaImportacion(HttpPostedFileBase archivo)
        {
            Session.Remove(SesionImportacion);
            Session.Remove(SesionArchivoImportacion);

            if (archivo == null || archivo.ContentLength == 0)
            {
                TempData["Error"] = "Seleccione un archivo Excel.";
                return RedirectToAction("ImportarExcel");
            }

            string extension = Path.GetExtension(archivo.FileName ?? "").ToLowerInvariant();

            if (extension == ".xls")
            {
                TempData["Error"] = "Formato incorrecto: el archivo está en formato Excel 97-2003 (.xls). " +
                                    "Ábralo en Excel y use \"Guardar como\" → Libro de Excel (.xlsx).";
                return RedirectToAction("ImportarExcel");
            }

            if (extension != ".xlsx")
            {
                TempData["Error"] = "Formato incorrecto: solo se permiten archivos Excel (.xlsx).";
                return RedirectToAction("ImportarExcel");
            }

            if (archivo.ContentLength > 5 * 1024 * 1024)
            {
                TempData["Error"] = "El archivo supera el tamaño máximo de 5 MB.";
                return RedirectToAction("ImportarExcel");
            }

            try
            {
                List<FilaImportacion> filas;

                using (var copia = new MemoryStream())
                {
                    archivo.InputStream.CopyTo(copia);
                    copia.Position = 0;
                    filas = importacion.LeerYValidar(copia);
                }

                Session[SesionImportacion] = filas;
                Session[SesionArchivoImportacion] = Path.GetFileName(archivo.FileName);

                int invalidas = filas.Count(f => !f.Valida);
                if (invalidas > 0)
                    TempData["Advertencia"] = "Se encontraron " + invalidas + " registros con inconsistencias. " +
                                              "Solo se importarán los registros válidos.";
            }
            catch (FormatException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo leer el archivo: " + ex.Message;
            }

            return RedirectToAction("ImportarExcel");
        }

        /// <summary>Paso 2: guardar los registros válidos de la vista previa.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult ConfirmarImportacion()
        {
            var filas = Session[SesionImportacion] as List<FilaImportacion>;

            if (filas == null || filas.Count == 0)
            {
                TempData["Error"] = "No hay una importación pendiente. Vuelva a cargar el archivo.";
                return RedirectToAction("ImportarExcel");
            }

            ResultadoOperacion resultado = importacion.Importar(filas, IdUsuarioActual);

            if (resultado.Exito)
            {
                Session.Remove(SesionImportacion);
                Session.Remove(SesionArchivoImportacion);
            }
            else
            {
                resultado.Mensaje = "No se guardó ningún registro. " + resultado.Mensaje;
            }

            MostrarResultado(resultado);
            return RedirectToAction(resultado.Exito ? "Docentes" : "ImportarExcel");
        }

        /// <summary>Cancela la importación: descarta la vista previa sin guardar cambios.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult CancelarImportacion()
        {
            Session.Remove(SesionImportacion);
            Session.Remove(SesionArchivoImportacion);

            TempData["Advertencia"] = "Importación cancelada. No se guardó ningún cambio.";
            return RedirectToAction("ImportarExcel");
        }

        [PermisoRequerido(Permisos.GestionarAcademica)]
        public ActionResult DescargarPlantilla()
        {
            return File(ImportacionExcelService.GenerarPlantilla(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Plantilla_asignaciones.xlsx");
        }
    }
}