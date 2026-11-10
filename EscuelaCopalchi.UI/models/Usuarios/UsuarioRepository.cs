using EscuelaCopalchi.UI.Models.Seguridad;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EscuelaCopalchi.UI.Models.Usuarios
{
    public class UsuarioRepository
    {
        private readonly ConexionBD db;
        private readonly string cadena;

        public UsuarioRepository()
        {
            db = new ConexionBD();
            var configuracion = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"];
            if (configuracion == null)
                throw new ConfigurationErrorsException("Falta la conexión AulaVirtualCopalchi en Web.config.");
            cadena = configuracion.ConnectionString;
        }

        private static SqlParameter ParametroTexto(string nombre, string valor, int longitud)
        {
            return new SqlParameter(nombre, SqlDbType.NVarChar, longitud)
            {
                Value = (object)valor ?? DBNull.Value
            };
        }

        private static string NombreRol(SqlConnection conexion, SqlTransaction tx, int idRol)
        {
            const string sql = "SELECT nombre FROM dbo.ROL WITH (UPDLOCK, HOLDLOCK) WHERE id_rol=@id AND estado=1";
            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idRol;
                var nombre = Convert.ToString(cmd.ExecuteScalar());
                if (string.IsNullOrWhiteSpace(nombre))
                    throw new InvalidOperationException("El rol seleccionado no existe o está inactivo.");
                return nombre.Trim();
            }
        }

        private static string RolActual(SqlConnection conexion, SqlTransaction tx, int idUsuario)
        {
            const string sql = @"SELECT r.nombre FROM dbo.USUARIO u WITH (UPDLOCK, HOLDLOCK)
                                 INNER JOIN dbo.ROL r ON r.id_rol=u.id_rol
                                 WHERE u.id_usuario=@id";
            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                var nombre = Convert.ToString(cmd.ExecuteScalar());
                if (string.IsNullOrWhiteSpace(nombre))
                    throw new InvalidOperationException("No se encontró el usuario.");
                return nombre.Trim();
            }
        }

        private static bool EsDocente(string rol)
        {
            return string.Equals(rol, "Docente", StringComparison.OrdinalIgnoreCase);
        }

        // Regla provisional: grupo vigente = grupo del año calendario actual.
        // Cambiar si la institución usa estado o períodos académicos distintos.
        private static bool TieneGruposVigentes(SqlConnection conexion, SqlTransaction tx, int idUsuario)
        {
            const string sql = @"SELECT COUNT(1)
                FROM dbo.GRUPO g WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.DOCENTE d ON d.id_docente=g.id_docente
                WHERE d.id_usuario=@id
                  AND TRY_CONVERT(int, g.periodo)=YEAR(GETDATE())";
            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void ValidarSalidaDocente(SqlConnection conexion, SqlTransaction tx,
            int idUsuario, string rolAnterior, string rolNuevo, bool desactivar)
        {
            if (EsDocente(rolAnterior) && (desactivar || !EsDocente(rolNuevo)) &&
                TieneGruposVigentes(conexion, tx, idUsuario))
            {
                throw new InvalidOperationException(
                    "Este docente tiene grupos del período vigente asignados. " +
                    "Reasigne sus grupos antes de cambiar el rol o desactivar la cuenta.");
            }
        }

        private static void AsegurarVinculoDocente(SqlConnection conexion, SqlTransaction tx,
            int idUsuario, string rolNuevo)
        {
            if (!EsDocente(rolNuevo)) return;

            // La tabla DOCENTE debe tener id_docente IDENTITY y una sola fila por id_usuario.
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.DOCENTE WITH (UPDLOCK, HOLDLOCK)
                               WHERE id_usuario=@id)
                BEGIN
                    INSERT INTO dbo.DOCENTE (id_usuario, fecha_ingreso, estado)
                    VALUES (@id, CAST(GETDATE() AS date), 1);
                END
                ELSE
                BEGIN
                    UPDATE dbo.DOCENTE SET estado=1 WHERE id_usuario=@id;
                END";
            using (var cmd = new SqlCommand(sql, conexion, tx))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                cmd.ExecuteNonQuery();
            }
        }

        public string Guardar(Usuario usuario)
        {
            if (usuario == null) throw new ArgumentNullException("usuario");
            if (string.IsNullOrWhiteSpace(usuario.Contrasena))
                throw new InvalidOperationException("Debe indicar una contraseña.");
            string hash = PasswordHelper.CrearHash(usuario.Contrasena);

            using (var conexion = new SqlConnection(cadena))
            {
                conexion.Open();
                using (var tx = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        string rol = NombreRol(conexion, tx, usuario.IdRol);
                        const string sql = @"INSERT INTO dbo.USUARIO
                            (id_rol, identificacion, nombre, apellido1, apellido2,
                             correo, telefono, contrasena_hash, estado)
                            OUTPUT INSERTED.id_usuario
                            VALUES (@rol, @identificacion, @nombre, @apellido1,
                                    @apellido2, @correo, @telefono, @hash, @estado)";
                        int id;
                        using (var cmd = new SqlCommand(sql, conexion, tx))
                        {
                            cmd.Parameters.Add("@rol", SqlDbType.Int).Value = usuario.IdRol;
                            cmd.Parameters.Add(ParametroTexto("@identificacion", usuario.Identificacion, 100));
                            cmd.Parameters.Add(ParametroTexto("@nombre", usuario.Nombre, 150));
                            cmd.Parameters.Add(ParametroTexto("@apellido1", usuario.Apellido1, 150));
                            cmd.Parameters.Add(ParametroTexto("@apellido2", usuario.Apellido2, 150));
                            cmd.Parameters.Add(ParametroTexto("@correo", usuario.Correo, 255));
                            cmd.Parameters.Add(ParametroTexto("@telefono", usuario.Telefono, 50));
                            cmd.Parameters.Add(ParametroTexto("@hash", hash, 500));
                            cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = usuario.Estado;
                            id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        AsegurarVinculoDocente(conexion, tx, id, rol);
                        tx.Commit();
                        return "Usuario registrado correctamente.";
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public string Actualizar(Usuario usuario)
        {
            if (usuario == null || usuario.IdUsuario <= 0)
                throw new InvalidOperationException("El usuario no es válido.");

            using (var conexion = new SqlConnection(cadena))
            {
                conexion.Open();
                using (var tx = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        string anterior = RolActual(conexion, tx, usuario.IdUsuario);
                        string nuevo = NombreRol(conexion, tx, usuario.IdRol);
                        ValidarSalidaDocente(conexion, tx, usuario.IdUsuario, anterior, nuevo, false);

                        const string sql = @"UPDATE dbo.USUARIO SET
                            id_rol=@rol, identificacion=@identificacion,
                            nombre=@nombre, apellido1=@apellido1, apellido2=@apellido2,
                            correo=@correo, telefono=@telefono
                            WHERE id_usuario=@id";
                        using (var cmd = new SqlCommand(sql, conexion, tx))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = usuario.IdUsuario;
                            cmd.Parameters.Add("@rol", SqlDbType.Int).Value = usuario.IdRol;
                            cmd.Parameters.Add(ParametroTexto("@identificacion", usuario.Identificacion, 100));
                            cmd.Parameters.Add(ParametroTexto("@nombre", usuario.Nombre, 150));
                            cmd.Parameters.Add(ParametroTexto("@apellido1", usuario.Apellido1, 150));
                            cmd.Parameters.Add(ParametroTexto("@apellido2", usuario.Apellido2, 150));
                            cmd.Parameters.Add(ParametroTexto("@correo", usuario.Correo, 255));
                            cmd.Parameters.Add(ParametroTexto("@telefono", usuario.Telefono, 50));
                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("No se encontró el usuario.");
                        }
                        AsegurarVinculoDocente(conexion, tx, usuario.IdUsuario, nuevo);
                        tx.Commit();
                        return "Usuario actualizado correctamente.";
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public string CambiarEstado(int idUsuario, bool nuevoEstado)
        {
            using (var conexion = new SqlConnection(cadena))
            {
                conexion.Open();
                using (var tx = conexion.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        string rol = RolActual(conexion, tx, idUsuario);
                        if (!nuevoEstado)
                            ValidarSalidaDocente(conexion, tx, idUsuario, rol, rol, true);
                        using (var cmd = new SqlCommand(
                            "UPDATE dbo.USUARIO SET estado=@estado WHERE id_usuario=@id", conexion, tx))
                        {
                            cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = nuevoEstado;
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("No se encontró el usuario.");
                        }
                        if (nuevoEstado) AsegurarVinculoDocente(conexion, tx, idUsuario, rol);
                        tx.Commit();
                        return nuevoEstado ? "Usuario activado correctamente." : "Usuario desactivado correctamente.";
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Se conserva la interfaz de consultas y contraseñas del repositorio existente.
        public List<Usuario> ObtenerTodos()
        {
            var lista = new List<Usuario>();
            DataTable dt = db.ejecutarProcedimiento("SP_ListarUsuarios", null, null);
            if (dt == null) throw new InvalidOperationException("No se pudo listar usuarios.");
            foreach (DataRow row in dt.Rows) lista.Add(MapearFila(row));
            return lista;
        }

        public Usuario ObtenerPorId(int idUsuario)
        {
            DataTable dt = db.ejecutarProcedimiento("SP_ObtenerUsuarioPorId",
                new[] { "@id_usuario" }, new[] { idUsuario.ToString() });
            return dt == null || dt.Rows.Count == 0 ? null : MapearFila(dt.Rows[0]);
        }

        public string CambiarContrasena(int idUsuario, string contrasena)
        {
            if (idUsuario <= 0) return "El usuario no es válido.";
            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < 8)
                return "La contraseña debe tener al menos 8 caracteres.";
            return db.ejecutarProcedimiento_Save("SP_CambiarContrasenaUsuario",
                new[] { "@id_usuario", "@contrasena_hash" },
                new[] { idUsuario.ToString(), PasswordHelper.CrearHash(contrasena) });
        }

        public bool VerificarContrasena(int idUsuario, string contrasena)
        {
            if (idUsuario <= 0 || string.IsNullOrEmpty(contrasena)) return false;
            DataTable dt = db.ejecutarProcedimiento("SP_ObtenerHashUsuario",
                new[] { "@id_usuario" }, new[] { idUsuario.ToString() });
            if (dt == null || dt.Rows.Count == 0) return false;
            return PasswordHelper.Verificar(contrasena,
                Convert.ToString(dt.Rows[0]["contrasena_hash"]));
        }

        // Compatibilidad: no permite eludir las validaciones usando otra ruta.
        public void ActualizarRolUsuario(int idUsuario, int idRolNuevo)
        {
            Usuario usuario = ObtenerPorId(idUsuario);
            if (usuario == null) throw new InvalidOperationException("No se encontró el usuario.");
            usuario.IdRol = idRolNuevo;
            Actualizar(usuario);
        }

        private Usuario MapearFila(DataRow row)
        {
            return new Usuario
            {
                IdUsuario = Convert.ToInt32(row["id_usuario"]),
                IdRol = Convert.ToInt32(row["id_rol"]),
                NombreRol = Convert.ToString(row["nombre_rol"]),
                Identificacion = Convert.ToString(row["identificacion"]),
                Nombre = Convert.ToString(row["nombre"]),
                Apellido1 = Convert.ToString(row["apellido1"]),
                Apellido2 = Convert.ToString(row["apellido2"]),
                Correo = Convert.ToString(row["correo"]),
                Telefono = Convert.ToString(row["telefono"]),
                Estado = Convert.ToBoolean(row["estado"]),
                UltimoAcceso = row["ultimo_acceso"] == DBNull.Value ?
                    (DateTime?)null : Convert.ToDateTime(row["ultimo_acceso"]),
                FechaCreacion = row["fecha_creacion"] == DBNull.Value ?
                    (DateTime?)null : Convert.ToDateTime(row["fecha_creacion"])
            };
        }
    }
}
