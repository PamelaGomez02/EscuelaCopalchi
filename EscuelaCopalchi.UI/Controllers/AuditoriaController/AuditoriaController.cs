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

        // Los permisos se consultan en cada solicitud desde SQL Server.
        private int UsuarioActual
        {
            get
            {
                int id;
                return Session["IdUsuario"] != null &&
                    int.TryParse(Convert.ToString(Session["IdUsuario"]), out id) && id > 0
                    ? id : 0;
            }
        }

        private bool TienePermiso(string nombre)
        {
            var cs = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"];
            if (cs == null)
                throw new ConfigurationErrorsException("Falta AulaVirtualCopalchi en Web.config.");

            const string sql = @"SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.USUARIO u
                INNER JOIN dbo.ROL r ON r.id_rol = u.id_rol
                INNER JOIN dbo.ROL_PERMISO rp ON rp.id_rol = r.id_rol
                INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                WHERE u.id_usuario = @id AND u.estado = 1
                  AND r.estado = 1 AND p.estado = 1 AND p.nombre = @permiso
            ) THEN 1 ELSE 0 END";
            using (var cn = new SqlConnection(cs.ConnectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = UsuarioActual;
                cmd.Parameters.Add("@permiso", SqlDbType.NVarChar, 100).Value = nombre;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }

        private bool PuedeVerTodo { get { return TienePermiso("VER_AUDITORIA"); } }
        private bool PuedeVerPropio { get { return TienePermiso("VER_MI_AUDITORIA"); } }

        protected override void OnActionExecuting(ActionExecutingContext context)
        {
            if (UsuarioActual <= 0)
            {
                context.Result = new HttpStatusCodeResult(401, "Inicia sesión para continuar.");
                return;
            }
            if (!PuedeVerTodo && !PuedeVerPropio)
            {
                context.Result = new HttpStatusCodeResult(403, "Sin permisos de auditoría.");
                return;
            }
            base.OnActionExecuting(context);
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
            filtro.TamanoPagina = 10;

            bool general = PuedeVerTodo;
            ViewBag.PuedeVerTodo = general;

            // Seguridad: ignorar cualquier IdUsuario enviado por la URL
            // si el usuario solo puede consultar su propia actividad.
            if (!general)
            {
                filtro.IdUsuario = UsuarioActual;
                ModelState.Remove("IdUsuario");
            }
            else
            {
                CargarUsuarios();
            }

            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
            {
                ModelState.AddModelError("", "La fecha inicial no puede superar la fecha final.");
            }
            if (!ModelState.IsValid)
                return View(new AuditoriaListadoVM { Filtro = filtro });

            return View(repo.Consultar(filtro));
        }

        [HttpGet]
        public ActionResult Detalle(long id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400);
            var registro = repo.Obtener(id);
            if (registro == null) return HttpNotFound();

            // Nunca confiar en el ID de la URL para consultar actividad ajena.
            if (!PuedeVerTodo && registro.IdUsuario != UsuarioActual)
                return HttpNotFound();

            return View(registro);
        }
    }
}
