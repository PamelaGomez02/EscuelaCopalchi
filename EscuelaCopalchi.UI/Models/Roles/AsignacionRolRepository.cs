
using EscuelaCopalchi.UI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Script.Serialization;

namespace EscuelaCopalchi.UI.Models.Roles
{
    public class AsignacionRolRepository
    {
        private readonly string cadenaConexion;

        public AsignacionRolRepository()
        {
            cadenaConexion =
                new ConexionBD().ObtenerConexion();
        }

        public AsignarRolViewModel ObtenerModelo(int idRol)
        {
            var modelo = new AsignarRolViewModel
            {
                IdRol = idRol
            };

            using (var conn =
                new SqlConnection(cadenaConexion))
            {
                conn.Open();

                // Obtener información del rol
                using (var cmd = new SqlCommand(
                    "dbo.SP_Rol_ObtenerPorId", conn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.Add("@id_rol",
                        SqlDbType.Int).Value = idRol;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        modelo.NombreRol =
                            Convert.ToString(reader["nombre"]);

                        modelo.DescripcionRol =
                            reader["descripcion"] == DBNull.Value
                            ? ""
                            : Convert.ToString(
                                reader["descripcion"]);
                    }
                }

                // Obtener usuarios desde USUARIO
                using (var cmd = new SqlCommand(
                    "dbo.SP_Rol_UsuariosAsignables", conn))
                {
                    cmd.CommandType =
                        CommandType.StoredProcedure;

                    cmd.Parameters.Add("@id_rol",
                        SqlDbType.Int).Value = idRol;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string nombre =
                                Convert.ToString(reader["nombre"]);

                            string apellido1 =
                                Convert.ToString(reader["apellido1"]);

                            string apellido2 =
                                Convert.ToString(reader["apellido2"]);

                            modelo.Usuarios.Add(
                                new UsuarioAsignableViewModel
                                {
                                    IdUsuario = Convert.ToInt32(
                                        reader["id_usuario"]),

                                    NombreCompleto =
                                        string.Join(" ", new[]
                                        {
                                            nombre,
                                            apellido1,
                                            apellido2
                                        }.Where(x =>
                                            !string.IsNullOrWhiteSpace(x))),

                                    Correo = Convert.ToString(
                                        reader["correo"]),

                                    Seleccionado = Convert.ToBoolean(
                                        reader["seleccionado"])
                                });
                        }
                    }
                }
            }

            return modelo;
        }

        public void Guardar(
            int idRol,
            IEnumerable<int> idsUsuarios)
        {
            var ids = idsUsuarios
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            string json =
                new JavaScriptSerializer().Serialize(ids);

            using (var conn =
                new SqlConnection(cadenaConexion))
            using (var cmd = new SqlCommand(
                "dbo.SP_Rol_AsignarUsuarios", conn))
            {
                cmd.CommandType =
                    CommandType.StoredProcedure;

                cmd.Parameters.Add("@id_rol",
                    SqlDbType.Int).Value = idRol;

                cmd.Parameters.Add("@ids_json",
                    SqlDbType.NVarChar, -1).Value = json;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
