using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EscuelaCopalchi.UI.Models.Seguridad
{
    public class PermisosService
    {
        private readonly string conexion;
        public PermisosService()
        {
            var cs = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"];
            if (cs == null) throw new ConfigurationErrorsException("Falta la conexión AulaVirtualCopalchi.");
            conexion = cs.ConnectionString;
        }

        public HashSet<string> ObtenerPermisos(int idUsuario)
        {
            var resultado = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"SELECT DISTINCT p.nombre FROM dbo.USUARIO u
INNER JOIN dbo.ROL r ON r.id_rol=u.id_rol
INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
INNER JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1 AND p.estado=1";
            using (var conn = new SqlConnection(conexion))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) resultado.Add(Convert.ToString(reader[0]));
            }
            return resultado;
        }
        public bool TienePermiso(int idUsuario, string permiso)
        {
            return idUsuario > 0 && ObtenerPermisos(idUsuario).Contains(permiso);
        }
        public string ObtenerRol(int idUsuario)
        {
            const string sql = @"SELECT r.nombre FROM dbo.USUARIO u INNER JOIN dbo.ROL r ON r.id_rol=u.id_rol WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1";
            using (var conn = new SqlConnection(conexion))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                conn.Open();
                return Convert.ToString(cmd.ExecuteScalar());
            }
        }
    }
}
