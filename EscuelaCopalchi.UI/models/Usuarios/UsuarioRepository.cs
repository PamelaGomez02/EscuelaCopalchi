using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using Org.BouncyCastle.Crypto.Generators;

namespace EscuelaCopalchi.UI.Models.Usuarios
{
    public class UsuarioRepository
    {
        private readonly ConexionBD db;

        public UsuarioRepository()
        {
            db = new ConexionBD();
        }

        // Registrar usuario (la contraseña se hashea aquí)
        public string Guardar(Usuario usuario)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasena);

            string[] parametros =
            {
                "@id_rol", "@identificacion", "@nombre", "@apellido1",
                "@apellido2", "@correo", "@telefono", "@contrasena_hash", "@estado"
            };

            string[] valores =
            {
                usuario.IdRol.ToString(),
                usuario.Identificacion,
                usuario.Nombre,
                usuario.Apellido1,
                usuario.Apellido2,
                usuario.Correo,
                usuario.Telefono,
                hash,
                usuario.Estado ? "1" : "0"
            };

            return db.ejecutarProcedimiento_Save(
                "SP_InsertarUsuario", parametros, valores);
        }

        // Listar usuarios
        public List<Usuario> ObtenerTodos()
        {
            List<Usuario> lista = new List<Usuario>();

            DataTable dt = db.ejecutarProcedimiento(
                "SP_ListarUsuarios", null, null);

            if (dt == null)
            {
                throw new Exception("DataTable es NULL");
            }

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(MapearFila(row));
            }

            return lista;
        }

        // Obtener un usuario por id (Editar y Detalle)
        public Usuario ObtenerPorId(int idUsuario)
        {
            string[] parametros = { "@id_usuario" };
            string[] valores = { idUsuario.ToString() };

            DataTable dt = db.ejecutarProcedimiento(
                "SP_ObtenerUsuarioPorId", parametros, valores);

            if (dt == null || dt.Rows.Count == 0)
            {
                return null;
            }

            return MapearFila(dt.Rows[0]);
        }

        // Actualizar datos del usuario
        public string Actualizar(Usuario usuario)
        {
            string[] parametros =
            {
                "@id_usuario", "@id_rol", "@identificacion", "@nombre",
                "@apellido1", "@apellido2", "@correo", "@telefono"
            };

            string[] valores =
            {
                usuario.IdUsuario.ToString(),
                usuario.IdRol.ToString(),
                usuario.Identificacion,
                usuario.Nombre,
                usuario.Apellido1,
                usuario.Apellido2,
                usuario.Correo,
                usuario.Telefono
            };

            return db.ejecutarProcedimiento_Save(
                "SP_ActualizarUsuario", parametros, valores);
        }

        // Activar / desactivar
        public string CambiarEstado(int idUsuario, bool nuevoEstado)
        {
            string[] parametros = { "@id_usuario", "@estado" };
            string[] valores =
            {
                idUsuario.ToString(),
                nuevoEstado ? "1" : "0"
            };

            return db.ejecutarProcedimiento_Save(
                "SP_CambiarEstadoUsuario", parametros, valores);
        }

        private Usuario MapearFila(DataRow row)
        {
            return new Usuario
            {
                IdUsuario = Convert.ToInt32(row["id_usuario"]),
                IdRol = Convert.ToInt32(row["id_rol"]),
                NombreRol = row["nombre_rol"].ToString(),
                Identificacion = row["identificacion"].ToString(),
                Nombre = row["nombre"].ToString(),
                Apellido1 = row["apellido1"].ToString(),
                Apellido2 = row["apellido2"].ToString(),
                Correo = row["correo"].ToString(),
                Telefono = row["telefono"].ToString(),
                Estado = Convert.ToBoolean(row["estado"]),
                UltimoAcceso = row["ultimo_acceso"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["ultimo_acceso"]),
                FechaCreacion = row["fecha_creacion"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["fecha_creacion"])
            };
        }
    }
}
