using EscuelaCopalchi.UI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class RolesController : Controller
    {
        private readonly string conexion;

        public RolesController()
        {
            ConexionBD db = new ConexionBD();
            conexion = db.ObtenerConexion();
        }

        // =========================================================
        // LISTAR ROLES
        // =========================================================
        public ActionResult Index()
        {
            List<Rol> roles = new List<Rol>();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string sql = @"SELECT id_rol, nombre, descripcion, estado
                               FROM ROL
                               ORDER BY nombre";

                SqlCommand cmd = new SqlCommand(sql, con);

                con.Open();

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Rol rol = new Rol
                    {
                        IdRol = Convert.ToInt32(reader["id_rol"]),
                        Nombre = reader["nombre"].ToString(),

                        Descripcion = reader["descripcion"] == DBNull.Value
                            ? ""
                            : reader["descripcion"].ToString(),

                        Estado = Convert.ToBoolean(reader["estado"])
                    };

                    roles.Add(rol);
                }
            }

            return View(roles);
        }


        // =========================================================
        // CREAR ROL - MOSTRAR
        // =========================================================
        [HttpGet]
        public ActionResult Crear()
        {
            return View();
        }


        // =========================================================
        // CREAR ROL - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Rol modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                ModelState.AddModelError(
                    "Nombre",
                    "El nombre del rol es obligatorio."
                );
            }

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
            {
                ModelState.AddModelError(
                    "Descripcion",
                    "La descripción es obligatoria."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                // Verificar si ya existe el rol
                string sqlExiste = @"
                    SELECT COUNT(*)
                    FROM ROL
                    WHERE LOWER(nombre) = LOWER(@nombre)";

                using (SqlCommand cmdExiste =
                       new SqlCommand(sqlExiste, con))
                {
                    cmdExiste.Parameters.AddWithValue(
                        "@nombre",
                        modelo.Nombre.Trim()
                    );

                    int cantidad =
                        Convert.ToInt32(cmdExiste.ExecuteScalar());

                    if (cantidad > 0)
                    {
                        ModelState.AddModelError(
                            "Nombre",
                            "Ya existe un rol con este nombre."
                        );

                        return View(modelo);
                    }
                }

                // Insertar nuevo rol
                string sqlInsertar = @"
                    INSERT INTO ROL
                        (nombre, descripcion, estado)
                    VALUES
                        (@nombre, @descripcion, @estado)";

                using (SqlCommand cmd =
                       new SqlCommand(sqlInsertar, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@nombre",
                        modelo.Nombre.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@descripcion",
                        modelo.Descripcion.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@estado",
                        modelo.Estado
                    );

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] =
                "El rol se registró correctamente.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // EDITAR ROL - MOSTRAR
        // =========================================================
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            Rol modelo = null;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string sql = @"
                    SELECT id_rol, nombre, descripcion, estado
                    FROM ROL
                    WHERE id_rol = @id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            modelo = new Rol
                            {
                                IdRol =
                                    Convert.ToInt32(reader["id_rol"]),

                                Nombre =
                                    reader["nombre"].ToString(),

                                Descripcion =
                                    reader["descripcion"] == DBNull.Value
                                        ? ""
                                        : reader["descripcion"].ToString(),

                                Estado =
                                    Convert.ToBoolean(reader["estado"])
                            };
                        }
                    }
                }
            }

            if (modelo == null)
            {
                return HttpNotFound();
            }

            return View(modelo);
        }


        // =========================================================
        // EDITAR ROL - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Rol modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
            {
                ModelState.AddModelError(
                    "Nombre",
                    "El nombre del rol es obligatorio."
                );
            }

            if (string.IsNullOrWhiteSpace(modelo.Descripcion))
            {
                ModelState.AddModelError(
                    "Descripcion",
                    "La descripción es obligatoria."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                // Verificar que no exista otro rol con ese nombre
                string sqlExiste = @"
                    SELECT COUNT(*)
                    FROM ROL
                    WHERE LOWER(nombre) = LOWER(@nombre)
                    AND id_rol <> @id";

                using (SqlCommand cmdExiste =
                       new SqlCommand(sqlExiste, con))
                {
                    cmdExiste.Parameters.AddWithValue(
                        "@nombre",
                        modelo.Nombre.Trim()
                    );

                    cmdExiste.Parameters.AddWithValue(
                        "@id",
                        modelo.IdRol
                    );

                    int cantidad =
                        Convert.ToInt32(cmdExiste.ExecuteScalar());

                    if (cantidad > 0)
                    {
                        ModelState.AddModelError(
                            "Nombre",
                            "Ya existe otro rol con este nombre."
                        );

                        return View(modelo);
                    }
                }

                string sqlActualizar = @"
                    UPDATE ROL
                    SET nombre = @nombre,
                        descripcion = @descripcion,
                        estado = @estado
                    WHERE id_rol = @id";

                using (SqlCommand cmd =
                       new SqlCommand(sqlActualizar, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@nombre",
                        modelo.Nombre.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@descripcion",
                        modelo.Descripcion.Trim()
                    );

                    cmd.Parameters.AddWithValue(
                        "@estado",
                        modelo.Estado
                    );

                    cmd.Parameters.AddWithValue(
                        "@id",
                        modelo.IdRol
                    );

                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] =
                "El rol se actualizó correctamente.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // ELIMINAR ROL
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            string[] rolesDelSistema =
            {
                "Administrador",
                "Director",
                "Docente",
                "Apoyo",
                "Evaluación"
            };

            try
            {
                using (SqlConnection con =
                       new SqlConnection(conexion))
                {
                    con.Open();

                    string nombre;

                    using (SqlCommand cmd =
                           new SqlCommand(
                               "SELECT nombre FROM ROL WHERE id_rol = @id",
                               con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        nombre = cmd.ExecuteScalar() as string;
                    }

                    if (nombre == null)
                    {
                        TempData["Error"] =
                            "El rol no existe.";

                        return RedirectToAction("Index");
                    }

                    // No permitir eliminar roles base
                    if (rolesDelSistema.Contains(nombre))
                    {
                        TempData["Error"] =
                            "El rol " + nombre +
                            " es un rol base del sistema y no se puede eliminar. Puede desactivarlo.";

                        return RedirectToAction(
                            "Editar",
                            new { id }
                        );
                    }

                    // Verificar usuarios asociados
                    using (SqlCommand cmd =
                           new SqlCommand(
                               @"SELECT COUNT(*)
                                 FROM USUARIO
                                 WHERE id_rol = @id",
                               con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        int usuarios =
                            Convert.ToInt32(
                                cmd.ExecuteScalar()
                            );

                        if (usuarios > 0)
                        {
                            TempData["Error"] =
                                "No se puede eliminar el rol porque tiene usuarios asociados (activos o inactivos). Debe cambiarles el rol primero.";

                            return RedirectToAction(
                                "Editar",
                                new { id }
                            );
                        }
                    }

                    // Eliminar dentro de una transacción
                    using (SqlTransaction tx =
                           con.BeginTransaction())
                    {
                        try
                        {
                            // Primero eliminar permisos
                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       @"DELETE FROM ROL_PERMISO
                                         WHERE id_rol = @id",
                                       con,
                                       tx))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@id",
                                    id
                                );

                                cmd.ExecuteNonQuery();
                            }

                            // Luego eliminar rol
                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       @"DELETE FROM ROL
                                         WHERE id_rol = @id",
                                       con,
                                       tx))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@id",
                                    id
                                );

                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                TempData["Error"] =
                    "No se pudo eliminar el rol: " +
                    ex.Message;

                return RedirectToAction("Index");
            }

            TempData["Exito"] =
                "El rol se eliminó correctamente.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // ASIGNAR ROL - MOSTRAR DOCENTES
        // =========================================================
        [HttpGet]
        public ActionResult Asignar(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            AsignarRolViewModel modelo =
                new AsignarRolViewModel();

            using (SqlConnection con =
                   new SqlConnection(conexion))
            {
                con.Open();

                // Obtener información del rol
                string sqlRol = @"
                    SELECT id_rol, nombre, descripcion
                    FROM ROL
                    WHERE id_rol = @id";

                using (SqlCommand cmdRol =
                       new SqlCommand(sqlRol, con))
                {
                    cmdRol.Parameters.AddWithValue(
                        "@id",
                        id.Value
                    );

                    using (SqlDataReader reader =
                           cmdRol.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return HttpNotFound();
                        }

                        modelo.IdRol =
                            Convert.ToInt32(
                                reader["id_rol"]
                            );

                        modelo.NombreRol =
                            reader["nombre"].ToString();

                        modelo.DescripcionRol =
                            reader["descripcion"] == DBNull.Value
                                ? ""
                                : reader["descripcion"].ToString();
                    }
                }

                // Obtener docentes activos
                string sqlUsuarios = @"
                    SELECT
                        u.id_usuario,
                        u.nombre,
                        u.apellido1,
                        u.apellido2,
                        u.correo,
                        u.id_rol
                    FROM USUARIO u
                    INNER JOIN DOCENTE d
                        ON d.id_usuario = u.id_usuario
                    WHERE u.estado = 1
                    AND d.estado = 1
                    ORDER BY u.nombre, u.apellido1";

                using (SqlCommand cmdUsuarios =
                       new SqlCommand(sqlUsuarios, con))
                {
                    using (SqlDataReader reader =
                           cmdUsuarios.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string apellido2 =
                                reader["apellido2"] == DBNull.Value
                                    ? ""
                                    : reader["apellido2"].ToString();

                            modelo.Usuarios.Add(
                                new UsuarioAsignacionViewModel
                                {
                                    IdUsuario =
                                        Convert.ToInt32(
                                            reader["id_usuario"]
                                        ),

                                    NombreCompleto =
                                        (
                                            reader["nombre"] + " " +
                                            reader["apellido1"] + " " +
                                            apellido2
                                        ).Trim(),

                                    Correo =
                                        reader["correo"].ToString(),

                                    Seleccionado =
                                        Convert.ToInt32(
                                            reader["id_rol"]
                                        ) == id.Value
                                }
                            );
                        }
                    }
                }
            }

            return View(modelo);
        }


        // =========================================================
        // ASIGNAR ROL - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Asignar(
            AsignarRolViewModel modelo)
        {
            if (modelo == null ||
                modelo.IdRol <= 0)
            {
                TempData["Error"] =
                    "No fue posible identificar el rol seleccionado.";

                return RedirectToAction("Index");
            }

            List<int> seleccionados =
                new List<int>();

            if (modelo.Usuarios != null)
            {
                foreach (var usuario in modelo.Usuarios)
                {
                    if (usuario.Seleccionado)
                    {
                        seleccionados.Add(
                            usuario.IdUsuario
                        );
                    }
                }
            }

            using (SqlConnection con =
                   new SqlConnection(conexion))
            {
                con.Open();

                // Verificar que exista el rol
                string sqlRol = @"
                    SELECT COUNT(*)
                    FROM ROL
                    WHERE id_rol = @idRol";

                using (SqlCommand cmdRol =
                       new SqlCommand(sqlRol, con))
                {
                    cmdRol.Parameters.AddWithValue(
                        "@idRol",
                        modelo.IdRol
                    );

                    int existe =
                        Convert.ToInt32(
                            cmdRol.ExecuteScalar()
                        );

                    if (existe == 0)
                    {
                        TempData["Error"] =
                            "El rol seleccionado no existe.";

                        return RedirectToAction("Index");
                    }
                }

                // Asignar rol a usuarios seleccionados
                foreach (int idUsuario in seleccionados)
                {
                    string sqlActualizar = @"
                        UPDATE u
                        SET u.id_rol = @idRol
                        FROM USUARIO u
                        INNER JOIN DOCENTE d
                            ON d.id_usuario = u.id_usuario
                        WHERE u.id_usuario = @idUsuario
                        AND u.estado = 1
                        AND d.estado = 1";

                    using (SqlCommand cmd =
                           new SqlCommand(
                               sqlActualizar,
                               con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@idRol",
                            modelo.IdRol
                        );

                        cmd.Parameters.AddWithValue(
                            "@idUsuario",
                            idUsuario
                        );

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["Exito"] =
                "El rol se asignó correctamente.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // PERMISOS - MOSTRAR
        // =========================================================
        [HttpGet]
        public ActionResult Permisos(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            RolPermisosViewModel modelo =
                new RolPermisosViewModel();

            using (SqlConnection con =
                   new SqlConnection(conexion))
            {
                con.Open();

                // Obtener información del rol
                string sqlRol = @"
                    SELECT id_rol, nombre, descripcion
                    FROM ROL
                    WHERE id_rol = @id";

                using (SqlCommand cmd =
                       new SqlCommand(sqlRol, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@id",
                        id.Value
                    );

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return HttpNotFound();
                        }

                        modelo.IdRol =
                            Convert.ToInt32(
                                reader["id_rol"]
                            );

                        modelo.NombreRol =
                            reader["nombre"].ToString();

                        modelo.DescripcionRol =
                            reader["descripcion"] == DBNull.Value
                                ? ""
                                : reader["descripcion"].ToString();
                    }
                }

                // Obtener permisos
                string sqlPermisos = @"
                    SELECT
                        p.id_permiso,
                        p.nombre,
                        p.descripcion,
                        p.modulo,

                        CASE
                            WHEN rp.id_permiso IS NULL
                            THEN 0
                            ELSE 1
                        END AS seleccionado

                    FROM PERMISO p

                    LEFT JOIN ROL_PERMISO rp
                        ON p.id_permiso = rp.id_permiso
                        AND rp.id_rol = @idRol

                    WHERE p.estado = 1

                    ORDER BY p.modulo, p.nombre";

                using (SqlCommand cmd =
                       new SqlCommand(
                           sqlPermisos,
                           con))
                {
                    cmd.Parameters.AddWithValue(
                        "@idRol",
                        id.Value
                    );

                    using (SqlDataReader reader =
                           cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            modelo.Permisos.Add(
                                new PermisoViewModel
                                {
                                    IdPermiso =
                                        Convert.ToInt32(
                                            reader["id_permiso"]
                                        ),

                                    Nombre =
                                        reader["nombre"].ToString(),

                                    Descripcion =
                                        reader["descripcion"]
                                        == DBNull.Value
                                            ? ""
                                            : reader["descripcion"]
                                                .ToString(),

                                    Modulo =
                                        reader["modulo"].ToString(),

                                    Seleccionado =
                                        Convert.ToBoolean(
                                            reader["seleccionado"]
                                        )
                                }
                            );
                        }
                    }
                }
            }

            return View(modelo);
        }


        // =========================================================
        // PERMISOS - GUARDAR
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Permisos(
            RolPermisosViewModel modelo)
        {
            if (modelo == null ||
                modelo.IdRol <= 0)
            {
                TempData["Error"] =
                    "No fue posible identificar el rol.";

                return RedirectToAction("Index");
            }

            using (SqlConnection con =
                   new SqlConnection(conexion))
            {
                con.Open();

                SqlTransaction transaccion =
                    con.BeginTransaction();

                try
                {
                    // Eliminar permisos actuales
                    string sqlEliminar = @"
                        DELETE FROM ROL_PERMISO
                        WHERE id_rol = @idRol";

                    using (SqlCommand cmd =
                           new SqlCommand(
                               sqlEliminar,
                               con,
                               transaccion))
                    {
                        cmd.Parameters.AddWithValue(
                            "@idRol",
                            modelo.IdRol
                        );

                        cmd.ExecuteNonQuery();
                    }

                    // Insertar permisos seleccionados
                    if (modelo.Permisos != null)
                    {
                        foreach (var permiso
                                 in modelo.Permisos)
                        {
                            if (!permiso.Seleccionado)
                            {
                                continue;
                            }

                            string sqlInsertar = @"
                                INSERT INTO ROL_PERMISO
                                    (id_rol, id_permiso)
                                VALUES
                                    (@idRol, @idPermiso)";

                            using (SqlCommand cmd =
                                   new SqlCommand(
                                       sqlInsertar,
                                       con,
                                       transaccion))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@idRol",
                                    modelo.IdRol
                                );

                                cmd.Parameters.AddWithValue(
                                    "@idPermiso",
                                    permiso.IdPermiso
                                );

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaccion.Commit();
                }
                catch
                {
                    transaccion.Rollback();

                    TempData["Error"] =
                        "Ocurrió un error al guardar los permisos.";

                    return RedirectToAction(
                        "Permisos",
                        new
                        {
                            id = modelo.IdRol
                        }
                    );
                }
            }

            TempData["Exito"] =
                "Los permisos del rol se actualizaron correctamente.";

            return RedirectToAction("Index");
        }
    }
}