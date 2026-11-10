using EscuelaCopalchi.UI.Models.PanelPrincipal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    [Authorize]
    public class PanelPrincipalController : Controller
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;

        // Ajustar únicamente los nombres de controladores que difieran en tu proyecto.
        private static readonly AccesoDef[] Catalogo =
        {
            new AccesoDef("Roles", "Administrar perfiles y permisos", "bi-shield-lock", "Roles", "Index", "GESTIONAR_ROLES"),
            new AccesoDef("Usuarios", "Administración del personal", "bi-people", "Usuarios", "Index", "GESTIONAR_USUARIOS"),
            new AccesoDef("Estudiantes", "Matrícula y expedientes", "bi-mortarboard", "Estudiantes", "Index", "GESTIONAR_ESTUDIANTES"),
            new AccesoDef("Gestión académica", "Grupos y asignaciones", "bi-diagram-3", "GestionAcademica", "Index", "GESTIONAR_ACADEMICA", "VER_MI_GRUPO"),
            new AccesoDef("Planes de estudio", "Planificación institucional", "bi-journal-text", "PlanesEstudio", "Index", "GESTIONAR_PLANES"),
            new AccesoDef("Gestión de notas", "Calificaciones y consultas", "bi-clipboard-data", "GestionNotas", "Index", "GESTIONAR_NOTAS", "REGISTRAR_NOTAS", "VER_TODAS_NOTAS"),
            new AccesoDef("Asistencia", "Registro y consulta", "bi-calendar-check", "Asistencia", "Index", "GESTIONAR_ASISTENCIA"),
            new AccesoDef("Material didáctico", "Repositorio institucional", "bi-folder2-open", "RepositorioMaterial", "Index", "VER_MATERIAL", "CARGAR_MATERIAL", "DESCARGAR_MATERIAL", "EDITAR_MATERIAL", "ELIMINAR_MATERIAL", "ADMINISTRAR_TODO_MATERIAL", "CARGAR_DOCUMENTO_INSTITUCIONAL"),
            new AccesoDef("Reportes", "Consultas y estadísticas", "bi-bar-chart", "Reporte", "Index", "VER_REPORTES"),
            new AccesoDef("Auditoría", "Actividad del sistema", "bi-journal-check", "AuditoriaSistema", "Index", "VER_AUDITORIA"),
            new AccesoDef("Inventario", "Control de existencias", "bi-box-seam", "InventarioLimpieza", "Index", "GESTIONAR_INVENTARIO")
        };

        public ActionResult Index()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Account");

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            var modelo = new PanelPrincipalViewModel();

            using (var cn = new SqlConnection(cadena))
            {
                cn.Open();
                int idRol;
                bool usuarioActivo;
                bool rolActivo;

                // Se obtiene el rol de la base de datos, no de una cookie o de un campo enviado por el navegador.
                using (var cmd = new SqlCommand(@"
                    SELECT u.id_rol, u.estado AS usuario_activo,
                           r.estado AS rol_activo, r.nombre AS rol,
                           u.nombre, u.apellido1
                    FROM dbo.USUARIO u
                    INNER JOIN dbo.ROL r ON r.id_rol = u.id_rol
                    WHERE u.id_usuario = @id", cn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                    using (var lector = cmd.ExecuteReader())
                    {
                        if (!lector.Read())
                            return CerrarSesion();

                        idRol = Convert.ToInt32(lector["id_rol"]);
                        usuarioActivo = Convert.ToBoolean(lector["usuario_activo"]);
                        rolActivo = Convert.ToBoolean(lector["rol_activo"]);
                        modelo.Rol = Convert.ToString(lector["rol"]);
                        modelo.Nombre = (Convert.ToString(lector["nombre"]) + " " + Convert.ToString(lector["apellido1"])).Trim();
                    }
                }

                if (!usuarioActivo || !rolActivo)
                    return CerrarSesion();

                // La clasificación administrativa controla la presentación, NO concede permisos.
                modelo.EsAdministrador = string.Equals(modelo.Rol, "Administrador", StringComparison.OrdinalIgnoreCase);

                var permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SqlCommand(@"
                    SELECT p.nombre
                    FROM dbo.ROL_PERMISO rp
                    INNER JOIN dbo.PERMISO p ON p.id_permiso = rp.id_permiso
                    WHERE rp.id_rol = @rol AND p.estado = 1", cn))
                {
                    cmd.Parameters.Add("@rol", SqlDbType.Int).Value = idRol;
                    using (var lector = cmd.ExecuteReader())
                    {
                        while (lector.Read())
                            permisos.Add(Convert.ToString(lector["nombre"]));
                    }
                }

                modelo.Accesos = Catalogo
                    .Where(a => a.Permisos.Any(permisos.Contains))
                    .Select(a => new AccesoPanel
                    {
                        Titulo = a.Titulo,
                        Descripcion = a.Descripcion,
                        Icono = a.Icono,
                        Controlador = a.Controlador,
                        Accion = a.Accion
                    }).ToList();

                // Los indicadores administrativos se muestran solo con permisos explícitos.
                modelo.MostrarIndicadoresAdministrativos =
                    modelo.EsAdministrador && permisos.Contains("GESTIONAR_USUARIOS") && permisos.Contains("GESTIONAR_ROLES");

                if (modelo.MostrarIndicadoresAdministrativos)
                {
                    using (var cmd = new SqlCommand(@"
                        SELECT
                          (SELECT COUNT(*) FROM dbo.USUARIO) AS total_usuarios,
                          (SELECT COUNT(*) FROM dbo.USUARIO WHERE estado = 1) AS usuarios_activos,
                          (SELECT COUNT(*) FROM dbo.ROL WHERE estado = 1) AS roles_activos,
                          (SELECT COUNT(*) FROM dbo.PERMISO WHERE estado = 1) AS permisos_activos", cn))
                    using (var lector = cmd.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            modelo.TotalUsuarios = Convert.ToInt32(lector["total_usuarios"]);
                            modelo.TotalUsuariosActivos = Convert.ToInt32(lector["usuarios_activos"]);
                            modelo.TotalRolesActivos = Convert.ToInt32(lector["roles_activos"]);
                            modelo.TotalPermisosActivos = Convert.ToInt32(lector["permisos_activos"]);
                        }
                    }
                }
            }

            return View(modelo);
        }

        private ActionResult CerrarSesion()
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        private sealed class AccesoDef
        {
            public readonly string Titulo, Descripcion, Icono, Controlador, Accion;
            public readonly string[] Permisos;
            public AccesoDef(string titulo, string descripcion, string icono, string controlador, string accion, params string[] permisos)
            {
                Titulo = titulo; Descripcion = descripcion; Icono = icono;
                Controlador = controlador; Accion = accion; Permisos = permisos;
            }
        }
    }
}
