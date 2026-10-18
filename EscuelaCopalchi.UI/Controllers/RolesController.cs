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

        // ---------- helpers ----------
        private static string Txt(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? "" : r[col].ToString();
        }

        private static Rol MapRol(DataRow r)
        {
            return new Rol
            {
                IdRol = Convert.ToInt32(r["id_rol"]),
                Nombre = Txt(r, "nombre"),
                Descripcion = Txt(r, "descripcion"),
                Estado = Convert.ToBoolean(r["estado"])
            };
        }

        private Rol ObtenerRol(int id)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_ObtenerPorId", new[] { "@id_rol" }, new[] { id.ToString() });

            return (dt != null && dt.Rows.Count > 0) ? MapRol(dt.Rows[0]) : null;
        }

        private static string Json(IEnumerable<int> ids)
        {
            return "[" + string.Join(",", ids) + "]";
        }

        // =========================================================
        // LISTAR ROLES
        // =========================================================
        public ActionResult Index()
        {
            List<Rol> roles = new List<Rol>();

            DataTable dt = db.ejecutarProcedimiento("SP_Rol_Listar", null, null);
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows) roles.Add(MapRol(r));
            }

            return View(roles);
        }

        // =========================================================
        // CREAR ROL
        // =========================================================
        [HttpGet]
        public ActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Rol modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio.");

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");

            if (!ModelState.IsValid) return View(modelo);

            string res = db.ejecutarProcedimiento_Save(
                "SP_Rol_Insertar",
                new[] { "@nombre", "@descripcion", "@estado" },
                new[] { modelo.Nombre.Trim(), modelo.Descripcion.Trim(), modelo.Estado ? "1" : "0" });

            if (res != Ok)
            {
                ModelState.AddModelError("Nombre", res);
                return View(modelo);
            }

            TempData["Exito"] = "El rol se registró correctamente.";
            return RedirectToAction("Index");
        }

        // =========================================================
        // EDITAR ROL
        // =========================================================
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            Rol modelo = ObtenerRol(id.Value);
            if (modelo == null) return HttpNotFound();

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Rol modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio.");

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");

            if (!ModelState.IsValid) return View(modelo);

            string res = db.ejecutarProcedimiento_Save(
                "SP_Rol_Actualizar",
                new[] { "@id_rol", "@nombre", "@descripcion", "@estado" },
                new[] { modelo.IdRol.ToString(), modelo.Nombre.Trim(), modelo.Descripcion.Trim(), modelo.Estado ? "1" : "0" });

            if (res != Ok)
            {
                ModelState.AddModelError("Nombre", res);
                return View(modelo);
            }

            TempData["Exito"] = "El rol se actualizó correctamente.";
            return RedirectToAction("Index");
        }

        // =========================================================
        // ELIMINAR ROL
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            string res = db.ejecutarProcedimiento_Save(
                "SP_Rol_Eliminar", new[] { "@id_rol" }, new[] { id.ToString() });

            if (res != Ok)
            {
                TempData["Error"] = res;
                return RedirectToAction("Editar", new { id });
            }

            TempData["Exito"] = "El rol se eliminó correctamente.";
            return RedirectToAction("Index");
        }

        // =========================================================
        // ASIGNAR ROL - MOSTRAR
        // =========================================================
        [HttpGet]
        public ActionResult Asignar(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            Rol rol = ObtenerRol(id.Value);
            if (rol == null) return HttpNotFound();

            AsignarRolViewModel modelo = new AsignarRolViewModel
            {
                IdRol = rol.IdRol,
                NombreRol = rol.Nombre,
                DescripcionRol = rol.Descripcion
            };

            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_UsuariosAsignables", new[] { "@id_rol" }, new[] { id.Value.ToString() });

            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    modelo.Usuarios.Add(new UsuarioAsignacionViewModel
                    {
                        IdUsuario = Convert.ToInt32(r["id_usuario"]),
                        NombreCompleto = (Txt(r, "nombre") + " " + Txt(r, "apellido1") + " " + Txt(r, "apellido2")).Trim(),
                        Correo = Txt(r, "correo"),
                        Seleccionado = Convert.ToInt32(r["seleccionado"]) == 1
                    });
                }
            }

            return View(modelo);
        }

        // =========================================================
        // ASIGNAR ROL - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Asignar(AsignarRolViewModel modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
            {
                TempData["Error"] = "No fue posible identificar el rol seleccionado.";
                return RedirectToAction("Index");
            }

            var seleccionados = (modelo.Usuarios ?? new List<UsuarioAsignacionViewModel>())
                .Where(u => u.Seleccionado)
                .Select(u => u.IdUsuario);

            string res = db.ejecutarProcedimiento_Save(
                "SP_Rol_AsignarUsuarios",
                new[] { "@id_rol", "@ids_json" },
                new[] { modelo.IdRol.ToString(), Json(seleccionados) });

            if (res != Ok)
            {
                TempData["Error"] = res;
                return RedirectToAction("Asignar", new { id = modelo.IdRol });
            }

            TempData["Exito"] = "El rol se asignó correctamente.";
            return RedirectToAction("Index");
        }

        // =========================================================
        // PERMISOS - MOSTRAR
        // =========================================================
        [HttpGet]
        public ActionResult Permisos(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            Rol rol = ObtenerRol(id.Value);
            if (rol == null) return HttpNotFound();

            RolPermisosViewModel modelo = new RolPermisosViewModel
            {
                IdRol = rol.IdRol,
                NombreRol = rol.Nombre,
                DescripcionRol = rol.Descripcion
            };

            DataTable dt = db.ejecutarProcedimiento(
                "SP_Rol_Permisos_Listar", new[] { "@id_rol" }, new[] { id.Value.ToString() });

            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    modelo.Permisos.Add(new PermisoViewModel
                    {
                        IdPermiso = Convert.ToInt32(r["id_permiso"]),
                        Nombre = Txt(r, "nombre"),
                        Descripcion = Txt(r, "descripcion"),
                        Modulo = Txt(r, "modulo"),
                        Seleccionado = Convert.ToInt32(r["seleccionado"]) == 1
                    });
                }
            }

            return View(modelo);
        }

        // =========================================================
        // PERMISOS - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Permisos(RolPermisosViewModel modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
            {
                TempData["Error"] = "No fue posible identificar el rol.";
                return RedirectToAction("Index");
            }

            var seleccionados = (modelo.Permisos ?? new List<PermisoViewModel>())
                .Where(p => p.Seleccionado)
                .Select(p => p.IdPermiso);

            string res = db.ejecutarProcedimiento_Save(
                "SP_Rol_Permisos_Guardar",
                new[] { "@id_rol", "@ids_json" },
                new[] { modelo.IdRol.ToString(), Json(seleccionados) });

            if (res != Ok)
            {
                TempData["Error"] = "Ocurrió un error al guardar los permisos.";
                return RedirectToAction("Permisos", new { id = modelo.IdRol });
            }

            TempData["Exito"] = "Los permisos del rol se actualizaron correctamente.";
            return RedirectToAction("Index");
        }
    }
}
