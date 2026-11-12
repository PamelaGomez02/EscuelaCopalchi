using EscuelaCopalchi.UI.Models.MonitoreoNuevo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.MonitoreoNuevo
{
    public class MonitoreoDashboardRepository
    {
        private readonly string cs = ConfigurationManager.ConnectionStrings["AulaVirtualCopalchi"].ConnectionString;

        public MonitoreoDashboardVM Obtener(int? anio, int? trimestre, int? grupo, int idUsuario, string buscar = "", string estado = "", int pagina = 1)
        {
            var vm = new MonitoreoDashboardVM();
            using (var cn = new SqlConnection(cs))
            {
                cn.Open();
                // El acceso al módulo requiere sesión; los datos institucionales se
                // muestran conforme a la decisión del proyecto de no usar permisos específicos.
                bool institucional = true;
                int? docente = null;
                using (var cmd = new SqlCommand("SELECT TOP (1) id_docente FROM dbo.DOCENTE WHERE id_usuario=@u AND estado=1", cn))
                {
                    cmd.Parameters.Add("@u", SqlDbType.Int).Value = idUsuario;
                    var value = cmd.ExecuteScalar();
                    if (value != null && value != DBNull.Value) docente = Convert.ToInt32(value);
                }
                if (!institucional && !docente.HasValue)
                    throw new UnauthorizedAccessException("No tiene alcance académico asignado para consultar estudiantes.");

                string alcance = institucional ? "" : " AND g.id_docente=@doc";
                using (var cmd = new SqlCommand("SELECT DISTINCT g.anio FROM dbo.GRUPO g WHERE g.estado=1" + alcance + " ORDER BY g.anio DESC", cn))
                {
                    if (!institucional) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente.Value;
                    using (var dr = cmd.ExecuteReader()) while (dr.Read()) vm.Anios.Add(dr.GetInt32(0));
                }
                vm.Anio = anio.HasValue && vm.Anios.Contains(anio.Value) ? anio.Value : vm.Anios.FirstOrDefault();
                if (vm.Anio == 0) return vm;
                vm.Trimestre = trimestre.HasValue && trimestre.Value >= 1 && trimestre.Value <= 3 ? trimestre.Value : 0;
                using (var cmd = new SqlCommand("SELECT g.id_grupo,g.nombre FROM dbo.GRUPO g WHERE g.estado=1 AND g.anio=@anio" + alcance + " ORDER BY g.nombre", cn))
                {
                    cmd.Parameters.Add("@anio", SqlDbType.Int).Value = vm.Anio;
                    if (!institucional) cmd.Parameters.Add("@doc", SqlDbType.Int).Value = docente.Value;
                    using (var dr = cmd.ExecuteReader()) while (dr.Read()) vm.Grupos.Add(new MonitoreoGrupoItem { IdGrupo = dr.GetInt32(0), Nombre = dr.GetString(1) });
                }
                vm.IdGrupo = grupo.HasValue && vm.Grupos.Any(g => g.IdGrupo == grupo.Value) ? grupo.Value : 0;

                // VW_MON_RENDIMIENTO contiene una fila por estudiante, materia y trimestre.
                // Primero se promedian las materias dentro de cada trimestre; después se
                // promedian los trimestres disponibles por estudiante. No se pondera por
                // número de evaluaciones, ni se interpreta la ausencia de notas como cero.
                const string sql = @"
WITH Materia AS (
 SELECT v.id_estudiante,v.estudiante,v.id_grupo,v.grupo,v.trimestre,v.asignatura,
        AVG(CAST(v.promedio AS DECIMAL(18,4))) AS promedio_materia
 FROM dbo.VW_MON_RENDIMIENTO v
 JOIN dbo.GRUPO g ON g.id_grupo=v.id_grupo
 WHERE v.anio=@anio AND (@grupo=0 OR v.id_grupo=@grupo)
   AND (@trimestre=0 OR v.trimestre=@trimestre)
   AND (@institucional=1 OR g.id_docente=@doc)
 GROUP BY v.id_estudiante,v.estudiante,v.id_grupo,v.grupo,v.trimestre,v.asignatura
), Trimestre AS (
 SELECT id_estudiante,estudiante,id_grupo,grupo,trimestre,
        AVG(promedio_materia) AS promedio_trimestre,
        COUNT(*) AS materias
 FROM Materia
 GROUP BY id_estudiante,estudiante,id_grupo,grupo,trimestre
)
SELECT id_estudiante,estudiante,id_grupo,grupo,
       CAST(AVG(promedio_trimestre) AS DECIMAL(7,2)) AS promedio,
       MAX(materias) AS materias
FROM Trimestre
GROUP BY id_estudiante,estudiante,id_grupo,grupo
ORDER BY promedio DESC, estudiante;";
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@anio", SqlDbType.Int).Value = vm.Anio;
                    cmd.Parameters.Add("@grupo", SqlDbType.Int).Value = vm.IdGrupo;
                    cmd.Parameters.Add("@trimestre", SqlDbType.Int).Value = vm.Trimestre;
                    cmd.Parameters.Add("@institucional", SqlDbType.Bit).Value = institucional;
                    cmd.Parameters.Add("@doc", SqlDbType.Int).Value = (object)docente ?? DBNull.Value;
                    using (var dr = cmd.ExecuteReader()) while (dr.Read())
                        vm.Estudiantes.Add(new MonitoreoEstudianteItem {
                            IdEstudiante = dr.GetInt32(0), Nombre = dr.GetString(1),
                            IdGrupo = dr.GetInt32(2), Grupo = dr.GetString(3),
                            Promedio = dr.GetDecimal(4), Materias = dr.GetInt32(5)
                        });
                }
                vm.TotalEvaluados = vm.Estudiantes.Select(x => x.IdEstudiante).Distinct().Count();
                vm.PromedioGeneral = vm.Estudiantes.Count == 0 ? (decimal?)null :
                    Math.Round(vm.Estudiantes.Average(x => x.Promedio), 2);
                vm.Mejores = vm.Estudiantes.OrderByDescending(x => x.Promedio).ThenBy(x => x.Nombre).Take(10).ToList();
                vm.Menores = vm.Estudiantes.OrderBy(x => x.Promedio).ThenBy(x => x.Nombre).Take(10).ToList();
                vm.EnRiesgo = vm.Estudiantes.Count(x => x.Promedio < 65);
                vm.Criticos = vm.Estudiantes.Count(x => x.Promedio < 50);
                vm.Buscar = (buscar ?? "").Trim();
                vm.Estado = estado ?? "";
                var filtrados = vm.Estudiantes.AsEnumerable();
                if (vm.Buscar.Length > 0) filtrados = filtrados.Where(x => x.Nombre.IndexOf(vm.Buscar, StringComparison.OrdinalIgnoreCase) >= 0);
                if (vm.Estado == "Crítico" || vm.Estado == "En riesgo" || vm.Estado == "Satisfactorio") filtrados = filtrados.Where(x => x.Estado == vm.Estado);
                var lista = filtrados.OrderBy(x => x.Promedio).ThenBy(x => x.Nombre).ToList();
                vm.TotalPaginas = Math.Max(1, (int)Math.Ceiling(lista.Count / 10.0));
                vm.Pagina = Math.Max(1, Math.Min(pagina, vm.TotalPaginas));
                vm.Listado = lista.Skip((vm.Pagina - 1) * 10).Take(10).ToList();
                vm.RankingGrupos = vm.Estudiantes.GroupBy(x => new { x.IdGrupo, x.Grupo })
                    .Select(g => new MonitoreoGrupoResumen {
                        IdGrupo = g.Key.IdGrupo, Grupo = g.Key.Grupo,
                        Promedio = Math.Round(g.Average(x => x.Promedio), 2),
                        Estudiantes = g.Select(x => x.IdEstudiante).Distinct().Count()
                    }).OrderByDescending(x => x.Promedio).ThenBy(x => x.Grupo).ToList();
                const string materiasSql = @"SELECT asignatura,CAST(AVG(promedio) AS DECIMAL(7,2)),COUNT(DISTINCT id_estudiante)
                  FROM dbo.VW_MON_RENDIMIENTO WHERE anio=@a AND (@g=0 OR id_grupo=@g) AND (@t=0 OR trimestre=@t)
                  GROUP BY asignatura ORDER BY AVG(promedio) DESC";
                using (var cmd = new SqlCommand(materiasSql, cn)) {
                    cmd.Parameters.Add("@a", SqlDbType.Int).Value=vm.Anio;
                    cmd.Parameters.Add("@g", SqlDbType.Int).Value=vm.IdGrupo;
                    cmd.Parameters.Add("@t", SqlDbType.Int).Value=vm.Trimestre;
                    using (var r=cmd.ExecuteReader()) while(r.Read()) vm.RankingMaterias.Add(new MonitoreoMateriaResumen { Materia=r.GetString(0),Promedio=r.GetDecimal(1),Estudiantes=r.GetInt32(2) });
                }
                const string evolucionSql = @"WITH x AS (SELECT id_estudiante,id_grupo,trimestre,asignatura,AVG(promedio) p FROM dbo.VW_MON_RENDIMIENTO
                  WHERE anio=@a AND (@g=0 OR id_grupo=@g) GROUP BY id_estudiante,id_grupo,trimestre,asignatura),
                  y AS (SELECT id_estudiante,id_grupo,trimestre,AVG(p) p FROM x GROUP BY id_estudiante,id_grupo,trimestre)
                  SELECT trimestre,CAST(AVG(p) AS DECIMAL(7,2)),COUNT(*) FROM y GROUP BY trimestre ORDER BY trimestre";
                using (var cmd=new SqlCommand(evolucionSql,cn)) {
                    cmd.Parameters.Add("@a", SqlDbType.Int).Value=vm.Anio;
                    cmd.Parameters.Add("@g", SqlDbType.Int).Value=vm.IdGrupo;
                    using(var r=cmd.ExecuteReader()) while(r.Read()) vm.Evolucion.Add(new MonitoreoTrimestreResumen {Trimestre=Convert.ToInt32(r.GetValue(0)),Promedio=r.GetDecimal(1),Estudiantes=r.GetInt32(2)});
                }
                using (var cmd=new SqlCommand(@"SELECT COUNT(*) FROM dbo.ALERTA_ACADEMICA a INNER JOIN dbo.ESTUDIANTE e ON e.id_estudiante=a.id_estudiante WHERE a.atendida=0 AND e.estado=1 AND (@g=0 OR a.id_grupo=@g) AND EXISTS (SELECT 1 FROM dbo.MATRICULA m INNER JOIN dbo.GRUPO gr ON gr.id_grupo=m.id_grupo WHERE m.id_estudiante=a.id_estudiante AND m.estado=N'Activa' AND gr.anio=@a AND (@g=0 OR gr.id_grupo=@g))",cn)) {
                    cmd.Parameters.Add("@a",SqlDbType.Int).Value=vm.Anio;cmd.Parameters.Add("@g",SqlDbType.Int).Value=vm.IdGrupo;
                    vm.AlertasPendientes=Convert.ToInt32(cmd.ExecuteScalar());
                }
                using (var cmd=new SqlCommand(@"SELECT CAST(100.0*SUM(CASE WHEN a.estado IN (N'Presente',N'Tardanza') THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS DECIMAL(7,2)) FROM dbo.ASISTENCIA a JOIN dbo.GRUPO g ON g.id_grupo=a.id_grupo JOIN dbo.ESTUDIANTE e ON e.id_estudiante=a.id_estudiante WHERE g.anio=@a AND e.estado=1 AND (@g=0 OR a.id_grupo=@g)",cn)) {
                    cmd.Parameters.Add("@a",SqlDbType.Int).Value=vm.Anio;cmd.Parameters.Add("@g",SqlDbType.Int).Value=vm.IdGrupo;var v=cmd.ExecuteScalar();vm.Asistencia=v==null||v==DBNull.Value?(decimal?)null:Convert.ToDecimal(v);
                }
                using (var cmd=new SqlCommand(@"SELECT COUNT(*) FROM dbo.MONITOREO_SEGUIMIENTO s JOIN dbo.GRUPO g ON g.id_grupo=s.id_grupo WHERE s.anio=@a AND (@g=0 OR s.id_grupo=@g) AND s.estado<>N'Resuelto'",cn)) {
                    cmd.Parameters.Add("@a",SqlDbType.Int).Value=vm.Anio;cmd.Parameters.Add("@g",SqlDbType.Int).Value=vm.IdGrupo;
                    vm.SeguimientosAbiertos=Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return vm;
        }
        public MonitoreoDetalleVM Detalle(int estudiante, int grupo, int anio)
        {
            var vm = new MonitoreoDetalleVM { IdEstudiante=estudiante,IdGrupo=grupo,Anio=anio };
            using(var cn=new SqlConnection(cs)) {cn.Open();
                using(var cmd=new SqlCommand(@"SELECT TOP 1 v.estudiante,v.grupo FROM dbo.VW_MON_RENDIMIENTO v WHERE v.id_estudiante=@e AND v.id_grupo=@g AND v.anio=@a",cn)) {
                    cmd.Parameters.Add("@e",SqlDbType.Int).Value=estudiante;cmd.Parameters.Add("@g",SqlDbType.Int).Value=grupo;cmd.Parameters.Add("@a",SqlDbType.Int).Value=anio;
                    using(var r=cmd.ExecuteReader()) {if(!r.Read()) return null;vm.Nombre=r.GetString(0);vm.Grupo=r.GetString(1);}
                }
                using(var cmd=new SqlCommand(@"SELECT trimestre,asignatura,promedio FROM dbo.VW_MON_RENDIMIENTO WHERE id_estudiante=@e AND id_grupo=@g AND anio=@a ORDER BY trimestre,asignatura",cn)) {
                    cmd.Parameters.Add("@e",SqlDbType.Int).Value=estudiante;cmd.Parameters.Add("@g",SqlDbType.Int).Value=grupo;cmd.Parameters.Add("@a",SqlDbType.Int).Value=anio;
                    using(var r=cmd.ExecuteReader()) while(r.Read()) vm.Notas.Add(new MonitoreoNotaDetalle {Trimestre=Convert.ToInt32(r.GetValue(0)),Materia=r.GetString(1),Promedio=r.GetDecimal(2)});
                }
                var trim=vm.Notas.GroupBy(x=>x.Trimestre).Select(x=>x.Average(y=>y.Promedio)).ToList();
                if(trim.Count>0) vm.Promedio=Math.Round(trim.Average(),2);
                using(var cmd=new SqlCommand(@"SELECT CAST(100.0*SUM(CASE WHEN a.estado IN (N'Presente',N'Tardanza') THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0) AS DECIMAL(7,2)) FROM dbo.ASISTENCIA a WHERE a.id_estudiante=@e AND a.id_grupo=@g",cn)) {
                    cmd.Parameters.Add("@e",SqlDbType.Int).Value=estudiante;cmd.Parameters.Add("@g",SqlDbType.Int).Value=grupo;
                    var v=cmd.ExecuteScalar();vm.Asistencia=v==null||v==DBNull.Value?(decimal?)null:Convert.ToDecimal(v);
                }
                vm.Seguimientos=ListarSeguimientos(cn,anio,grupo,estudiante);
            } return vm;
        }
        private static List<MonitoreoSeguimientoItem> ListarSeguimientos(SqlConnection cn,int anio,int grupo,int estudiante)
        {
            var list=new List<MonitoreoSeguimientoItem>();
            using(var cmd=new SqlCommand(@"SELECT s.id_seguimiento,s.id_estudiante,s.id_grupo,s.anio,s.trimestre,e.nombre+N' '+e.apellido1,g.nombre,s.nivel_riesgo,s.estado,s.observacion,ISNULL(s.accion_realizada,N''),s.fecha_registro FROM dbo.MONITOREO_SEGUIMIENTO s JOIN dbo.ESTUDIANTE e ON e.id_estudiante=s.id_estudiante JOIN dbo.GRUPO g ON g.id_grupo=s.id_grupo WHERE s.anio=@a AND (@g=0 OR s.id_grupo=@g) AND (@e=0 OR s.id_estudiante=@e) ORDER BY s.fecha_registro DESC",cn)) {
                cmd.Parameters.Add("@a",SqlDbType.Int).Value=anio;cmd.Parameters.Add("@g",SqlDbType.Int).Value=grupo;cmd.Parameters.Add("@e",SqlDbType.Int).Value=estudiante;
                using(var r=cmd.ExecuteReader()) while(r.Read()) list.Add(new MonitoreoSeguimientoItem {IdSeguimiento=r.GetInt32(0),IdEstudiante=r.GetInt32(1),IdGrupo=r.GetInt32(2),Anio=r.GetInt32(3),Trimestre=r.IsDBNull(4)?(byte?)null:r.GetByte(4),Estudiante=r.GetString(5),Grupo=r.GetString(6),NivelRiesgo=r.GetString(7),Estado=r.GetString(8),Observacion=r.GetString(9),AccionRealizada=r.GetString(10),FechaRegistro=r.GetDateTime(11)});
            } return list;
        }
        public List<MonitoreoSeguimientoItem> Seguimientos(int anio,int grupo) {using(var cn=new SqlConnection(cs)){cn.Open();return ListarSeguimientos(cn,anio,grupo,0);}}
        public void GuardarSeguimiento(int estudiante,int grupo,int anio,byte? trimestre,string riesgo,string estado,string observacion,string accion,int usuario)
        {
            if(string.IsNullOrWhiteSpace(observacion)) throw new ArgumentException("La observación es obligatoria.");
            using(var cn=new SqlConnection(cs)){cn.Open();
                using(var cmd=new SqlCommand(@"INSERT INTO dbo.MONITOREO_SEGUIMIENTO(id_estudiante,id_grupo,anio,trimestre,nivel_riesgo,estado,observacion,accion_realizada,id_usuario_responsable) SELECT @e,@g,@a,@t,@r,@s,@o,@ac,@u WHERE EXISTS(SELECT 1 FROM dbo.ESTUDIANTE e JOIN dbo.MATRICULA m ON m.id_estudiante=e.id_estudiante JOIN dbo.GRUPO g ON g.id_grupo=m.id_grupo WHERE e.id_estudiante=@e AND e.estado=1 AND m.estado=N'Activa' AND g.id_grupo=@g AND g.anio=@a AND g.estado=1); SELECT @@ROWCOUNT",cn)) {
                    cmd.Parameters.Add("@e",SqlDbType.Int).Value=estudiante;cmd.Parameters.Add("@g",SqlDbType.Int).Value=grupo;cmd.Parameters.Add("@a",SqlDbType.Int).Value=anio;
                    cmd.Parameters.Add("@t",SqlDbType.TinyInt).Value=(object)trimestre??DBNull.Value;cmd.Parameters.Add("@r",SqlDbType.NVarChar,30).Value=riesgo;cmd.Parameters.Add("@s",SqlDbType.NVarChar,30).Value=estado;
                    cmd.Parameters.Add("@o",SqlDbType.NVarChar,2000).Value=observacion.Trim();cmd.Parameters.Add("@ac",SqlDbType.NVarChar,2000).Value=(object)accion??DBNull.Value;cmd.Parameters.Add("@u",SqlDbType.Int).Value=usuario;
                    if(Convert.ToInt32(cmd.ExecuteScalar())!=1) throw new ArgumentException("Estudiante o matrícula no válida para el grupo y año indicados.");
                }
            }
        }
        public void ActualizarEstado(int id,string estado) {using(var cn=new SqlConnection(cs)){cn.Open();using(var cmd=new SqlCommand("UPDATE dbo.MONITOREO_SEGUIMIENTO SET estado=@s,fecha_actualizacion=SYSDATETIME() WHERE id_seguimiento=@id",cn)){cmd.Parameters.Add("@s",SqlDbType.NVarChar,30).Value=estado;cmd.Parameters.Add("@id",SqlDbType.Int).Value=id;if(cmd.ExecuteNonQuery()==0)throw new ArgumentException("Seguimiento no encontrado.");}}}

    }
}
