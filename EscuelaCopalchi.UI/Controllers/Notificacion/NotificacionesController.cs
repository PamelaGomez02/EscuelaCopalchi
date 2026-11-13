
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Notificaciones;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

using System;
using System.Linq;
using System.Web.Mvc;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly NotificacionRepository repository;

        public NotificacionesController()
        {
            repository = new NotificacionRepository();
        }
        // ELIMINAR NOTIFICACIÓN DE LA BASE DE DATOS
        private bool EliminarNotificacionBD(
    int idNotificacion,
    int idUsuario)
        {
            var cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"];

            if (cadena == null)
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la conexión AulaVirtualDB.");
            }

            using (SqlConnection conexion =
                new SqlConnection(cadena.ConnectionString))
            {
                using (SqlCommand comando =
                    new SqlCommand("dbo.SP_NOT_Eliminar", conexion))
                {
                    comando.CommandType = CommandType.StoredProcedure;

                    comando.Parameters.Add(
                        "@id_notificacion",
                        SqlDbType.Int).Value = idNotificacion;

                    comando.Parameters.Add(
                        "@id_usuario",
                        SqlDbType.Int).Value = idUsuario;

                    conexion.Open();

                    object resultado = comando.ExecuteScalar();

                    return resultado != null &&
                           resultado != DBNull.Value &&
                           Convert.ToBoolean(resultado);
                }
            }
        }


        private string ObtenerDestinoAcademico(
    int idNotificacion,
    int idUsuario)
        {
            var cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"];

            if (cadena == null)
                throw new ConfigurationErrorsException(
                    "No existe la conexión AulaVirtualDB.");

            using (var conexion =
                new SqlConnection(cadena.ConnectionString))
            using (var comando = new SqlCommand(
                "dbo.SP_NOT_ObtenerDestinoAcademico",
                conexion))
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

                    return Url.Action(
                        "Detalle",
                        "MonitoreoDashboard",
                        new
                        {
                            id = Convert.ToInt32(
                                lector["id_estudiante"]),

                            grupo = Convert.ToInt32(
                                lector["id_grupo"]),

                            anio = Convert.ToInt32(
                                lector["anio"])
                        });
                }
            }
        }
        private int? ObtenerIdUsuario()
        {
            int id;

            if (Session["IdUsuario"] == null ||
                !int.TryParse(
                    Convert.ToString(Session["IdUsuario"]),
                    out id) || id <= 0)
            {
                return null;
            }

            return id;
        }

        private string ResolverUrlDestino(
    NotificacionDestino destino)
        {
            if (destino == null ||
                !destino.IdReferencia.HasValue ||
                destino.IdReferencia.Value <= 0)
            {
                return null;
            }

            string modulo = (destino.ModuloOrigen ?? "")
                .Trim().ToUpperInvariant();

            string accion = (destino.AccionDestino ?? "")
                .Trim().ToUpperInvariant();

            int id = destino.IdReferencia.Value;

            // Lista de rutas explícitamente permitidas.
            // Cada módulo se habilita después de verificar
            // su controlador y sus permisos.

            if (modulo == "ESTUDIANTES" &&
                accion == "DETALLE")
            {
                return Url.Action(
                    "Detalle",
                    "Estudiantes",
                    new { id = id }
                );
            }

            return null;
        }

        // PÁGINA DE NOTIFICACIONES
        [HttpGet]
        public ActionResult Index()
        {
            var idUsuario = ObtenerIdUsuario();

            if (!idUsuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                var notificaciones = repository
                    .ObtenerNotificaciones(idUsuario.Value)
                    .OrderByDescending(x => x.Fecha)
                    .ThenByDescending(x => x.IdNotificacion)
                    .ToList();

                return View(
                    "~/Views/Notificaciones/Index.cshtml",
                    notificaciones
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al cargar notificaciones: " + ex);

                return new HttpStatusCodeResult(
                    500,
                    "No fue posible cargar las notificaciones.");
            }
        }

        // CONFIGURACIÓN DE NOTIFICACIONES Y RECORDATORIOS
        [HttpGet]
        public ActionResult Configuracion()
        {
            var idUsuario = ObtenerIdUsuario();

            if (!idUsuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                // Preferencias generales de notificaciones
                var preferencias =
                    repository.ObtenerPreferencias(idUsuario.Value);

                // Frecuencias de recordatorios académicos
                var recordatorios =
                    repository.ObtenerRecordatorios(idUsuario.Value);

                ViewBag.Recordatorios = recordatorios;

                return View(
                    "~/Views/Notificaciones/Configuracion.cshtml",
                    preferencias
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al cargar configuración: " + ex);

                return new HttpStatusCodeResult(
                    500,
                    "No fue posible cargar las preferencias de notificaciones."
                );
            }
        }

        // RESUMEN PARA LA CAMPANA
        [HttpGet]
        public ActionResult Resumen()
        {
            var id = ObtenerIdUsuario();

            if (!id.HasValue)
                return new HttpStatusCodeResult(401);

            var lista = repository.ObtenerNotificaciones(id.Value);

            var notificaciones = lista
                .OrderBy(x => x.Leida)
                .ThenByDescending(x => x.Fecha)
                .ThenByDescending(x => x.IdNotificacion)
                .Select(x => new
                {
                    id = x.IdNotificacion,
                    titulo = x.Titulo,
                    mensaje = x.Mensaje,
                    tipo = x.Tipo,
                    fecha = x.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    leida = x.Leida
                })
                .ToList();

            return Json(new
            {
                total = lista.Count,
                pendientes = lista.Count(x => !x.Leida),
                notificaciones = notificaciones
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                bool eliminada = EliminarNotificacionBD(
                    id, usuario.Value);

                TempData[eliminada ? "Mensaje" : "Error"] =
                    eliminada
                    ? "Notificación eliminada correctamente."
                    : "No se encontró la notificación.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ERROR AL ELIMINAR: " + ex.ToString());

                TempData["Error"] =
                    "No fue posible eliminar la notificación.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarSeleccionadas(string ids)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            if (string.IsNullOrWhiteSpace(ids))
            {
                TempData["Error"] =
                    "No seleccionaste ninguna notificación.";

                return RedirectToAction("Index");
            }

            var identificadores = new List<int>();

            foreach (string valor in ids.Split(','))
            {
                int id;

                if (int.TryParse(valor.Trim(), out id) && id > 0)
                    identificadores.Add(id);
            }

            identificadores = identificadores.Distinct().ToList();

            if (identificadores.Count == 0 ||
                identificadores.Count > 500)
            {
                TempData["Error"] =
                    "La selección de notificaciones no es válida.";

                return RedirectToAction("Index");
            }

            int eliminadas = 0;
            int fallidas = 0;

            foreach (int id in identificadores)
            {
                try
                {
                    if (EliminarNotificacionBD(id, usuario.Value))
                        eliminadas++;
                    else
                        fallidas++;
                }
                catch (Exception ex)
                {
                    fallidas++;

                    System.Diagnostics.Trace.TraceError(
                        "Error al eliminar notificación " + id + ": " + ex);
                }
            }

            if (eliminadas > 0)
            {
                TempData["Mensaje"] =
                    "Se eliminaron " + eliminadas +
                    " notificación(es).";
            }

            if (fallidas > 0)
            {
                TempData["Error"] =
                    fallidas + " notificación(es) no pudieron eliminarse.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRecordatorio(
     RecordatorioConfiguracion recordatorio)
        {
            var idUsuario = ObtenerIdUsuario();

            if (!idUsuario.HasValue)
                return new HttpStatusCodeResult(401);

            if (recordatorio == null ||
                (recordatorio.TipoRecordatorio != "RIESGO_ACADEMICO" &&
                 recordatorio.TipoRecordatorio != "SEGUIMIENTO") ||
                recordatorio.FrecuenciaDias < 1 ||
                recordatorio.FrecuenciaDias > 365)
            {
                TempData["Error"] =
                    "La configuración del recordatorio no es válida.";

                return Redirect(
       Url.Action("Configuracion", "Notificaciones")
       + "#recordatorios"
   );
            }

            try
            {
                repository.GuardarRecordatorio(
                    idUsuario.Value,
                    recordatorio
                );

                TempData["Mensaje"] =
                    "Se procesó la configuración del recordatorio.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al guardar recordatorio: " + ex);

                TempData["Error"] =
                    "No fue posible guardar el recordatorio.";
            }

            return RedirectToAction("Configuracion");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarConfiguracion(
            PreferenciaNotificacion preferencia)
        {
            var idUsuario = ObtenerIdUsuario();

            if (!idUsuario.HasValue)
                return new HttpStatusCodeResult(401);

            if (preferencia == null ||
                preferencia.IdTipo <= 0 ||
                !new[] { "INMEDIATO", "DIARIO", "SEMANAL" }
                    .Contains(preferencia.FrecuenciaCorreo))
            {
                TempData["Error"] = "La configuración enviada no es válida.";
                return RedirectToAction("Configuracion");
            }

            try
            {
                repository.GuardarPreferencia(
                    idUsuario.Value,
                    preferencia
                );

                TempData["Mensaje"] =
                    "Se procesó la solicitud de configuración.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al guardar preferencias: " + ex);

                TempData["Error"] =
                    "No fue posible guardar la configuración.";
            }

            return RedirectToAction("Configuracion");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarConfiguracionGeneral(
            List<PreferenciaNotificacion> preferencias)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            if (preferencias == null || preferencias.Count == 0)
            {
                TempData["Error"] =
                    "No se recibieron preferencias para guardar.";

                return Redirect(
                    Url.Action("Configuracion") + "#general");
            }

            try
            {
                var existentes = repository
                    .ObtenerPreferencias(usuario.Value)
                    .ToDictionary(x => x.IdTipo);

                if (preferencias.Count != existentes.Count ||
                    preferencias.Select(x => x.IdTipo).Distinct().Count()
                        != preferencias.Count ||
                    preferencias.Any(x =>
                        !existentes.ContainsKey(x.IdTipo) ||
                        !new[] { "INMEDIATO", "DIARIO", "SEMANAL" }
                            .Contains(x.FrecuenciaCorreo)))
                {
                    TempData["Error"] =
                        "Las preferencias enviadas no son válidas.";

                    return Redirect(
                        Url.Action("Configuracion") + "#general");
                }

                foreach (var item in preferencias)
                {
                    // No permitir desactivar avisos obligatorios.
                    if (!existentes[item.IdTipo].PermiteDesactivar)
                        item.RecibirSistema = true;

                }

                repository.GuardarPreferenciasEnLote(usuario.Value, preferencias);
                TempData["Mensaje"] =
                    "Preferencias guardadas correctamente.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al guardar preferencias: " + ex);

                TempData["Error"] =
                    "No se pudieron guardar todas las preferencias.";
            }

            return Redirect(
                Url.Action("Configuracion") + "#general");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarRecordatoriosGeneral(
            List<RecordatorioConfiguracion> recordatorios)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            if (recordatorios == null || recordatorios.Count == 0)
            {
                TempData["Error"] =
                    "No se recibieron recordatorios para guardar.";

                return Redirect(
                    Url.Action("Configuracion") + "#recordatorios");
            }

            try
            {
                var existentes = repository
                    .ObtenerRecordatorios(usuario.Value)
                    .Select(x => x.TipoRecordatorio)
                    .ToList();

                if (recordatorios.Count != existentes.Count ||
                    recordatorios
                        .Select(x => x.TipoRecordatorio)
                        .Distinct().Count() != recordatorios.Count ||
                    recordatorios.Any(x =>
                        !existentes.Contains(x.TipoRecordatorio) ||
                        !new[] { "RIESGO_ACADEMICO", "SEGUIMIENTO" }
                            .Contains(x.TipoRecordatorio) ||
                        x.FrecuenciaDias < 1 ||
                        x.FrecuenciaDias > 365))
                {
                    TempData["Error"] =
                        "La configuración de recordatorios no es válida.";

                    return Redirect(
                        Url.Action("Configuracion") + "#recordatorios");
                }

                repository.GuardarRecordatoriosEnLote(usuario.Value, recordatorios);

                TempData["Mensaje"] =
                    "Recordatorios guardados correctamente.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al guardar recordatorios: " + ex);

                TempData["Error"] =
                    "No se pudieron guardar todos los recordatorios.";
            }

            return Redirect(
                Url.Action("Configuracion") + "#recordatorios");
        }


        // MARCAR UNA NOTIFICACIÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarLeida(int id)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                repository.MarcarComoLeida(id, usuario.Value);

                var lista = repository.ObtenerNotificaciones(
                    usuario.Value);

                bool actualizada = lista.Any(x =>
                    x.IdNotificacion == id && x.Leida);

                return Json(new
                {
                    ok = actualizada,
                    pendientes = lista.Count(x => !x.Leida)
                });
            }
            catch
            {
                Response.StatusCode = 500;

                return Json(new
                {
                    ok = false,
                    mensaje = "No se pudo actualizar la notificación."
                });
            }
        }

        [HttpGet]
        public ActionResult Abrir(int id)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            return RedirectToAction(
                "Detalle",
                "Notificaciones",
                new { id = id }
            );
        }

        [HttpGet]
        public ActionResult Detalle(int id)
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                var notificacion = repository
                    .ObtenerNotificaciones(usuario.Value)
                    .FirstOrDefault(x => x.IdNotificacion == id);

                if (notificacion == null)
                    return HttpNotFound();

                if (!notificacion.Leida)
                {
                    repository.MarcarComoLeida(
                        id,
                        usuario.Value
                    );

                    notificacion = repository
                        .ObtenerNotificaciones(usuario.Value)
                        .FirstOrDefault(x => x.IdNotificacion == id);

                    if (notificacion == null || !notificacion.Leida)
                    {
                        return new HttpStatusCodeResult(
                            500,
                            "No se pudo confirmar la lectura."
                        );
                    }
                }

                ViewBag.UrlVerMas = null;

                // Buscar destino general
                var destino = repository.ObtenerDestino(
                    id,
                    usuario.Value
                );

                string url = ResolverUrlDestino(destino);

                // Compatibilidad con recordatorios académicos
                // generados anteriormente.
                if (string.IsNullOrWhiteSpace(url))
                {
                    string tipo = (notificacion.Tipo ?? "")
                        .Trim().ToUpperInvariant();

                    if (tipo == "RIESGO_ACADEMICO" ||
                        tipo == "SEGUIMIENTO")
                    {
                        url = ObtenerDestinoAcademico(
                            id,
                            usuario.Value
                        );
                    }
                }

                ViewBag.UrlVerMas = url;

                return View(
                    "~/Views/Notificaciones/Detalle.cshtml",
                    notificacion
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al consultar notificación: " + ex
                );

                return new HttpStatusCodeResult(
                    500,
                    "No fue posible consultar la notificación."
                );
            }
        }

        // MARCAR TODAS COMO LEÍDAS

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarTodasLeidas()
        {
            var usuario = ObtenerIdUsuario();

            if (!usuario.HasValue)
                return new HttpStatusCodeResult(401);

            try
            {
                var antes = repository.ObtenerNotificaciones(
                    usuario.Value);

                int pendientesAntes = antes.Count(x => !x.Leida);

                string respuestaBD =
                    repository.MarcarTodasComoLeidas(usuario.Value);

                var despues = repository.ObtenerNotificaciones(
                    usuario.Value);

                int pendientesDespues =
                    despues.Count(x => !x.Leida);

                bool actualizado = pendientesDespues == 0;

                return Json(new
                {
                    ok = actualizado,
                    pendientes = pendientesDespues,
                    mensaje = actualizado
                        ? "Notificaciones actualizadas correctamente."
                        : "La actualización no se reflejó en la consulta.",
                    pendientesAntes = pendientesAntes,
                    respuestaBD = respuestaBD
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Error al marcar notificaciones: " + ex);

                Response.StatusCode = 500;

                return Json(new
                {
                    ok = false,
                    mensaje = "Error al actualizar las notificaciones."
                });
            }
        }


        // RIESGO ACADÉMICO
        [HttpGet]
        public ActionResult Riesgo()
        {
            var lista = repository.ObtenerEstudiantesRiesgo();

            ViewBag.TotalRiesgo = lista.Count;

            ViewBag.BajoRendimiento = lista.Count(x =>
                x.Motivo == "Bajo rendimiento");

            ViewBag.Inasistencia = lista.Count(x =>
                x.Motivo == "Inasistencia");

            return View(
                "~/Views/Notificaciones/Riesgo.cshtml",
                lista);
        }
    }
}
