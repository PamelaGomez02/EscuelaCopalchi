using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>Notificaciones de los usuarios (tabla NOTIFICACION).</summary>
    public interface INotificacionRepository
    {
        void Crear(int idUsuario, string tipo, string titulo, string mensaje);

        /// <param name="idUsuario">0 = de todos los usuarios.</param>
        /// <param name="estadoEnvio">"Pendiente", "Enviada", "Error" o vacío para todos.</param>
        List<Notificacion> Listar(int idUsuario, bool soloNoLeidas = false, string estadoEnvio = "");

        /// <param name="idNotificacion">0 = todas las del usuario.</param>
        void MarcarLeida(int idNotificacion, int idUsuario);

        void ActualizarEnvio(int idNotificacion, string estadoEnvio);
    }

    public class NotificacionRepository : INotificacionRepository
    {
        private readonly IAccesoDatos db;

        public NotificacionRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public void Crear(int idUsuario, string tipo, string titulo, string mensaje)
        {
            db.Ejecutar("SP_GA_Notificacion_Insertar",
                P("@id_usuario", idUsuario), P("@tipo", tipo), P("@titulo", titulo), P("@mensaje", mensaje));
        }

        public List<Notificacion> Listar(int idUsuario, bool soloNoLeidas = false, string estadoEnvio = "")
        {
            return db.Consultar("SP_GA_Notificacion_Listar",
                        P("@id_usuario", idUsuario), P("@solo_no_leidas", soloNoLeidas), P("@estado_envio", estadoEnvio ?? ""))
                     .AsEnumerable()
                     .Select(r => new Notificacion
                     {
                         IdNotificacion = Fila.Entero(r, "id_notificacion"),
                         IdUsuario = Fila.Entero(r, "id_usuario"),
                         Tipo = Fila.Texto(r, "tipo"),
                         Titulo = Fila.Texto(r, "titulo"),
                         Mensaje = Fila.Texto(r, "mensaje"),
                         Fecha = Fila.Fecha(r, "fecha") ?? DateTime.MinValue,
                         Leida = Fila.Bool(r, "leida"),
                         EstadoEnvio = Fila.Texto(r, "estado_envio"),
                         Correo = Fila.Texto(r, "correo"),
                         Destinatario = Fila.Texto(r, "destinatario")
                     }).ToList();
        }

        public void MarcarLeida(int idNotificacion, int idUsuario)
        {
            db.Ejecutar("SP_GA_Notificacion_MarcarLeida", P("@id_notificacion", idNotificacion), P("@id_usuario", idUsuario));
        }

        public void ActualizarEnvio(int idNotificacion, string estadoEnvio)
        {
            db.Ejecutar("SP_GA_Notificacion_ActualizarEnvio", P("@id_notificacion", idNotificacion), P("@estado_envio", estadoEnvio));
        }
    }
}