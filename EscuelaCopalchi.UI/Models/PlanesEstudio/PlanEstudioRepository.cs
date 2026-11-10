using System;
using System.Collections.Generic;
using System.Data;
namespace EscuelaCopalchi.UI.Models.PlanesEstudio
{
    public class PlanEstudioRepository
    {
        private readonly ConexionBD db = new ConexionBD();
        private const string Ok = "Se ha guardado correctamente";
        private static string S(object o) { return o == null || o == DBNull.Value ? "" : Convert.ToString(o); }
        private static int I(object o) { return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o); }
        private static string C(DataRow r, string col) { return r.Table.Columns.Contains(col) ? S(r[col]) : ""; }
        private static int N(DataRow r, string col) { return r.Table.Columns.Contains(col) ? I(r[col]) : 0; }
        private static PlanEstudio Map(DataRow r)
        {
            return new PlanEstudio {
                IdPlan=N(r,"id_plan"), IdDocente=N(r,"id_docente"), IdGrupo=N(r,"id_grupo"),
                Titulo=C(r,"titulo"), Mes=(byte)N(r,"mes"), Anio=N(r,"anio"),
                Nivel=C(r,"nivel"), Asignatura=C(r,"asignatura"), Estado=C(r,"estado"),
                Observaciones=C(r,"observaciones"), Archivo=C(r,"archivo"),
                NombreDocente=C(r,"nombre_docente"),
                FechaCarga=r.Table.Columns.Contains("fecha_carga") && r["fecha_carga"]!=DBNull.Value ? Convert.ToDateTime(r["fecha_carga"]) : DateTime.MinValue,
                FechaRevision=r.Table.Columns.Contains("fecha_revision") && r["fecha_revision"]!=DBNull.Value ? (DateTime?)Convert.ToDateTime(r["fecha_revision"]) : null,
                IdUsuarioRevision=r.Table.Columns.Contains("id_usuario_revision") && r["id_usuario_revision"]!=DBNull.Value ? (int?)Convert.ToInt32(r["id_usuario_revision"]) : null
            };
        }
        public List<PlanEstudio> ObtenerTodos()
        {
            var dt=db.ejecutarProcedimiento("SP_OBTENER_PLANES_ESTUDIO",new string[0],new string[0]);
            var lista=new List<PlanEstudio>(); foreach(DataRow r in dt.Rows) lista.Add(Map(r)); return lista;
        }
        public PlanEstudio ObtenerPorId(int id)
        {
            var dt=db.ejecutarProcedimiento("SP_OBTENER_PLAN_ESTUDIO",new[]{"@id_plan"},new[]{id.ToString()});
            return dt.Rows.Count==0 ? null : Map(dt.Rows[0]);
        }
        public DataTable ObtenerDocentes() { return db.ejecutarProcedimiento("SP_PLAN_LISTAR_DOCENTES",new string[0],new string[0]); }
        public DataTable ObtenerGrupos() { return db.ejecutarProcedimiento("SP_PLAN_LISTAR_GRUPOS",new string[0],new string[0]); }
        public string GuardarPlanEstudio(PlanEstudio p)
        {
            return db.ejecutarProcedimiento_Save("SP_GUARDAR_PLAN_ESTUDIO",
                new[]{"@id_docente","@id_grupo","@titulo","@mes","@anio","@nivel","@asignatura","@estado","@observaciones","@archivo"},
                new[]{p.IdDocente.ToString(),p.IdGrupo.ToString(),p.Titulo,p.Mes.ToString(),p.Anio.ToString(),p.Nivel??"",p.Asignatura??"",p.Estado??"Pendiente",p.Observaciones??"",p.Archivo??""});
        }
        public string ActualizarPlanEstudio(PlanEstudio p)
        {
            return db.ejecutarProcedimiento_Save("SP_ACTUALIZAR_PLAN_ESTUDIO",
                new[]{"@id_plan","@id_grupo","@titulo","@mes","@anio","@nivel","@asignatura","@archivo"},
                new[]{p.IdPlan.ToString(),p.IdGrupo.ToString(),p.Titulo,p.Mes.ToString(),p.Anio.ToString(),p.Nivel??"",p.Asignatura??"",p.Archivo??""});
        }
        public string RevisarPlanEstudio(PlanEstudio p)
        {
            return db.ejecutarProcedimiento_Save("SP_REVISAR_PLAN_ESTUDIO",
                new[]{"@id_plan","@estado","@observaciones","@id_usuario_revision"},
                new[]{p.IdPlan.ToString(),p.Estado,p.Observaciones??"",p.IdUsuarioRevision.Value.ToString()});
        }
    }
}
