using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Notificaciones de los usuarios.
    ///  - Las crean los demás servicios (grupos, asignaciones) dentro de su transacción.
    ///  - Siempre quedan visibles dentro del sistema (campana).
    ///  - Si GA_EnviarCorreos = true en Web.config, además se envían por correo.
    ///  - Si el envío falla, quedan en estado "Error" y se registra el incidente en AUDITORIA.
    /// </summary>
    public class NotificacionService
    {
        public List<Notificacion> Listar(int idUsuario, bool soloNoLeidas)
        {
            return Repositorios.Consulta().Notificaciones.Listar(idUsuario, soloNoLeidas);
        }

        public int ContarNoLeidas(int idUsuario)
        {
            return Repositorios.Consulta().Notificaciones.Listar(idUsuario, true).Count;
        }

        /// <param name="idNotificacion">0 = todas.</param>
        public ResultadoOperacion MarcarLeida(int idNotificacion, int idUsuario)
        {
            return Operacion.Ejecutar(() =>
            {
                Repositorios.Consulta().Notificaciones.MarcarLeida(idNotificacion, idUsuario);
                return "Notificaciones actualizadas.";
            });
        }

        public List<IncidenteNotificacion> ListarIncidentes()
        {
            return Repositorios.Consulta().Auditoria.Listar(Modulos.Notificaciones, TiposCambio.ErrorEnvio, null, null)
                               .Select(r => new IncidenteNotificacion { Fecha = r.Fecha, Detalle = r.Detalle })
                               .ToList();
        }

        /// <summary>Vuelve a intentar las notificaciones con error. Devuelve cuántas volvieron a fallar.</summary>
        public int ReintentarConError()
        {
            var repositorio = Repositorios.Consulta().Notificaciones;

            foreach (Notificacion n in repositorio.Listar(0, false, "Error"))
                repositorio.ActualizarEnvio(n.IdNotificacion, "Pendiente");

            return ProcesarPendientes();
        }

        /// <summary>
        /// Entrega las notificaciones pendientes. Devuelve cuántas fallaron.
        /// Nunca lanza excepciones: si falla al notificar, la operación principal ya quedó guardada.
        /// </summary>
        public int ProcesarPendientes()
        {
            int errores = 0;

            try
            {
                Repositorios datos = Repositorios.Consulta();
                var repositorio = datos.Notificaciones;

                foreach (Notificacion n in repositorio.Listar(0, false, "Pendiente").OrderBy(x => x.IdNotificacion))
                {
                    if (!CorreoHabilitado)
                    {
                        // Solo notificación interna (campana del sistema)
                        repositorio.ActualizarEnvio(n.IdNotificacion, "Enviada");
                        continue;
                    }

                    string error = EnviarCorreo(n);

                    if (error == null)
                    {
                        repositorio.ActualizarEnvio(n.IdNotificacion, "Enviada");
                    }
                    else
                    {
                        errores++;
                        repositorio.ActualizarEnvio(n.IdNotificacion, "Error");
                        datos.Auditoria.Registrar(Modulos.Notificaciones, TiposCambio.ErrorEnvio, "NOTIFICACION", n.IdNotificacion,
                                                  "\"" + n.Titulo + "\" para " + n.Destinatario + ": " + error, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Gestión académica - error al procesar notificaciones: " + ex);
            }

            return errores;
        }

        // ================================================================
        // Correo
        // ================================================================

        private static string Config(string clave, string porDefecto = "")
        {
            return ConfigurationManager.AppSettings[clave] ?? porDefecto;
        }

        private static bool CorreoHabilitado
        {
            get
            {
                bool valor;
                return bool.TryParse(Config("GA_EnviarCorreos", "false"), out valor) && valor;
            }
        }

        /// <summary>Envía un correo. Devuelve null si salió bien, o el detalle del error.</summary>
        private static string EnviarCorreo(Notificacion n)
        {
            if (string.IsNullOrWhiteSpace(n.Correo))
                return "El destinatario " + n.Destinatario + " no tiene un correo registrado.";

            try
            {
                int puerto;
                bool ssl;
                int.TryParse(Config("GA_SmtpPuerto", "587"), out puerto);
                bool.TryParse(Config("GA_SmtpSsl", "true"), out ssl);

                using (var mensaje = new MailMessage())
                using (var smtp = new SmtpClient(Config("GA_SmtpHost"), puerto == 0 ? 587 : puerto))
                {
                    mensaje.From = new MailAddress(Config("GA_CorreoRemitente"), "Aula Virtual Copalchi");
                    mensaje.To.Add(new MailAddress(n.Correo, n.Destinatario));
                    mensaje.Subject = "Aula Virtual Copalchi - " + n.Titulo;
                    mensaje.Body = "Hola " + n.Destinatario + ",\n\n" + n.Mensaje +
                                   "\n\nEste es un mensaje automático del módulo de Gestión Académica.";

                    smtp.EnableSsl = ssl;
                    smtp.Timeout = 15000;

                    string usuario = Config("GA_SmtpUsuario");
                    if (!string.IsNullOrEmpty(usuario))
                        smtp.Credentials = new NetworkCredential(usuario, Config("GA_SmtpClave"));

                    smtp.Send(mensaje);
                }

                return null;
            }
            catch (Exception ex)
            {
                return "No se pudo enviar el correo a " + n.Correo + ": " +
                       (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }
    }
}