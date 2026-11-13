using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Notificaciones
{
    public class NotificacionRepository
    {

        private readonly ConexionBD db;

        public NotificacionRepository()
        {
            db = new ConexionBD();
        }

        public List<Notificacion> ObtenerNotificaciones(
        int idUsuario)
        {
            string[] parametros =
            {
                "@id_usuario"
            };

            string[] valores =
            {
                idUsuario.ToString()
            };

            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_OBTENER_NOTIFICACIONES",
                    parametros,
                    valores);

            List<Notificacion> lista =
                new List<Notificacion>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new Notificacion
                {
                    IdNotificacion =
                        Convert.ToInt32(row["id_notificacion"]),

                    IdUsuario =
                        Convert.ToInt32(row["id_usuario"]),

                    Tipo =
                        row["tipo"].ToString(),

                    Titulo =
                        row["titulo"].ToString(),

                    Mensaje =
                        row["mensaje"].ToString(),

                    Fecha =
                        Convert.ToDateTime(row["fecha"]),

                    Leida =
                        Convert.ToBoolean(row["leida"]),

                    EstadoEnvio =
                        row["estado_envio"].ToString()
                });
            }

            return lista;
        }

        public List<EstudianteRiesgo>
    ObtenerEstudiantesRiesgo()
        {
            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_OBTENER_ESTUDIANTES_RIESGO",
                    new string[] { },
                    new string[] { });

            List<EstudianteRiesgo> lista =
                new List<EstudianteRiesgo>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new EstudianteRiesgo
                {
                    IdEstudiante =
                        Convert.ToInt32(
                            row["id_estudiante"]),

                    NombreCompleto =
                        row["nombre_completo"]
                        .ToString(),

                    Grupo =
                        row["grupo"]
                        .ToString(),

                    Motivo =
                        row["motivo"]
                        .ToString(),

                    Docente =
                        row["docente"]
                        .ToString()
                });
            }

            return lista;
        }

        public string MarcarComoLeida(
            int idNotificacion,
            int idUsuario)
        {
            string[] parametros =
            {
        "@id_notificacion",
        "@id_usuario"
    };

            string[] valores =
            {
        idNotificacion.ToString(),
        idUsuario.ToString()
    };

            return db.ejecutarProcedimiento_Save(
                "SP_GA_Notificacion_MarcarLeida",
                parametros,
                valores);
        }

        public string MarcarTodasComoLeidas(int idUsuario)
        {
            string[] parametros =
            {
        "@id_usuario"
    };

            string[] valores =
            {
        idUsuario.ToString()
    };

            return db.ejecutarProcedimiento_Save(
                "SP_GA_Notificacion_MarcarTodasLeidas",
                parametros,
                valores);
        }



        public List<PreferenciaNotificacion> ObtenerPreferencias(int idUsuario)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_NOT_Preferencias_Obtener",
                new[] { "@id_usuario" },
                new[] { idUsuario.ToString() }
            );

            var lista = new List<PreferenciaNotificacion>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new PreferenciaNotificacion
                {
                    IdTipo = Convert.ToInt32(row["id_tipo"]),
                    Codigo = Convert.ToString(row["codigo"]),
                    Nombre = Convert.ToString(row["nombre"]),
                    Descripcion = Convert.ToString(row["descripcion"]),
                    PermiteDesactivar = Convert.ToBoolean(row["permite_desactivar"]),
                    RecibirSistema = Convert.ToBoolean(row["recibir_sistema"]),
                    RecibirCorreo = Convert.ToBoolean(row["recibir_correo"]),
                    FrecuenciaCorreo = Convert.ToString(row["frecuencia_correo"])
                });
            }

            return lista;
        }

        public string GuardarPreferencia(
            int idUsuario,
            PreferenciaNotificacion preferencia)
        {
            return db.ejecutarProcedimiento_Save(
                "SP_NOT_Preferencias_Guardar",
                new[]
                {
            "@id_usuario",
            "@id_tipo",
            "@recibir_sistema",
            "@recibir_correo",
            "@frecuencia_correo"
                },
                new[]
                {
            idUsuario.ToString(),
            preferencia.IdTipo.ToString(),
            preferencia.RecibirSistema ? "1" : "0",
            preferencia.RecibirCorreo ? "1" : "0",
            preferencia.FrecuenciaCorreo
                }
            );
        }

        public List<RecordatorioConfiguracion> ObtenerRecordatorios(
            int idUsuario)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_NOT_Recordatorios_Obtener",
                new[] { "@id_usuario" },
                new[] { idUsuario.ToString() }
            );

            var lista = new List<RecordatorioConfiguracion>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new RecordatorioConfiguracion
                {
                    TipoRecordatorio =
                        Convert.ToString(row["tipo_recordatorio"]),

                    FrecuenciaDias =
                        Convert.ToInt32(row["frecuencia_dias"]),

                    Activo =
                        Convert.ToBoolean(row["activo"])
                });
            }

            return lista;
        }
        public string GuardarNotificacion(
            int idUsuario,
            string tipo,
            string titulo,
            string mensaje,
            string moduloOrigen = null,
            int? idReferencia = null,
            string accionDestino = null)
        {
            var cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"];

            if (cadena == null)
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la conexión AulaVirtualDB.");
            }

            using (var conexion = new SqlConnection(
                cadena.ConnectionString))
            using (var comando = new SqlCommand(
                "dbo.SP_GA_Notificacion_Insertar",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add(
                    "@id_usuario", SqlDbType.Int
                ).Value = idUsuario;

                comando.Parameters.Add(
                    "@tipo", SqlDbType.NVarChar, 50
                ).Value = tipo;

                comando.Parameters.Add(
                    "@titulo", SqlDbType.NVarChar, 200
                ).Value = titulo;

                comando.Parameters.Add(
                    "@mensaje", SqlDbType.NVarChar, -1
                ).Value = mensaje;

                comando.Parameters.Add(
                    "@modulo_origen", SqlDbType.NVarChar, 80
                ).Value = (object)moduloOrigen ?? DBNull.Value;

                comando.Parameters.Add(
                    "@id_referencia", SqlDbType.Int
                ).Value = (object)idReferencia ?? DBNull.Value;

                comando.Parameters.Add(
                    "@accion_destino", SqlDbType.NVarChar, 80
                ).Value = (object)accionDestino ?? DBNull.Value;

                conexion.Open();
                comando.ExecuteNonQuery();

                return "Notificación registrada correctamente.";
            }
        }



        public string GuardarRecordatorio(
            int idUsuario,
            RecordatorioConfiguracion recordatorio)
        {
            if (recordatorio == null)
            {
                throw new ArgumentNullException(
                    nameof(recordatorio));
            }

            return db.ejecutarProcedimiento_Save(
                "SP_NOT_Recordatorios_Guardar",
                new[]
                {
            "@id_usuario",
            "@tipo_recordatorio",
            "@frecuencia_dias",
            "@activo"
                },
                new[]
                {
            idUsuario.ToString(),
            recordatorio.TipoRecordatorio,
            recordatorio.FrecuenciaDias.ToString(),
            recordatorio.Activo ? "1" : "0"
                }
            );
        }

        // Guardado masivo atómico de preferencias.
        public void GuardarPreferenciasEnLote(int idUsuario, List<PreferenciaNotificacion> preferencias)
        {
            if (preferencias == null || preferencias.Count == 0)
                throw new ArgumentException("No hay preferencias para guardar.");

            var cadena = ConfigurationManager.ConnectionStrings["AulaVirtualDB"];
            if (cadena == null) throw new ConfigurationErrorsException("Falta AulaVirtualDB.");

            using (var conexion = new SqlConnection(cadena.ConnectionString))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in preferencias)
                        {
                            using (var cmd = new SqlCommand("dbo.SP_NOT_Preferencias_Guardar", conexion, transaccion))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = idUsuario;
                                cmd.Parameters.Add("@id_tipo", SqlDbType.Int).Value = item.IdTipo;
                                cmd.Parameters.Add("@recibir_sistema", SqlDbType.Bit).Value = item.RecibirSistema;
                                cmd.Parameters.Add("@recibir_correo", SqlDbType.Bit).Value = item.RecibirCorreo;
                                cmd.Parameters.Add("@frecuencia_correo", SqlDbType.NVarChar, 20).Value = item.FrecuenciaCorreo;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Guardado masivo atómico de recordatorios.
        public void GuardarRecordatoriosEnLote(int idUsuario, List<RecordatorioConfiguracion> recordatorios)
        {
            if (recordatorios == null || recordatorios.Count == 0)
                throw new ArgumentException("No hay recordatorios para guardar.");

            var cadena = ConfigurationManager.ConnectionStrings["AulaVirtualDB"];
            if (cadena == null) throw new ConfigurationErrorsException("Falta AulaVirtualDB.");

            using (var conexion = new SqlConnection(cadena.ConnectionString))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in recordatorios)
                        {
                            using (var cmd = new SqlCommand("dbo.SP_NOT_Recordatorios_Guardar", conexion, transaccion))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = idUsuario;
                                cmd.Parameters.Add("@tipo_recordatorio", SqlDbType.NVarChar, 30).Value = item.TipoRecordatorio;
                                cmd.Parameters.Add("@frecuencia_dias", SqlDbType.Int).Value = item.FrecuenciaDias;
                                cmd.Parameters.Add("@activo", SqlDbType.Bit).Value = item.Activo;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        public NotificacionDestino ObtenerDestino(
       int idNotificacion,
       int idUsuario)
        {
            var cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"];

            if (cadena == null)
                throw new ConfigurationErrorsException(
                    "No se encontró AulaVirtualDB.");

            using (var conexion = new SqlConnection(
                cadena.ConnectionString))
            using (var comando = new SqlCommand(
                "dbo.SP_NOT_ObtenerDestino", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add(
                    "@id_notificacion", SqlDbType.Int
                ).Value = idNotificacion;

                comando.Parameters.Add(
                    "@id_usuario", SqlDbType.Int
                ).Value = idUsuario;

                conexion.Open();

                using (var lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                        return null;

                    return new NotificacionDestino
                    {
                        ModuloOrigen =
                            lector["modulo_origen"] == DBNull.Value
                            ? null
                            : Convert.ToString(lector["modulo_origen"]),

                        AccionDestino =
                            lector["accion_destino"] == DBNull.Value
                            ? null
                            : Convert.ToString(lector["accion_destino"]),

                        IdReferencia =
                            lector["id_referencia"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(lector["id_referencia"])
                    };
                }
            }
        }

        public bool AsignarDestino(
            int idNotificacion,
            int idUsuario,
            string modulo,
            string accion,
            int idReferencia)
        {
            var cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"];

            if (cadena == null)
                throw new ConfigurationErrorsException(
                    "No se encontró AulaVirtualDB.");

            using (var conexion = new SqlConnection(
                cadena.ConnectionString))
            using (var comando = new SqlCommand(
                "dbo.SP_NOT_AsignarDestino", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add(
                    "@id_notificacion", SqlDbType.Int
                ).Value = idNotificacion;

                comando.Parameters.Add(
                    "@id_usuario", SqlDbType.Int
                ).Value = idUsuario;

                comando.Parameters.Add(
                    "@modulo_origen", SqlDbType.NVarChar, 80
                ).Value = modulo;

                comando.Parameters.Add(
                    "@accion_destino", SqlDbType.NVarChar, 80
                ).Value = accion;

                comando.Parameters.Add(
                    "@id_referencia", SqlDbType.Int
                ).Value = idReferencia;

                conexion.Open();

                object resultado = comando.ExecuteScalar();

                return resultado != null &&
                       resultado != DBNull.Value &&
                       Convert.ToBoolean(resultado);
            }
        }

    }
}
