using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    public class ReportesRepository
    {
        private readonly ConexionBD db = new ConexionBD();

        private static string S(int v) { return v.ToString(CultureInfo.InvariantCulture); }
        private static string Txt(DataRow r, string c) { return r[c] == DBNull.Value ? "" : Convert.ToString(r[c]); }
        private static int Int(DataRow r, string c) { return r[c] == DBNull.Value ? 0 : Convert.ToInt32(r[c]); }
        private static decimal? Dec(DataRow r, string c) { return r[c] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r[c]); }

        // escapa los comodines de LIKE para que el texto se busque tal cual
        private static string Texto(string t)
        {
            return (t ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }

        public List<EstudianteRendimiento> Rendimiento(int idDocente, int idGrupo, string busqueda = "")
        {
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Rendimiento",
                new[] { "@id_docente", "@id_grupo", "@busqueda" }, new[] { S(idDocente), S(idGrupo), Texto(busqueda) });

            var lista = new List<EstudianteRendimiento>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new EstudianteRendimiento
                {
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = Txt(r, "grupo"),
                    IdEstudiante = Int(r, "id_estudiante"),
                    Identificacion = Txt(r, "identificacion"),
                    Estudiante = Txt(r, "estudiante"),
                    Promedio = Dec(r, "promedio"),
                    Periodos = Int(r, "periodos"),
                    Clases = Int(r, "clases"),
                    Presentes = Int(r, "presentes"),
                    Tardanzas = Int(r, "tardanzas"),
                    Justificados = Int(r, "justificados"),
                    Ausentes = Int(r, "ausentes"),
                    PctAsistencia = Dec(r, "pct_asistencia"),
                    BajoRendimiento = Int(r, "bajo_rendimiento") == 1,
                    BajaAsistencia = Int(r, "baja_asistencia") == 1
                });
            }
            return lista;
        }

        public List<AsignaturaPromedio> PorAsignatura(int idDocente, int idGrupo, string busqueda = "")
        {
            DataTable dt = db.ejecutarProcedimiento("SP_RP_RendimientoAsignatura",
                new[] { "@id_docente", "@id_grupo", "@busqueda" }, new[] { S(idDocente), S(idGrupo), Texto(busqueda) });

            var lista = new List<AsignaturaPromedio>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new AsignaturaPromedio
                {
                    Asignatura = Txt(r, "asignatura"),
                    Promedio = Dec(r, "promedio") ?? 0m,
                    Estudiantes = Int(r, "estudiantes")
                });
            }
            return lista;
        }
    }
}
