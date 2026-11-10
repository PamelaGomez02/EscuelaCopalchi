using System;
using System.Collections.Generic;
using System.Data;
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

        public string GuardarNotificacion(int idUsuario, string tipo, string titulo, string mensaje)
        {

            string[] parametros =
            {
            "@id_usuario",
            "@tipo",
            "@titulo",
            "@mensaje"
        };

            string[] valores =
            {
            idUsuario.ToString(),
            tipo,
            titulo,
            mensaje
        };

            return db.ejecutarProcedimiento_Save(
                "SP_GA_Notificacion_Insertar",
                parametros,
                valores);
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


    }
}