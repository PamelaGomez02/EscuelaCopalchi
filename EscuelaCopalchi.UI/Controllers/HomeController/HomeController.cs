using EscuelaCopalchi.UI.Models.Home;
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
    public class HomeController : Controller
    {
        private readonly string cs = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["IdUsuario"] == null) { System.Web.Security.FormsAuthentication.SignOut(); return RedirectToAction("Login", "Account"); }
            int id = Convert.ToInt32(Session["IdUsuario"]);
            var vm = new HomeDashboardViewModel();
            using (var cn = new SqlConnection(cs))
            {
                cn.Open();
                // No confiar en el rol guardado en sesión: verificar rol y estado actual.
                using (var cmd = new SqlCommand(@"SELECT u.nombre + N' ' + u.apellido1, r.nombre
                    FROM dbo.USUARIO u JOIN dbo.ROL r ON r.id_rol=u.id_rol
                    WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1", cn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) { System.Web.Security.FormsAuthentication.SignOut(); Session.Clear(); return RedirectToAction("Login", "Account"); }
                        vm.Nombre = dr.GetString(0); vm.Rol = dr.GetString(1);
                    }
                }
                vm.EsAdministrador = string.Equals(vm.Rol, "Administrador", StringComparison.OrdinalIgnoreCase);
                using (var cmd = new SqlCommand(@"SELECT p.nombre FROM dbo.USUARIO u
                    JOIN dbo.ROL r ON r.id_rol=u.id_rol
                    JOIN dbo.ROL_PERMISO rp ON rp.id_rol=r.id_rol
                    JOIN dbo.PERMISO p ON p.id_permiso=rp.id_permiso
                    WHERE u.id_usuario=@id AND u.estado=1 AND r.estado=1 AND p.estado=1", cn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    using (var dr = cmd.ExecuteReader()) while (dr.Read()) vm.Permisos.Add(dr.GetString(0));
                }
                vm.Modulos = CrearModulos().Where(x => x.Permisos.Any(vm.Permisos.Contains)).ToList();
                int? docente = null;
                using (var cmd = new SqlCommand("SELECT TOP (1) id_docente FROM dbo.DOCENTE WHERE id_usuario=@id AND estado=1", cn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    var result = cmd.ExecuteScalar(); if (result != null && result != DBNull.Value) docente = Convert.ToInt32(result);
                }
                // Solo el administrador ve totales institucionales; otros usuarios ven sus grupos
                // cuando son docentes. Los roles no docentes sin alcance asignado no ven cifras globales.
                bool global = vm.EsAdministrador;
                if (global || docente.HasValue)
                {
                    if (global || vm.Puede("GESTIONAR_ESTUDIANTES", "VER_MI_GRUPO", "GESTIONAR_ACADEMICA"))
                    {
                        vm.Estudiantes = Entero(cn, global ?
                            "SELECT COUNT(*) FROM dbo.ESTUDIANTE WHERE estado=1" :
                            @"SELECT COUNT(DISTINCT m.id_estudiante) FROM dbo.MATRICULA m
                              JOIN dbo.GRUPO g ON g.id_grupo=m.id_grupo
                              JOIN dbo.ESTUDIANTE e ON e.id_estudiante=m.id_estudiante
                              WHERE m.estado=N'Activa' AND e.estado=1 AND g.estado=1 AND g.id_docente=@doc", docente);
                    }
                    if (global || vm.Puede("GESTIONAR_ACADEMICA", "VER_MI_GRUPO"))
                        vm.Grupos = Entero(cn, "SELECT COUNT(*) FROM dbo.GRUPO WHERE estado=1" + (global ? "" : " AND id_docente=@doc"), docente);
                    if (global || vm.Puede("GESTIONAR_ASISTENCIA"))
                    {
                        vm.Asistencia = Decimal(cn, @"SELECT CAST(100.0 * SUM(CASE WHEN a.estado IN (N'Presente',N'Tardanza') THEN 1 ELSE 0 END)
                            / NULLIF(COUNT(*),0) AS DECIMAL(5,1)) FROM dbo.ASISTENCIA a
                            JOIN dbo.GRUPO g ON g.id_grupo=a.id_grupo WHERE a.fecha >= DATEADD(DAY,-30,CAST(GETDATE() AS DATE))" +
                            (global ? "" : " AND g.id_docente=@doc"), docente);
                        vm.AsistenciaMensual = Meses(cn, global, docente);
                    }
                    if (global || vm.Puede("GESTIONAR_NOTAS", "REGISTRAR_NOTAS", "VER_TODAS_NOTAS"))
                    {
                        vm.PromedioNotas = Decimal(cn, @"SELECT CAST(AVG(100.0*c.nota/NULLIF(ev.nota_maxima,0)) AS DECIMAL(5,1))
                          FROM dbo.CALIFICACION c JOIN dbo.EVALUACION ev ON ev.id_evaluacion=c.id_evaluacion
                          JOIN dbo.GRUPO g ON g.id_grupo=ev.id_grupo WHERE ev.estado=1 AND g.estado=1" +
                          (global ? "" : " AND g.id_docente=@doc"), docente);
                        vm.NotasPorGrupo = Notas(cn, global, docente);
                    }
                    if (global || vm.Puede("GESTIONAR_PLANES"))
                        vm.PlanesPendientes = Entero(cn, @"SELECT COUNT(*) FROM dbo.PLAN_ESTUDIO p
                            JOIN dbo.GRUPO g ON g.id_grupo=p.id_grupo WHERE p.estado=N'Pendiente'" +
                            (global ? "" : " AND g.id_docente=@doc"), docente);
                    if (global || vm.Puede("VER_REPORTES", "GESTIONAR_ACADEMICA"))
                        vm.Alertas = Entero(cn, @"SELECT COUNT(*) FROM dbo.ALERTA_ACADEMICA a
                            WHERE a.atendida=0" + (global ? "" :
                            @" AND EXISTS (SELECT 1 FROM dbo.MATRICULA m JOIN dbo.GRUPO g ON g.id_grupo=m.id_grupo
                                WHERE m.id_estudiante=a.id_estudiante AND m.estado=N'Activa' AND g.id_docente=@doc)"), docente);
                }
                if (vm.Puede("GESTIONAR_USUARIOS"))
                    vm.Usuarios = Entero(cn, "SELECT COUNT(*) FROM dbo.USUARIO WHERE estado=1", null);
                if (vm.Puede("GESTIONAR_INVENTARIO"))
                    vm.StockBajo = Entero(cn, "SELECT COUNT(*) FROM dbo.INVENTARIO WHERE estado=1 AND stock_actual<stock_minimo", null);
                // Las notificaciones son siempre del usuario autenticado.
                vm.Notificaciones = Entero(cn, "SELECT COUNT(*) FROM dbo.NOTIFICACION WHERE id_usuario=@id AND leida=0", null, id);
                if (vm.Puede("VER_AUDITORIA")) vm.Actividad = Actividades(cn, id, global);
            }

            vm.Modulos.Add(new ModuloHome
            {
                Titulo = "PRUEBA REPORTES 2026",
                Descripcion = "Comprobación del Home",
                Icono = "bi-file-earmark-bar-graph",
                Controlador = "Reportes",
                Accion = "Index",
                Permisos = new[] { "VER_REPORTES_ACADEMICOS" }
            });
            return View(vm.EsAdministrador ? "IndexAdmin" : "IndexUsuario", vm);
        }

        private static int Entero(SqlConnection cn, string sql, int? docente, int? id = null)
        {
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (sql.Contains("@doc")) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente ?? (object)DBNull.Value;
                if (sql.Contains("@id")) cmd.Parameters.Add("@id", SqlDbType.Int).Value = id ?? (object)DBNull.Value;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        private static decimal? Decimal(SqlConnection cn, string sql, int? docente)
        {
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (sql.Contains("@doc")) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente ?? (object)DBNull.Value;
                var v = cmd.ExecuteScalar(); return v == null || v == DBNull.Value ? (decimal?)null : Convert.ToDecimal(v);
            }
        }
        private static List<SerieMes> Meses(SqlConnection cn, bool global, int? docente)
        {
            var list = new List<SerieMes>();
            string sql = @"SELECT YEAR(a.fecha) AS anio, MONTH(a.fecha) AS mes,
                CAST(100.0*SUM(CASE WHEN a.estado IN (N'Presente',N'Tardanza') THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS DECIMAL(5,1)) AS porcentaje
                FROM dbo.ASISTENCIA a JOIN dbo.GRUPO g ON g.id_grupo=a.id_grupo
                WHERE a.fecha >= DATEADD(MONTH,-5,DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1))" +
                (global ? "" : " AND g.id_docente=@doc") + " GROUP BY YEAR(a.fecha),MONTH(a.fecha) ORDER BY anio,mes";
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (!global) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente.Value;
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    list.Add(new SerieMes { Mes = new DateTime(r.GetInt32(0), r.GetInt32(1), 1).ToString("MMM yyyy"), Valor = r.GetDecimal(2) });
            }
            return list;
        }
        private static List<SerieGrupo> Notas(SqlConnection cn, bool global, int? docente)
        {
            var list = new List<SerieGrupo>();
            string sql = @"SELECT TOP (8) g.nombre,
                CAST(AVG(100.0*c.nota/NULLIF(ev.nota_maxima,0)) AS DECIMAL(5,1)) AS promedio
                FROM dbo.CALIFICACION c JOIN dbo.EVALUACION ev ON ev.id_evaluacion=c.id_evaluacion
                JOIN dbo.GRUPO g ON g.id_grupo=ev.id_grupo
                WHERE ev.estado=1 AND g.estado=1" + (global ? "" : " AND g.id_docente=@doc") +
                " GROUP BY g.id_grupo,g.nombre ORDER BY g.nombre";
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (!global) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente.Value;
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    list.Add(new SerieGrupo { Grupo = r.GetString(0), Valor = r.GetDecimal(1) });
            }
            return list;
        }
        private static List<ActividadHome> Actividades(SqlConnection cn, int id, bool global)
        {
            var list = new List<ActividadHome>();
            string sql = "SELECT TOP (5) accion, ISNULL(descripcion,N''), fecha FROM dbo.AUDITORIA " +
                (global ? "" : "WHERE id_usuario=@id ") + "ORDER BY fecha DESC";
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (!global) cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(new ActividadHome
                { Accion = r.GetString(0), Descripcion = r.GetString(1), Fecha = r.GetDateTime(2) });
            }
            return list;
        }
        private static List<ModuloHome> CrearModulos() => new List<ModuloHome>
        {
            new ModuloHome { Titulo="Usuarios", Descripcion="Administración de cuentas", Icono="bi-people", Controlador="Usuarios", Permisos=new[]{"GESTIONAR_USUARIOS"}},
            new ModuloHome { Titulo="Roles y permisos", Descripcion="Seguridad y accesos", Icono="bi-shield-lock", Controlador="Roles", Permisos=new[]{"GESTIONAR_ROLES"}},
            new ModuloHome { Titulo="Estudiantes", Descripcion="Matrícula y expedientes", Icono="bi-mortarboard", Controlador="Estudiantes", Permisos=new[]{"GESTIONAR_ESTUDIANTES"}},
            new ModuloHome { Titulo="Gestión académica", Descripcion="Grupos, aulas y docentes", Icono="bi-diagram-3", Controlador="GestionAcademica", Permisos=new[]{"GESTIONAR_ACADEMICA","VER_MI_GRUPO"}},
            new ModuloHome { Titulo="Asistencia", Descripcion="Control diario", Icono="bi-calendar-check", Controlador="Asistencia", Permisos=new[]{"GESTIONAR_ASISTENCIA"}},
            new ModuloHome { Titulo="Gestión de notas", Descripcion="Evaluaciones y calificaciones", Icono="bi-clipboard-data", Controlador="GestionNotas", Permisos=new[]{"GESTIONAR_NOTAS","REGISTRAR_NOTAS","VER_TODAS_NOTAS"}},
            new ModuloHome { Titulo="Planes de estudio", Descripcion="Planeamiento y aprobación", Icono="bi-journal-text", Controlador="PlanesEstudio", Permisos=new[]{"GESTIONAR_PLANES"}},
            new ModuloHome { Titulo="Material didáctico", Descripcion="Repositorio educativo", Icono="bi-folder2-open", Controlador="RepositorioMaterial", Permisos=new[]{"GESTIONAR_MATERIAL","VER_MATERIAL","CARGAR_MATERIAL","DESCARGAR_MATERIAL","EDITAR_MATERIAL","ADMINISTRAR_TODO_MATERIAL","CARGAR_DOCUMENTO_INSTITUCIONAL"}},
            new ModuloHome { Titulo="Monitoreo académico", Descripcion="Seguimiento y alertas académicas", Icono="bi-graph-up", Controlador="Monitoreo", Permisos=new[]{"VER_REPORTES"}},
            new ModuloHome { Titulo="Reportes académicos", Descripcion="Estadísticas, fichas y exportaciones PDF/Excel", Icono="bi-file-earmark-bar-graph", Controlador="Reportes", Permisos=new[]{"VER_REPORTES_ACADEMICOS"}},
            new ModuloHome { Titulo="Auditoría", Descripcion="Historial de movimientos", Icono="bi-journal-check", Controlador="AuditoriaSistema", Permisos=new[]{"VER_AUDITORIA"}},
            new ModuloHome { Titulo="Inventario de limpieza", Descripcion="Existencias y suministros", Icono="bi-bucket", Controlador="InventarioLimpieza", Permisos=new[]{"GESTIONAR_INVENTARIO"}},
            new ModuloHome { Titulo="Inventario de cocina", Descripcion="Insumos y existencias", Icono="bi-basket", Controlador="InventarioCocina", Permisos=new[]{"GESTIONAR_INVENTARIO"}}
        };
        [AllowAnonymous] public ActionResult About() => View();
        [AllowAnonymous] public ActionResult Contact() => View();

        [HttpGet]
        public ActionResult AccesoDenegado()
        {
            return View();
        }

    }
}


