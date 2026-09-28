using EscuelaCopalchi.UI.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class RolesController : Controller
    {
        private readonly string conexion =
            ConfigurationManager.ConnectionStrings["AulaVirtualDB"].ConnectionString;

 
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

        public ActionResult Crear()
        {
            return View();
        }

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

      
        // EDITAR ROL - MOSTRAR
     
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
                                IdRol = Convert.ToInt32(reader["id_rol"]),
                                Nombre = reader["nombre"].ToString(),

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


        // EDITAR ROL - GUARDAR
   
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

                // Verificar que no exista OTRO rol con ese nombre
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


        // ELIMINAR ROL
   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                // Verificar usuarios ACTIVOS asociados
                string sqlUsuarios = @"
            SELECT COUNT(*)
            FROM USUARIO
            WHERE id_rol = @id
              AND estado = 1";

                using (SqlCommand cmdUsuarios =
                       new SqlCommand(sqlUsuarios, con))
                {
                    cmdUsuarios.Parameters.AddWithValue("@id", id);

                    int usuarios =
                        Convert.ToInt32(
                            cmdUsuarios.ExecuteScalar()
                        );

                    if (usuarios > 0)
                    {
                        TempData["Error"] =
                            "No se puede eliminar el rol porque tiene usuarios activos asociados. Debe desvincularlos primero.";

                        return RedirectToAction(
                            "Editar",
                            new { id = id }
                        );
                    }
                }


                string sqlEliminar = @"
            DELETE FROM ROL
            WHERE id_rol = @id";

                using (SqlCommand cmd =
                       new SqlCommand(sqlEliminar, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Exito"] =
                "El rol se eliminó correctamente.";

            return RedirectToAction("Index");
        }


        // ASIGNAR ROL - MOSTRAR DOCENTES

        [HttpGet]
        public ActionResult Asignar(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            AsignarRolViewModel modelo = new AsignarRolViewModel();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

               
                string sqlRol = @"
            SELECT id_rol, nombre, descripcion
            FROM ROL
            WHERE id_rol = @id";

                using (SqlCommand cmdRol = new SqlCommand(sqlRol, con))
                {
                    cmdRol.Parameters.AddWithValue("@id", id.Value);

                    using (SqlDataReader reader = cmdRol.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return HttpNotFound();
                        }

                        modelo.IdRol = Convert.ToInt32(reader["id_rol"]);
                        modelo.NombreRol = reader["nombre"].ToString();

                        modelo.DescripcionRol =
                            reader["descripcion"] == DBNull.Value
                            ? ""
                            : reader["descripcion"].ToString();
                    }
                }

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


 
        // ASIGNAR ROL - GUARDAR
    
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

            List<int> seleccionados = new List<int>();

            if (modelo.Usuarios != null)
            {
                foreach (var usuario in modelo.Usuarios)
                {
                    if (usuario.Seleccionado)
                    {
                        seleccionados.Add(usuario.IdUsuario);
                    }
                }
            }

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

              
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
                        Convert.ToInt32(cmdRol.ExecuteScalar());

                    if (existe == 0)
                    {
                        TempData["Error"] =
                            "El rol seleccionado no existe.";

                        return RedirectToAction("Index");
                    }
                }

           
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
                           new SqlCommand(sqlActualizar, con))
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




        // PERMISOS

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
                       new SqlCommand(sqlPermisos, con))
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Permisos(
            RolPermisosViewModel modelo)
        {
            if (modelo == null || modelo.IdRol <= 0)
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
                        new { id = modelo.IdRol }
                    );
                }
            }

            TempData["Exito"] =
                "Los permisos del rol se actualizaron correctamente.";

            return RedirectToAction("Index");
        }
    }
}