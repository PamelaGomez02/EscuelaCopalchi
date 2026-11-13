using EscuelaCopalchi.UI.Models.Auditoria;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class AuditoriaController : Controller
    {
        private readonly AuditoriaRepository repo = new AuditoriaRepository();

        // La autorización depende de Roles y Permisos, no de un rol fijo.
        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            int idUsuario;
            if (Session["IdUsuario"] == null ||
                !int.TryParse(Convert.ToString(Session["IdUsuario"]), out idUsuario) ||
                idUsuario <= 0)
            {
                context.Result = new HttpStatusCodeResult(401, "Inicia sesión para continuar.");
                return;
            }

            if (!TienePermisoAuditoria(idUsuario))
            {
                context.Result = new HttpStatusCodeResult(403, "No tiene el permiso VER_AUDITORIA.");
                return;
            }

            base.OnActionExecuting(context);
        }

        private bool TienePermisoAuditoria(int idUsuario)
        {
            var configuracion = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"];
            if (configuracion == null)
                throw new ConfigurationErrorsException("No se encontró la conexión AulaVirtualCopalchi.");

            const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.USUARIO u
                    INNER JOIN dbo.ROL r ON r.id_rol = u.id_rol
                    INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol = r.id_rol
                    INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                    WHERE u.id_usuario = @id_usuario
                      AND u.estado = 1
                      AND r.estado = 1
                      AND p.estado = 1
                      AND p.nombre = N'VER_AUDITORIA'
                ) THEN 1 ELSE 0 END";

            using (var conexion = new SqlConnection(configuracion.ConnectionString))
            using (var comando = new SqlCommand(sql, conexion))
            {
                comando.Parameters.Add("@id_usuario", SqlDbType.Int).Value = idUsuario;
                conexion.Open();
                return Convert.ToInt32(comando.ExecuteScalar()) == 1;
            }
        }

        private void CargarUsuarios()
        {
            var lista = new List<SelectListItem>
    {
        new SelectListItem
        {
            Value = "",
            Text = "Todos los usuarios"
        }
    };

            string cadena = ConfigurationManager
                .ConnectionStrings["AulaVirtualDB"]
                .ConnectionString;

            using (var conexion = new SqlConnection(cadena))
            using (var comando = new SqlCommand(@"
        SELECT
            id_usuario,
            LTRIM(RTRIM(
                CONCAT(nombre, N' ', apellido1)
            )) AS nombre_completo
        FROM dbo.USUARIO
        ORDER BY nombre_completo, id_usuario;
    ", conexion))
            {
                conexion.Open();

                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        int id = Convert.ToInt32(
                            lector["id_usuario"]);

                        string nombre = Convert.ToString(
                            lector["nombre_completo"]);

                        lista.Add(new SelectListItem
                        {
                            Value = id.ToString(),
                            Text = string.IsNullOrWhiteSpace(nombre)
                                ? "Usuario #" + id
                                : nombre
                        });
                    }
                }
            }

            ViewBag.Usuarios = lista;
        }

        [HttpGet]
        public ActionResult Index(AuditoriaFiltro filtro)
        {
            if (filtro == null) filtro = new AuditoriaFiltro();
            if (filtro.Pagina < 1) filtro.Pagina = 1;
            if (filtro.TamanoPagina < 1 || filtro.TamanoPagina > 100) filtro.TamanoPagina = 20;
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
            {
                ModelState.AddModelError("", "La fecha inicial no puede superar la fecha final.");
                return View(new AuditoriaListadoVM { Filtro = filtro });
            }
            if (!ModelState.IsValid) return View(new AuditoriaListadoVM { Filtro = filtro });
            return View(repo.Consultar(filtro));
        }

        [HttpGet]
        public ActionResult Detalle(long id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400);
            var registro = repo.Obtener(id);
            if (registro == null) return HttpNotFound();
            return View(registro);
        }
    }
}
