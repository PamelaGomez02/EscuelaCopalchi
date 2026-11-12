using EscuelaCopalchi.UI.Models.Reportes;
using EscuelaCopalchi.UI.Models.Traslados;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    // Permisos administrados desde Roles y Permisos; no utiliza ReportesSesion.
    [Authorize]
    public class TrasladosController : Controller
    {
        // Los permisos se consultan en la base de datos y se asignan desde Roles y Permisos.
        // GESTIONAR_TRASLADOS permite ingresar; las operaciones requieren su permiso propio.
        private bool TienePermiso(string permiso)
        {
            int id = IdUsuarioActual();
            if (id <= 0) return false;

            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString))
            using (var cmd = new SqlCommand(@"SELECT COUNT(1) FROM dbo.USUARIO u
                INNER JOIN dbo.ROL r ON r.id_rol = u.id_rol
                INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol = r.id_rol
                INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                WHERE u.id_usuario = @id AND u.estado = 1
                  AND r.estado = 1 AND p.estado = 1 AND p.nombre = @permiso", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 100).Value = permiso;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            if (IdUsuarioActual() <= 0)
            {
                context.Result = new HttpStatusCodeResult(401, "Usuario no identificado");
                return;
            }
            if (!TienePermiso("GESTIONAR_TRASLADOS"))
            {
                context.Result = new HttpStatusCodeResult(403, "Sin permiso para acceder a Gestión de Traslados");
                return;
            }
            base.OnActionExecuting(context);
        }

        private readonly ReportesRepository reportes = new ReportesRepository();
        private readonly FichaRepository ficha = new FichaRepository();
        private readonly CartaRepository cartas = new CartaRepository();
        private const int MaxCarta = 4 * 1024 * 1024;
        private const string TipoDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        // Adaptar esta única función a la clave de sesión del módulo de Usuarios.
        // No permite registrar cartas con un usuario inventado.
        private int IdUsuarioActual()
        {
            object valor = Session["IdUsuario"];
            int id;
            return valor != null && int.TryParse(Convert.ToString(valor), out id) && id > 0 ? id : 0;
        }

        public ActionResult Index(string buscar = "")
        {
            ViewBag.PuedeGenerar = TienePermiso("GENERAR_CARTAS_TRASLADO");
            ViewBag.PuedeCargar = TienePermiso("CARGAR_CARTAS_TRASLADO");
            ViewBag.PuedeVer = TienePermiso("VER_CARTAS_TRASLADO");
            var vm = new TrasladoViewModel { Buscar = (buscar ?? "").Trim() };
            try
            {
                vm.Estudiantes = reportes.Rendimiento(0, 0, vm.Buscar)
                    .OrderBy(x => x.Estudiante).ToList();
                if (ViewBag.PuedeVer) vm.Cartas = cartas.Listar(0);
            }
            catch (Exception ex) { ViewBag.Error = "No se pudo cargar Gestión de Traslados: " + ex.Message; }
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult GenerarCarta(int idEstudiante, string destino, string motivo, string lugar)
        {
            if (!TienePermiso("GENERAR_CARTAS_TRASLADO"))
                return new HttpStatusCodeResult(403, "Sin permiso para generar cartas de traslado");
            int usuario = IdUsuarioActual();
            if (usuario == 0) { TempData["Error"] = "No se pudo identificar el usuario. Revise la integración con el módulo de Usuarios."; return RedirectToAction("Index"); }
            if (idEstudiante <= 0 || string.IsNullOrWhiteSpace(destino))
            { TempData["Error"] = "Seleccione un estudiante e indique la institución de destino."; return RedirectToAction("Index"); }
            try
            {
                var datos = ficha.Datos(0, idEstudiante);
                if (datos == null) { TempData["Error"] = "Estudiante no encontrado."; return RedirectToAction("Index"); }
                var notas = ficha.Notas(idEstudiante);
                // No emitir un documento académico oficial sin historial verificable.
                if (notas == null || notas.Count == 0)
                { TempData["Error"] = "No se puede generar la carta: el estudiante no tiene historial académico activo registrado."; return RedirectToAction("Index"); }
                byte[] doc = CartaDocx.Generar(datos, notas, ficha.Asistencia(idEstudiante),
                    destino.Trim(), motivo, lugar, "Director(a)", DateTime.Now);
                string nombre = CartaDocx.NombreArchivo(datos.NombreCompleto);
                string resultado = cartas.Guardar(usuario, idEstudiante, "Generada", destino.Trim(), motivo, nombre, doc);
                if (resultado != "Se ha guardado correctamente")
                { TempData["Error"] = "No se pudo guardar la carta: " + resultado; return RedirectToAction("Index"); }
                return File(doc, TipoDocx, nombre);
            }
            catch (Exception ex) { TempData["Error"] = "No se pudo generar la carta: " + ex.Message; return RedirectToAction("Index"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CargarCarta(int idEstudiante, string destino, HttpPostedFileBase archivo)
        {
            if (!TienePermiso("CARGAR_CARTAS_TRASLADO"))
                return new HttpStatusCodeResult(403, "Sin permiso para cargar cartas de traslado");
            int usuario = IdUsuarioActual();
            if (usuario == 0) { TempData["Error"] = "No se pudo identificar el usuario. Revise la integración con Usuarios."; return RedirectToAction("Index"); }
            if (idEstudiante <= 0 || ficha.Datos(0, idEstudiante) == null)
            { TempData["Error"] = "Estudiante inválido."; return RedirectToAction("Index"); }
            if (archivo == null || archivo.ContentLength == 0 || archivo.ContentLength > MaxCarta)
            { TempData["Error"] = "Seleccione un archivo de hasta 4 MB."; return RedirectToAction("Index"); }
            try
            {
                string nombre = Path.GetFileName(archivo.FileName);
                string ext = Path.GetExtension(nombre).ToLowerInvariant();
                byte[] bytes;
                using (var reader = new BinaryReader(archivo.InputStream)) bytes = reader.ReadBytes(archivo.ContentLength);
                bool valido = bytes.Length > 4 &&
                    ((ext == ".pdf" && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) ||
                     (ext == ".docx" && bytes[0] == 0x50 && bytes[1] == 0x4B) ||
                     (ext == ".doc" && bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0));
                if (!valido) { TempData["Error"] = "Formato inválido: solo PDF o Word."; return RedirectToAction("Index"); }
                string resultado = cartas.Guardar(usuario, idEstudiante, "Cargada", destino, "", nombre, bytes);
                TempData[resultado == "Se ha guardado correctamente" ? "Success" : "Error"] =
                    resultado == "Se ha guardado correctamente" ? "Carta cargada correctamente." : resultado;
            }
            catch (Exception ex) { TempData["Error"] = "No se pudo cargar la carta: " + ex.Message; }
            return RedirectToAction("Index");
        }

        public ActionResult DescargarCarta(int id)
        {
            if (!TienePermiso("VER_CARTAS_TRASLADO"))
                return new HttpStatusCodeResult(403, "Sin permiso para consultar cartas de traslado");
            if (id <= 0) return new HttpStatusCodeResult(400);
            try
            {
                string nombre;
                byte[] contenido = cartas.Obtener(id, out nombre);
                if (contenido == null) return HttpNotFound();
                nombre = Path.GetFileName(nombre ?? "carta.docx");
                string ext = Path.GetExtension(nombre).ToLowerInvariant();
                string tipo = ext == ".pdf" ? "application/pdf" : ext == ".doc" ? "application/msword" : TipoDocx;
                return File(contenido, tipo, nombre);
            }
            catch (Exception ex) { TempData["Error"] = "No se pudo descargar la carta: " + ex.Message; return RedirectToAction("Index"); }
        }
    }
}
