using EscuelaCopalchi.UI.Models.PlanesEstudio;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class PlanesEstudioController : Controller
    {
        private readonly PlanEstudioRepository repository = new PlanEstudioRepository();
        private readonly string cadena = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;
        private const int MaxBytes = 10 * 1024 * 1024;

        // Misma autenticación y asignación de permisos que RepositorioMaterialController.
        private int? UsuarioActual()
        {
            if (!Request.IsAuthenticated) return null;
            int id;
            if (Session["IdUsuario"] != null && int.TryParse(Session["IdUsuario"].ToString(), out id) && id > 0)
                return id;
            string correo = User.Identity.Name;
            if (string.IsNullOrWhiteSpace(correo)) return null;
            const string sql = "SELECT id_usuario FROM dbo.USUARIO WHERE correo=@correo AND estado=1";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@correo", SqlDbType.NVarChar, 255).Value = correo;
                cn.Open();
                object valor = cmd.ExecuteScalar();
                if (valor == null || valor == DBNull.Value) return null;
                id = Convert.ToInt32(valor);
                Session["IdUsuario"] = id;
                return id;
            }
        }
        private bool TienePermiso(string permiso)
        {
            int? id = UsuarioActual();
            if (!id.HasValue) return false;
            const string sql = @"SELECT COUNT(1) FROM dbo.USUARIO u
                JOIN dbo.ROL r ON r.id_rol=u.id_rol
                JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
                JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
                WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1 AND p.estado=1 AND p.nombre=@permiso";
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 150).Value = permiso;
                cn.Open(); return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        private ActionResult Denegado() { return new HttpStatusCodeResult(403, "No tiene permiso para esta acción."); }
        private bool PuedeVer() { return TienePermiso("VER_PLANES") || TienePermiso("CREAR_PLANES") || TienePermiso("REVISAR_PLANES") || TienePermiso("ADMINISTRAR_TODO_PLANES"); }
        private int? DocenteActual()
        {
            int? id = UsuarioActual(); if (!id.HasValue) return null;
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand("SELECT TOP (1) id_docente FROM dbo.DOCENTE WHERE id_usuario=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                cn.Open(); object v = cmd.ExecuteScalar();
                return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
            }
        }
        private bool PuedeEditar(PlanEstudio p)
        {
            return p != null && p.Estado == "Pendiente" &&
                (TienePermiso("ADMINISTRAR_TODO_PLANES") ||
                 (TienePermiso("EDITAR_PLANES") && DocenteActual() == p.IdDocente));
        }
        private void PermisosVista()
        {
            ViewBag.PuedeCrear = TienePermiso("CREAR_PLANES");
            ViewBag.PuedeRevisar = TienePermiso("REVISAR_PLANES");
            ViewBag.PuedeDescargar = TienePermiso("DESCARGAR_PLANES");
            ViewBag.PuedeAdministrar = TienePermiso("ADMINISTRAR_TODO_PLANES");
            ViewBag.PuedeEditar = TienePermiso("EDITAR_PLANES") || TienePermiso("ADMINISTRAR_TODO_PLANES");
            ViewBag.IdDocenteActual = DocenteActual() ?? 0;
        }
        private void Listas()
        {
            var docentes = repository.ObtenerDocentes();
            ViewBag.Docentes = docentes.AsEnumerable().Select(r => new SelectListItem
            {
                Value = Convert.ToString(r["id_docente"]),
                Text = Convert.ToString(r["nombre_docente"])
            }).ToList();
            var grupos = repository.ObtenerGrupos();
            ViewBag.Grupos = grupos.AsEnumerable().Select(r => new SelectListItem
            {
                Value = Convert.ToString(r["id_grupo"]),
                Text = Convert.ToString(r["nombre_grupo"])
            }).ToList();
        }
        private void PrepararCrear(PlanEstudio p)
        {
            Listas(); PermisosVista();
            int? docente = DocenteActual();
            ViewBag.DocenteVinculado = docente.HasValue;
            if (docente.HasValue) p.IdDocente = docente.Value;
            var docentes = (IEnumerable<SelectListItem>)ViewBag.Docentes;
            ViewBag.NombreDocente = docentes.Where(d => d.Value == p.IdDocente.ToString())
                .Select(d => d.Text).FirstOrDefault() ?? "Sin docente asociado";
        }
        private static void Validar(PlanEstudio p)
        {
            if (p.IdDocente <= 0) throw new Exception("Seleccione un docente válido.");
            if (p.IdGrupo <= 0) throw new Exception("Seleccione un grupo.");
            if (string.IsNullOrWhiteSpace(p.Titulo)) throw new Exception("Ingrese el título.");
            if (p.Mes < 1 || p.Mes > 12) throw new Exception("Seleccione un mes válido.");
            if (p.Anio < 2000 || p.Anio > 2100) throw new Exception("Seleccione un año válido.");
            if (string.IsNullOrWhiteSpace(p.Nivel) || string.IsNullOrWhiteSpace(p.Asignatura))
                throw new Exception("Seleccione el grado y la asignatura.");
        }
        private string GuardarArchivo(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0) throw new Exception("Seleccione un archivo.");
            if (archivo.ContentLength > MaxBytes) throw new Exception("El archivo supera 10 MB.");
            string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".pdf") throw new Exception("Solo Excel o PDF.");
            string carpeta = Server.MapPath("~/App_Data/PlanesEstudio");
            Directory.CreateDirectory(carpeta);
            string nombre = Guid.NewGuid().ToString("N") + ext;
            archivo.SaveAs(Path.Combine(carpeta, nombre));
            return nombre;
        }
        private void BorrarArchivo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return;
            string ruta = Path.Combine(Server.MapPath("~/App_Data/PlanesEstudio"), Path.GetFileName(nombre));
            if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
        }
        private static bool Ok(string r) { return string.Equals((r ?? "").Trim(), "Se ha guardado correctamente", StringComparison.OrdinalIgnoreCase); }
        [HttpGet]
        public ActionResult Index(string estado = "", string periodo = "")
        {
            if (!PuedeVer()) return Denegado();
            var todos = repository.ObtenerTodos();
            ViewBag.Pendientes = todos.Count(p => p.Estado == "Pendiente");
            ViewBag.Aprobados = todos.Count(p => p.Estado == "Aprobado");
            ViewBag.Rechazados = todos.Count(p => p.Estado == "Denegado");
            ViewBag.EstadoSeleccionado = estado; ViewBag.PeriodoSeleccionado = periodo;
            if (!string.IsNullOrWhiteSpace(estado)) todos = todos.Where(p => p.Estado == estado).ToList();
            if (!string.IsNullOrWhiteSpace(periodo)) todos = todos.Where(p => string.Equals(p.Periodo, periodo, StringComparison.OrdinalIgnoreCase)).ToList();
            PermisosVista(); return View("~/Views/PlanesEstudio/Index.cshtml", todos);
        }
        [HttpGet]
        public ActionResult Crear()
        {
            if (!TienePermiso("CREAR_PLANES")) return Denegado();
            var plan = new PlanEstudio { Anio = DateTime.Now.Year, Estado = "Pendiente" };
            PrepararCrear(plan);
            return View("~/Views/PlanesEstudio/Crear.cshtml", plan);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Crear(PlanEstudio plan, HttpPostedFileBase archivo)
        {
            if (!TienePermiso("CREAR_PLANES")) return Denegado();
            string nuevo = null;
            try
            {
                int? vinculado = DocenteActual();
                if (vinculado.HasValue) plan.IdDocente = vinculado.Value;
                // Usuarios autorizados sin registro DOCENTE pueden seleccionar docente del formulario.
                plan.Estado = "Pendiente"; plan.Observaciones = "";
                Validar(plan);
                nuevo = GuardarArchivo(archivo); plan.Archivo = nuevo;
                string resultado = repository.GuardarPlanEstudio(plan);
                if (!Ok(resultado)) throw new Exception(resultado);
                TempData["Success"] = "Plan registrado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (nuevo != null) BorrarArchivo(nuevo);
                ViewBag.Error = ex.Message; PrepararCrear(plan);
                return View("~/Views/PlanesEstudio/Crear.cshtml", plan);
            }
        }
        [HttpGet]
        public ActionResult Editar(int id)
        {
            var p = repository.ObtenerPorId(id);
            if (p == null) return HttpNotFound();
            if (!PuedeEditar(p)) return Denegado();
            Listas(); PermisosVista(); return View("~/Views/PlanesEstudio/Editar.cshtml", p);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Editar(PlanEstudio plan, HttpPostedFileBase archivo)
        {
            var actual = repository.ObtenerPorId(plan.IdPlan);
            if (actual == null) return HttpNotFound();
            if (!PuedeEditar(actual)) return Denegado();
            string nuevo = null;
            try
            {
                plan.IdDocente = actual.IdDocente; Validar(plan);
                plan.Archivo = actual.Archivo;
                if (archivo != null && archivo.ContentLength > 0) { nuevo = GuardarArchivo(archivo); plan.Archivo = nuevo; }
                string resultado = repository.ActualizarPlanEstudio(plan);
                if (!Ok(resultado)) throw new Exception(resultado);
                if (nuevo != null) BorrarArchivo(actual.Archivo);
                TempData["Success"] = "Plan actualizado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (nuevo != null) BorrarArchivo(nuevo);
                ViewBag.Error = ex.Message; Listas(); PermisosVista();
                return View("~/Views/PlanesEstudio/Editar.cshtml", plan);
            }
        }
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!PuedeVer()) return Denegado();
            var p = repository.ObtenerPorId(id); if (p == null) return HttpNotFound();
            PermisosVista(); return View("~/Views/PlanesEstudio/Detalle.cshtml", p);
        }
        [HttpGet]
        public ActionResult Revisar(int id)
        {
            if (!TienePermiso("REVISAR_PLANES")) return Denegado();
            var p = repository.ObtenerPorId(id); if (p == null) return HttpNotFound();
            return View("~/Views/PlanesEstudio/Revisar.cshtml", p);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Revisar(PlanEstudio plan, string accion)
        {
            if (!TienePermiso("REVISAR_PLANES")) return Denegado();
            var actual = repository.ObtenerPorId(plan.IdPlan); if (actual == null) return HttpNotFound();
            if (actual.Estado != "Pendiente") return new HttpStatusCodeResult(409, "El plan ya fue revisado.");
            try
            {
                int? usuario = UsuarioActual(); if (!usuario.HasValue) return Denegado();
                plan.IdUsuarioRevision = usuario.Value;
                if (accion == "Aprobar") { plan.Estado = "Aprobado"; plan.Observaciones = ""; }
                else if (accion == "Denegar")
                {
                    plan.Estado = "Denegado";
                    if (string.IsNullOrWhiteSpace(plan.Observaciones)) throw new Exception("Ingrese una observación para denegar.");
                }
                else throw new Exception("Acción inválida.");
                string resultado = repository.RevisarPlanEstudio(plan);
                if (!Ok(resultado)) throw new Exception(resultado);
                TempData["Success"] = "Revisión registrada correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message; actual.Observaciones = plan.Observaciones;
                return View("~/Views/PlanesEstudio/Revisar.cshtml", actual);
            }
        }
        private ActionResult ArchivoPlan(int id, bool inline)
        {
            if (!(inline ? PuedeVer() : TienePermiso("DESCARGAR_PLANES"))) return Denegado();
            var p = repository.ObtenerPorId(id);
            if (p == null || string.IsNullOrWhiteSpace(p.Archivo)) return HttpNotFound();
            string nombre = Path.GetFileName(p.Archivo);
            string ruta = Path.Combine(Server.MapPath("~/App_Data/PlanesEstudio"), nombre);
            // Compatibilidad con archivos antiguos guardados en /Uploads/Planes.
            if (!System.IO.File.Exists(ruta)) ruta = Path.Combine(Server.MapPath("~/Uploads/Planes"), nombre);
            if (!System.IO.File.Exists(ruta)) return HttpNotFound();
            string ext = Path.GetExtension(nombre).ToLowerInvariant();
            string mime = ext == ".pdf" ? "application/pdf" : ext == ".xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/vnd.ms-excel";
            if (inline && ext == ".pdf") { Response.Headers["X-Content-Type-Options"] = "nosniff"; return File(ruta, mime); }
            return File(ruta, mime, "Plan_" + id + ext);
        }
        [HttpGet] public ActionResult Descargar(int id) { return ArchivoPlan(id, false); }
        [HttpGet] public ActionResult VistaPrevia(int id) { return ArchivoPlan(id, true); }
    }
}
