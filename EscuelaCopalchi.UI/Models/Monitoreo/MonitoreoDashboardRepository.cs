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

        public MonitoreoDashboardVM Obtener(int? anio, int? trimestre, int? grupo, int idUsuario)
        {
            var vm = new MonitoreoDashboardVM();
            using (var cn = new SqlConnection(cs))
            {
                cn.Open();
                // Acceso institucional para cualquier usuario autenticado.
                // No se consultan permisos del modulo.
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
                vm.RankingGrupos = vm.Estudiantes.GroupBy(x => new { x.IdGrupo, x.Grupo })
                    .Select(g => new MonitoreoGrupoResumen {
                        IdGrupo = g.Key.IdGrupo, Grupo = g.Key.Grupo,
                        Promedio = Math.Round(g.Average(x => x.Promedio), 2),
                        Estudiantes = g.Select(x => x.IdEstudiante).Distinct().Count()
                    }).OrderByDescending(x => x.Promedio).ThenBy(x => x.Grupo).ToList();
            }
            return vm;
        }
    }
}
