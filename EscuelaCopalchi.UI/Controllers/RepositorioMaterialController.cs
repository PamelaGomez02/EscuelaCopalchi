using EscuelaCopalchi.UI.Models;
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
    public class RepositorioMaterialController : Controller
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;
        private static readonly string[] Extensiones = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private const int MaxBytes = 50 * 1024 * 1024;


        private int? UsuarioActual()
        {
            // Verificar que el usuario esté autenticado
            if (!Request.IsAuthenticated)
            {
                return null;
            }

            // Intentar recuperar el ID desde la sesión
            int idSesion = 0;

            if (Session["IdUsuario"] != null)
            {
                if (int.TryParse(
                    Session["IdUsuario"].ToString(),
                    out idSesion) && idSesion > 0)
                {
                    return idSesion;
                }
            }

            // Recuperar el correo del usuario autenticado
            string correo = User.Identity.Name;

            if (string.IsNullOrWhiteSpace(correo))
            {
                return null;
            }

            // Consultar el ID en SQL Server
            const string sql = @"
        SELECT id_usuario
        FROM dbo.USUARIO
        WHERE correo = @correo
          AND estado = 1";

            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@correo", SqlDbType.NVarChar, 255)
                    .Value = correo;

                cn.Open();

                object resultado = cmd.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    return null;
                }

                int idUsuario = Convert.ToInt32(resultado);

                // Guardar el ID en la sesión
                Session["IdUsuario"] = idUsuario;

                return idUsuario;
            }
        }


        private bool TienePermiso(string permiso)
        {
            int? usuario = UsuarioActual();
            if (!usuario.HasValue) return false;

            const string sql = @"SELECT COUNT(1)
                FROM dbo.USUARIO u
                INNER JOIN dbo.ROL r ON r.id_rol = u.id_rol
                INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol = r.id_rol
                INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                WHERE u.id_usuario = @usuario
                  AND u.estado = 1 AND r.estado = 1 AND p.estado = 1
                  AND p.nombre = @permiso";

            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = usuario.Value;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 150).Value = permiso;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private ActionResult Denegado()
        {
            return new HttpStatusCodeResult(403, "No tiene permisos para realizar esta acción.");
        }

        private bool PuedeEditar(RepositorioMaterialViewModel m)
        {
            int? id = UsuarioActual();
            return TienePermiso("EDITAR_MATERIAL") &&
                ((id.HasValue && m.IdUsuarioCreador == id) || TienePermiso("ADMINISTRAR_TODO_MATERIAL"));
        }

        private bool PuedeEliminar(RepositorioMaterialViewModel m)
        {
            int? id = UsuarioActual();
            return TienePermiso("ELIMINAR_MATERIAL") &&
                ((id.HasValue && m.IdUsuarioCreador == id) || TienePermiso("ADMINISTRAR_TODO_MATERIAL"));
        }

        private bool PuedeCargar(string tipo)
        {
            if (tipo == "Material didáctico") return TienePermiso("CARGAR_MATERIAL");
            if (tipo == "Documento institucional") return TienePermiso("CARGAR_DOCUMENTO_INSTITUCIONAL");
            return false;
        }

        private void PrepararPermisosFormulario()
        {
            ViewBag.PuedeCargarMaterial = TienePermiso("CARGAR_MATERIAL");
            ViewBag.PuedeCargarDocumentoInstitucional = TienePermiso("CARGAR_DOCUMENTO_INSTITUCIONAL");
        }

        private string Carpeta()
        {
            string path = Server.MapPath("~/App_Data/RepositorioMaterial");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string Extension(string nombre)
        {
            return Path.GetExtension(nombre ?? "").ToLowerInvariant();
        }

        private static string Mime(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".ppt": return "application/vnd.ms-powerpoint";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                default: return "application/octet-stream";
            }
        }

        private bool ValidarArchivo(HttpPostedFileBase archivo, bool obligatorio)
        {
            if (archivo == null || archivo.ContentLength <= 0)
            {
                if (obligatorio) ModelState.AddModelError("Archivo", "Seleccione un archivo.");
                return !obligatorio;
            }

            if (archivo.ContentLength > MaxBytes)
                ModelState.AddModelError("Archivo", "El archivo no puede superar 50 MB.");

            if (!Extensiones.Contains(Extension(archivo.FileName)))
                ModelState.AddModelError("Archivo", "Formato de archivo no permitido.");

            return ModelState.IsValid;
        }

        private void ValidarClasificacion(RepositorioMaterialViewModel m)
        {
            string[] categorias = { "Matemáticas", "Español", "Ciencias", "Estudios Sociales", "Inglés", "Institucional" };
            string[] grados = { "No aplica", "Primer grado", "Segundo grado", "Tercer grado", "Cuarto grado", "Quinto grado", "Sexto grado" };

            if (!categorias.Contains(m.Categoria))
                ModelState.AddModelError("Categoria", "Categoría inválida.");
            if (!grados.Contains(m.Grado))
                ModelState.AddModelError("Grado", "Grado inválido.");
            if (m.TipoMaterial == "Documento institucional" &&
                (m.Categoria != "Institucional" || m.Grado != "No aplica"))
                ModelState.AddModelError("Categoria", "Los documentos institucionales requieren categoría Institucional y grado No aplica.");
        }

        private static string CadenaNullable(SqlDataReader r, string campo)
        {
            return r[campo] == DBNull.Value ? null : Convert.ToString(r[campo]);
        }

        private static RepositorioMaterialViewModel Leer(SqlDataReader r)
        {
            return new RepositorioMaterialViewModel
            {
                IdMaterial = Convert.ToInt32(r["id_material"]),
                IdDocente = r["id_docente"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["id_docente"]),
                IdGrupo = r["id_grupo"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["id_grupo"]),
                Titulo = Convert.ToString(r["titulo"]),
                Descripcion = CadenaNullable(r, "descripcion"),
                Tipo = Convert.ToString(r["tipo"]),
                NombreArchivo = CadenaNullable(r, "archivo"),
                Url = CadenaNullable(r, "url"),
                TipoMaterial = CadenaNullable(r, "tipo_material"),
                Categoria = CadenaNullable(r, "categoria"),
                Grado = CadenaNullable(r, "grado"),
                IdUsuarioCreador = r["id_usuario_carga"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["id_usuario_carga"]),
                FechaCarga = Convert.ToDateTime(r["fecha_carga"]),
                Estado = Convert.ToBoolean(r["estado"])
            };
        }

        private RepositorioMaterialViewModel Obtener(int id)
        {
            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand("SELECT * FROM dbo.MATERIAL_DIDACTICO WHERE id_material = @id AND estado = 1", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    return r.Read() ? Leer(r) : null;
            }
        }

        private void PrepararPermisos(RepositorioMaterialViewModel m)
        {
            m.PuedeEditar = PuedeEditar(m);
            m.PuedeEliminar = PuedeEliminar(m);
            m.PuedeDescargar = TienePermiso("DESCARGAR_MATERIAL");
            m.PuedeModificar = m.PuedeEditar || m.PuedeEliminar;
        }

        [HttpGet]
        public ActionResult Index(string busqueda, string categoria, string tipo, string grado)
        {
            if (!TienePermiso("VER_MATERIAL")) return Denegado();

            var lista = new List<RepositorioMaterialViewModel>();
            const string sql = @"SELECT * FROM dbo.MATERIAL_DIDACTICO
                WHERE estado = 1
                  AND (@b = '' OR titulo LIKE '%' + @b + '%' OR descripcion LIKE '%' + @b + '%')
                  AND (@c = '' OR categoria = @c)
                  AND (@t = '' OR tipo_material = @t OR tipo = @t)
                  AND (@g = '' OR grado = @g)
                ORDER BY fecha_carga DESC";

            string tipoFiltro = tipo ?? "";
            if (tipoFiltro.StartsWith(".")) tipoFiltro = tipoFiltro.Substring(1).ToUpperInvariant();

            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@b", SqlDbType.NVarChar, 200).Value = busqueda ?? "";
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 100).Value = categoria ?? "";
                cmd.Parameters.Add("@t", SqlDbType.NVarChar, 50).Value = tipoFiltro;
                cmd.Parameters.Add("@g", SqlDbType.NVarChar, 50).Value = grado ?? "";
                cn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) lista.Add(Leer(r));
            }

            foreach (var m in lista) PrepararPermisos(m);
            ViewBag.Busqueda = busqueda;
            ViewBag.Categoria = categoria;
            ViewBag.Tipo = tipo;
            ViewBag.Grado = grado;
            PrepararPermisosFormulario();
            return View(lista);
        }

        [HttpGet]
        public ActionResult Crear()
        {
            if (!TienePermiso("CARGAR_MATERIAL") && !TienePermiso("CARGAR_DOCUMENTO_INSTITUCIONAL"))
                return Denegado();
            PrepararPermisosFormulario();
            return View(new RepositorioMaterialViewModel { Grado = "No aplica" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(RepositorioMaterialViewModel m)
        {
            if (!PuedeCargar(m.TipoMaterial)) return Denegado();
            ValidarClasificacion(m);
            ValidarArchivo(m.Archivo, true);

            if (!ModelState.IsValid)
            {
                PrepararPermisosFormulario();
                return View(m);
            }

            string ext = Extension(m.Archivo.FileName);
            string almacenado = Guid.NewGuid().ToString("N") + ext;
            string ruta = Path.Combine(Carpeta(), almacenado);
            m.Archivo.SaveAs(ruta);

            try
            {
                const string sql = @"INSERT INTO dbo.MATERIAL_DIDACTICO
                    (id_docente, id_grupo, titulo, tipo, archivo, url, descripcion, fecha_carga,
                     estado, id_usuario_carga, categoria, grado, tipo_material)
                    VALUES (NULL, NULL, @titulo, @tipo, @archivo, NULL, @descripcion,
                            SYSDATETIME(), 1, @usuario, @categoria, @grado, @tipoMaterial)";

                using (var cn = new SqlConnection(cadena))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@titulo", SqlDbType.NVarChar, 200).Value = m.Titulo.Trim();
                    cmd.Parameters.Add("@tipo", SqlDbType.NVarChar, 20).Value = ext.TrimStart('.').ToUpperInvariant();
                    cmd.Parameters.Add("@archivo", SqlDbType.NVarChar, 500).Value = almacenado;
                    cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, 1000).Value = (object)m.Descripcion ?? DBNull.Value;
                    cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = UsuarioActual().Value;
                    cmd.Parameters.Add("@categoria", SqlDbType.NVarChar, 100).Value = m.Categoria;
                    cmd.Parameters.Add("@grado", SqlDbType.NVarChar, 50).Value = m.Grado;
                    cmd.Parameters.Add("@tipoMaterial", SqlDbType.NVarChar, 50).Value = m.TipoMaterial;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
                throw;
            }

            TempData["Success"] = "Material cargado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            if (!TienePermiso("VER_MATERIAL"))
                return Denegado();

            var material = Obtener(id);

            if (material == null)
                return HttpNotFound();

            PrepararPermisos(material);

            material.NombreUsuarioCreador =
                ObtenerNombreUsuario(material.IdUsuarioCreador);

            return View(material);
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {
            var m = Obtener(id);
            if (m == null) return HttpNotFound();
            if (!PuedeEditar(m)) return Denegado();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(RepositorioMaterialViewModel m)
        {
            var original = Obtener(m.IdMaterial);
            if (original == null) return HttpNotFound();
            if (!PuedeEditar(original)) return Denegado();

            m.TipoMaterial = original.TipoMaterial;
            ModelState.Remove("TipoMaterial");
            ValidarClasificacion(m);
            ValidarArchivo(m.Archivo, false);

            if (!ModelState.IsValid)
            {
                m.NombreArchivo = original.NombreArchivo;
                return View(m);
            }

            bool reemplazar = m.Archivo != null && m.Archivo.ContentLength > 0;
            string nuevo = reemplazar ? Guid.NewGuid().ToString("N") + Extension(m.Archivo.FileName) : null;
            string rutaNueva = reemplazar ? Path.Combine(Carpeta(), nuevo) : null;
            if (reemplazar) m.Archivo.SaveAs(rutaNueva);

            try
            {
                const string sql = @"UPDATE dbo.MATERIAL_DIDACTICO
                    SET titulo = @titulo, descripcion = @descripcion,
                        categoria = @categoria, grado = @grado,
                        archivo = CASE WHEN @archivo IS NULL THEN archivo ELSE @archivo END,
                        tipo = CASE WHEN @tipo IS NULL THEN tipo ELSE @tipo END
                    WHERE id_material = @id AND estado = 1";

                using (var cn = new SqlConnection(cadena))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@titulo", SqlDbType.NVarChar, 200).Value = m.Titulo.Trim();
                    cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, 1000).Value = (object)m.Descripcion ?? DBNull.Value;
                    cmd.Parameters.Add("@categoria", SqlDbType.NVarChar, 100).Value = m.Categoria;
                    cmd.Parameters.Add("@grado", SqlDbType.NVarChar, 50).Value = m.Grado;
                    cmd.Parameters.Add("@archivo", SqlDbType.NVarChar, 500).Value = (object)nuevo ?? DBNull.Value;
                    cmd.Parameters.Add("@tipo", SqlDbType.NVarChar, 20).Value = reemplazar ? (object)Extension(nuevo).TrimStart('.').ToUpperInvariant() : DBNull.Value;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = m.IdMaterial;
                    cn.Open();
                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        if (rutaNueva != null && System.IO.File.Exists(rutaNueva)) System.IO.File.Delete(rutaNueva);
                        return HttpNotFound();
                    }
                }
            }
            catch
            {
                if (rutaNueva != null && System.IO.File.Exists(rutaNueva)) System.IO.File.Delete(rutaNueva);
                throw;
            }

            if (reemplazar && !string.IsNullOrEmpty(original.NombreArchivo))
            {
                string viejo = Path.Combine(Carpeta(), Path.GetFileName(original.NombreArchivo));
                if (System.IO.File.Exists(viejo)) System.IO.File.Delete(viejo);
            }

            TempData["Success"] = "Material actualizado correctamente.";
            return RedirectToAction("Detalle", new { id = m.IdMaterial });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            var m = Obtener(id);
            if (m == null) return HttpNotFound();
            if (!PuedeEliminar(m)) return Denegado();

            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand("UPDATE dbo.MATERIAL_DIDACTICO SET estado = 0 WHERE id_material = @id AND estado = 1", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Material eliminado del repositorio.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Descargar(int id)
        {
            if (!TienePermiso("DESCARGAR_MATERIAL")) return Denegado();
            var m = Obtener(id);
            if (m == null || string.IsNullOrWhiteSpace(m.NombreArchivo)) return HttpNotFound();
            string ruta = Path.Combine(Carpeta(), Path.GetFileName(m.NombreArchivo));
            if (!System.IO.File.Exists(ruta)) return HttpNotFound();
            string ext = Extension(m.NombreArchivo);
            return File(ruta, Mime(ext), "Material_" + id + ext);
        }

        [HttpGet]
        public ActionResult VistaPrevia(int id)
        {
            if (!TienePermiso("VER_MATERIAL"))
                return Denegado();

            var material = Obtener(id);

            if (material == null ||
                string.IsNullOrWhiteSpace(material.NombreArchivo))
            {
                return HttpNotFound();
            }

            string extension = Extension(material.NombreArchivo);

            string[] permitidas = { ".jpg", ".jpeg", ".png", ".pdf" };

            if (!permitidas.Contains(extension))
            {
                return new HttpStatusCodeResult(
                    415,
                    "Vista previa no disponible para este formato."
                );
            }

            string ruta = Path.Combine(
                Carpeta(),
                Path.GetFileName(material.NombreArchivo)
            );

            if (!System.IO.File.Exists(ruta))
                return HttpNotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Mantener el archivo dentro del navegador.
            Response.Headers["Content-Disposition"] = "inline";

            return File(ruta, Mime(extension));
        }


        private string ObtenerNombreUsuario(int? idUsuario)
        {
            if (!idUsuario.HasValue)
            {
                return "No registrado";
            }

            const string sql = @"
        SELECT
            LTRIM(RTRIM(
                CONCAT(
                    nombre, ' ',
                    apellido1, ' ',
                    apellido2
                )
            ))
        FROM dbo.USUARIO
        WHERE id_usuario = @idUsuario";

            using (var cn = new SqlConnection(cadena))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@idUsuario", SqlDbType.Int)
                    .Value = idUsuario.Value;

                cn.Open();

                object resultado = cmd.ExecuteScalar();

                if (resultado == null || resultado == DBNull.Value)
                {
                    return "Usuario no encontrado";
                }

                string nombreCompleto = Convert.ToString(resultado);

                return string.IsNullOrWhiteSpace(nombreCompleto)
                    ? "Nombre no registrado"
                    : nombreCompleto;
            }
        }

    }
}
