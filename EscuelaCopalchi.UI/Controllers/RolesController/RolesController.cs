
using EscuelaCopalchi.UI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class RolesController : Controller
    {
        private const string Ok = "Se ha guardado correctamente";

        private readonly ConexionBD db;

        public RolesController()
        {
            db = new ConexionBD();
        }

        // ================================================
        // MÉTODOS AUXILIARES
        // ================================================

        private static string Txt(DataRow fila, string columna)
        {
            if (fila[columna] == DBNull.Value)
                return "";

            return fila[columna].ToString();
        }

        private static Rol MapRol(DataRow fila)
        {
            return new Rol
            {
                IdRol = Convert.ToInt32(fila["id_rol"]),
                Nombre = Txt(fila, "nombre"),
                Descripcion = Txt(fila, "descripcion"),
                Estado = Convert.ToBoolean(fila["estado"])
            };
        }

        private Rol ObtenerRol(int id)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_ObtenerPorId",
                new[] { "@id_rol" },
                new[] { id.ToString() }
            );

            if (dt == null || dt.Rows.Count == 0)
                return null;

            return MapRol(dt.Rows[0]);
        }

        private static string Json(IEnumerable<int> ids)
        {
            return "[" + string.Join(",",
                ids.Distinct().OrderBy(x => x)) + "]";
        }

        private static bool EsRolUnico(string nombre)
        {
            return string.Equals(
                nombre,
                "Director",
                StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    nombre,
                    "Administrador",
                    StringComparison.OrdinalIgnoreCase);
        }

        // ================================================
        // LISTAR ROLES
        // ================================================

        [HttpGet]
        public ActionResult Index()
        {
            List<Rol> roles = new List<Rol>();

            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_Listar",
                null,
                null
            );

            if (dt != null)
            {
                foreach (DataRow fila in dt.Rows)
                {
                    roles.Add(MapRol(fila));
                }
            }

            return View(roles);
        }

        // ================================================
        // CREAR ROL - GET
        // ================================================

        [HttpGet]
        public ActionResult Crear()
        {
            return View();
        }

        // ================================================
        // CREAR ROL - POST
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Rol modelo)
        {
            if (modelo == null)
                return new HttpStatusCodeResult(400);

            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                ModelState.AddModelError(
                    "Nombre",
                    "El nombre del rol es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
            {
                ModelState.AddModelError(
                    "Descripcion",
                    "La descripción es obligatoria.");
            }

            if (!ModelState.IsValid)
                return View(modelo);

            string resultado = db.ejecutarProcedimiento_Save(
                "SP_Rol_Insertar",
                new[]
                {
                    "@nombre",
                    "@descripcion",
                    "@estado"
                },
                new[]
                {
                    modelo.Nombre.Trim(),
                    modelo.Descripcion.Trim(),
                    modelo.Estado ? "1" : "0"
                }
            );

            if (resultado != Ok)
            {
                ModelState.AddModelError(
                    "Nombre",
                    resultado);

                return View(modelo);
            }

            TempData["Exito"] =
                "El rol se registró correctamente.";

            return RedirectToAction("Index");
        }

        // ================================================
        // EDITAR ROL - GET
        // ================================================

        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un rol.";

                return RedirectToAction("Index");
            }

            Rol modelo = ObtenerRol(id.Value);

            if (modelo == null)
                return HttpNotFound();

            return View(modelo);
        }

        // ================================================
        // EDITAR ROL - POST
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Rol modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
                return new HttpStatusCodeResult(400);

            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                ModelState.AddModelError(
                    "Nombre",
                    "El nombre del rol es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
            {
                ModelState.AddModelError(
                    "Descripcion",
                    "La descripción es obligatoria.");
            }

            if (!ModelState.IsValid)
                return View(modelo);

            string resultado = db.ejecutarProcedimiento_Save(
                "SP_Rol_Actualizar",
                new[]
                {
                    "@id_rol",
                    "@nombre",
                    "@descripcion",
                    "@estado"
                },
                new[]
                {
                    modelo.IdRol.ToString(),
                    modelo.Nombre.Trim(),
                    modelo.Descripcion.Trim(),
                    modelo.Estado ? "1" : "0"
                }
            );

            if (resultado != Ok)
            {
                ModelState.AddModelError(
                    "Nombre",
                    resultado);

                return View(modelo);
            }

            TempData["Exito"] =
                "El rol se actualizó correctamente.";

            return RedirectToAction("Index");
        }

        // ================================================
        // ELIMINAR ROL
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(400);

            string resultado = db.ejecutarProcedimiento_Save(
                "SP_Rol_Eliminar",
                new[] { "@id_rol" },
                new[] { id.ToString() }
            );

            if (resultado != Ok)
            {
                TempData["Error"] = resultado;

                return RedirectToAction(
                    "Editar",
                    new { id = id });
            }

            TempData["Exito"] =
                "El rol se eliminó correctamente.";

            return RedirectToAction("Index");
        }

        // ================================================
        // ASIGNAR ROL - GET
        // ================================================

        [HttpGet]
        public ActionResult Asignar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un rol.";

                return RedirectToAction("Index");
            }

            Rol rol = ObtenerRol(id.Value);

            if (rol == null)
                return HttpNotFound();

            if (!rol.Estado)
            {
                TempData["Error"] =
                    "No se pueden asignar roles inactivos.";

                return RedirectToAction("Index");
            }

            AsignarRolViewModel modelo =
                new AsignarRolViewModel
                {
                    IdRol = rol.IdRol,
                    NombreRol = rol.Nombre,
                    DescripcionRol = rol.Descripcion
                };

            if (modelo.Usuarios == null)
            {
                modelo.Usuarios =
                    new List<UsuarioAsignableViewModel>();
            }

            // Este procedimiento debe consultar USUARIO,
            // no depender de la tabla DOCENTE.
            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_UsuariosAsignables",
                new[] { "@id_rol" },
                new[] { id.Value.ToString() }
            );

            if (dt != null)
            {
                foreach (DataRow fila in dt.Rows)
                {
                    modelo.Usuarios.Add(
                        new UsuarioAsignableViewModel
                        {
                            IdUsuario = Convert.ToInt32(
                                fila["id_usuario"]),

                            NombreCompleto = (
                                Txt(fila, "nombre") + " " +
                                Txt(fila, "apellido1") + " " +
                                Txt(fila, "apellido2")
                            ).Trim(),

                            Correo = Txt(fila, "correo"),

                            Seleccionado = Convert.ToBoolean(
                                fila["seleccionado"])
                        });
                }
            }

            return View(modelo);
        }

        // ================================================
        // ASIGNAR ROL - POST
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Asignar(AsignarRolViewModel modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
            {
                TempData["Error"] =
                    "No fue posible identificar el rol seleccionado.";

                return RedirectToAction("Index");
            }

            try
            {
                Rol rol = ObtenerRol(modelo.IdRol);

                if (rol == null)
                {
                    TempData["Error"] =
                        "El rol seleccionado no existe.";

                    return RedirectToAction("Index");
                }

                if (!rol.Estado)
                {
                    TempData["Error"] =
                        "No se pueden asignar roles inactivos.";

                    return RedirectToAction("Index");
                }

                var seleccionados = (
                    modelo.Usuarios ??
                   new List<UsuarioAsignableViewModel>())
                    .Where(u => u.Seleccionado)
                    .Select(u => u.IdUsuario)
                    .Distinct()
                    .ToList();

                // No permitir IDs inválidos.
                if (seleccionados.Any(id => id <= 0))
                {
                    TempData["Error"] =
                        "Se seleccionaron usuarios inválidos.";

                    return RedirectToAction(
                        "Asignar",
                        new { id = modelo.IdRol });
                }

                // Verificar que los IDs enviados pertenezcan
                // a usuarios activos que devuelve SQL.
                DataTable disponibles = db.ejecutarProcedimiento(
                    "SP_Rol_UsuariosAsignables",
                    new[] { "@id_rol" },
                    new[] { modelo.IdRol.ToString() }
                );

                var idsPermitidos = new HashSet<int>();

                if (disponibles != null)
                {
                    foreach (DataRow fila in disponibles.Rows)
                    {
                        idsPermitidos.Add(
                            Convert.ToInt32(fila["id_usuario"]));
                    }
                }

                if (seleccionados.Any(id =>
                    !idsPermitidos.Contains(id)))
                {
                    TempData["Error"] =
                        "Uno o varios usuarios no están disponibles.";

                    return RedirectToAction(
                        "Asignar",
                        new { id = modelo.IdRol });
                }

                if (seleccionados.Count == 0)
                {
                    TempData["Error"] =
                        "Debe seleccionar al menos un usuario.";

                    return RedirectToAction(
                        "Asignar",
                        new { id = modelo.IdRol });
                }

                // Administrador y Director: máximo uno.
                if (EsRolUnico(rol.Nombre) &&
                    seleccionados.Count > 1)
                {
                    TempData["Error"] =
                        "El rol " + rol.Nombre +
                        " solo puede asignarse a un usuario activo.";

                    return RedirectToAction(
                        "Asignar",
                        new { id = modelo.IdRol });
                }

                // Convertir IDs a JSON: [1,2,3].
                string idsJson = Json(seleccionados);

                // Guardar en USUARIO mediante SQL Server.
                string resultado = db.ejecutarProcedimiento_Save(
                    "SP_Rol_AsignarUsuarios",
                    new[]
                    {
                        "@id_rol",
                        "@ids_json"
                    },
                    new[]
                    {
                        modelo.IdRol.ToString(),
                        idsJson
                    }
                );

                if (resultado != Ok)
                {
                    TempData["Error"] = resultado;

                    return RedirectToAction(
                        "Asignar",
                        new { id = modelo.IdRol });
                }

                TempData["Exito"] =
                    "El rol " + rol.Nombre +
                    " se asignó correctamente.";

                // Regresar a Asignar para ver los cambios.
                return RedirectToAction(
                    "Asignar",
                    new { id = modelo.IdRol });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error al guardar la asignación: " +
                    ex.Message;

                return RedirectToAction(
                    "Asignar",
                    new { id = modelo.IdRol });
            }
        }

        // ================================================
        // PERMISOS - GET
        // ================================================

        [HttpGet]
        public ActionResult Permisos(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un rol.";

                return RedirectToAction("Index");
            }

            Rol rol = ObtenerRol(id.Value);

            if (rol == null)
                return HttpNotFound();

            RolPermisosViewModel modelo =
                new RolPermisosViewModel
                {
                    IdRol = rol.IdRol,
                    NombreRol = rol.Nombre,
                    DescripcionRol = rol.Descripcion
                };

            if (modelo.Permisos == null)
            {
                modelo.Permisos =
                    new List<PermisoViewModel>();
            }

            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_Permisos_Listar",
                new[] { "@id_rol" },
                new[] { id.Value.ToString() }
            );

            if (dt != null)
            {
                foreach (DataRow fila in dt.Rows)
                {
                    modelo.Permisos.Add(
                        new PermisoViewModel
                        {
                            IdPermiso = Convert.ToInt32(
                                fila["id_permiso"]),

                            Nombre = Txt(fila, "nombre"),

                            Descripcion = Txt(
                                fila, "descripcion"),

                            Modulo = Txt(fila, "modulo"),

                            Seleccionado = Convert.ToBoolean(
                                fila["seleccionado"])
                        });
                }
            }

            return View(modelo);
        }

        // ================================================
        // PERMISOS - POST
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Permisos(RolPermisosViewModel modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
            {
                TempData["Error"] =
                    "No fue posible identificar el rol.";

                return RedirectToAction("Index");
            }

            try
            {
                var seleccionados = (
                    modelo.Permisos ??
                    new List<PermisoViewModel>())
                    .Where(p => p.Seleccionado)
                    .Select(p => p.IdPermiso)
                    .Distinct()
                    .ToList();

                string resultado = db.ejecutarProcedimiento_Save(
                    "SP_Rol_Permisos_Guardar",
                    new[]
                    {
                        "@id_rol",
                        "@ids_json"
                    },
                    new[]
                    {
                        modelo.IdRol.ToString(),
                        Json(seleccionados)
                    }
                );

                if (resultado != Ok)
                {
                    TempData["Error"] = resultado;

                    return RedirectToAction(
                        "Permisos",
                        new { id = modelo.IdRol });
                }

                TempData["Exito"] =
                    "Los permisos del rol se actualizaron correctamente.";

                return RedirectToAction(
                    "Permisos",
                    new { id = modelo.IdRol });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Error al guardar los permisos: " +
                    ex.Message;

                return RedirectToAction(
                    "Permisos",
                    new { id = modelo.IdRol });
            }
        }
    }
}
