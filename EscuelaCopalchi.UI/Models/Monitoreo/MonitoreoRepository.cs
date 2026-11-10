using EscuelaCopalchi.UI.Models.GestionNotas;      // NotaExterna
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // Lee de la base de datos con los procedimientos SP_MON_*. No calcula nada:
    // los promedios y estados los arma MonitoreoCalculo.
    //
    // Alcance (parámetro idDocente):
    //    > 0  solo los grupos de ese docente        0  todos los grupos (director)
    public class MonitoreoRepository
    {
        private const string PrefijoError = "Error al ejecutar el procedimiento: ";

        private readonly ConexionBD db;

        public MonitoreoRepository()
        {
            db = new ConexionBD();
        }

        // Quita el prefijo técnico para mostrar solo el mensaje real del error.
        public static string Mensaje(Exception ex)
        {
            string m = ex.Message ?? "";
            return m.StartsWith(PrefijoError) ? m.Substring(PrefijoError.Length) : m;
        }

        // ---------- utilidades ----------
        private static string S(int n) { return n.ToString(CultureInfo.InvariantCulture); }

        private static string Txt(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? "" : Convert.ToString(r[col]);
        }

        private static int Int(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        }

        private static int? IntN(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? (int?)null : Convert.ToInt32(r[col]);
        }

        private static decimal Dec(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0m : Convert.ToDecimal(r[col]);
        }

        private static decimal? DecN(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r[col]);
        }

        // ---------- filtros ----------
        public List<int> ListarAnios(int idDocente)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_MON_Anio_Listar", new[] { "@id_docente" }, new[] { S(idDocente) });

            var lista = new List<int>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows) lista.Add(Int(r, "anio"));
            return lista;
        }

        // anio = 0 -> todos los años
        public List<GrupoMonitoreoItem> ListarGrupos(int idDocente, int anio)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_MON_Grupo_Listar",
                new[] { "@id_docente", "@anio" },
                new[] { S(idDocente), S(anio) });

            return MapearGrupos(dt);
        }

        public static List<GrupoMonitoreoItem> MapearGrupos(DataTable dt)
        {
            var lista = new List<GrupoMonitoreoItem>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new GrupoMonitoreoItem
                {
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = Txt(r, "grupo"),
                    Nivel = Txt(r, "nivel"),
                    Seccion = Txt(r, "seccion"),
                    Anio = Int(r, "anio"),
                    Docente = Txt(r, "docente"),
                    TotalEstudiantes = Int(r, "total_estudiantes")
                });
            }
            return lista;
        }

        // ---------- notas finales (base de promedios, reportes y estadísticas) ----------
        // 0 = sin filtro en idGrupo, idEstudiante y anio.
        // soloActivas = true  -> matrículas activas (vista actual)
        //             = false -> todas las matrículas (historial del estudiante)
        public List<NotaFinalFila> ObtenerNotasFinales(
            int idDocente, int idGrupo, int idEstudiante, int anio, bool soloActivas)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_MON_Notas_Finales",
                new[] { "@id_docente", "@id_grupo", "@id_estudiante", "@anio", "@solo_activas" },
                new[] { S(idDocente), S(idGrupo), S(idEstudiante), S(anio), soloActivas ? "1" : "0" });

            return MapearNotasFinales(dt);
        }

        public static List<NotaFinalFila> MapearNotasFinales(DataTable dt)
        {
            var lista = new List<NotaFinalFila>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new NotaFinalFila
                {
                    IdEstudiante = Int(r, "id_estudiante"),
                    Identificacion = Txt(r, "identificacion"),
                    Estudiante = Txt(r, "estudiante"),
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = Txt(r, "grupo"),
                    Nivel = Txt(r, "nivel"),
                    Seccion = Txt(r, "seccion"),
                    Anio = Int(r, "anio"),
                    Asignatura = Txt(r, "asignatura"),
                    Trimestre = IntN(r, "trimestre"),
                    EvaluacionesTotal = Int(r, "evaluaciones_total"),
                    EvaluacionesConNota = Int(r, "evaluaciones_con_nota"),
                    NotaFinal = DecN(r, "nota_final")
                });
            }
            return lista;
        }

        // ---------- historial del director ----------
        public List<EstudianteBusquedaItem> BuscarEstudiantes(string texto)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_MON_Estudiante_Buscar",
                new[] { "@texto" },
                new[] { (texto ?? "").Trim() });

            return MapearEstudiantes(dt);
        }

        public static List<EstudianteBusquedaItem> MapearEstudiantes(DataTable dt)
        {
            var lista = new List<EstudianteBusquedaItem>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new EstudianteBusquedaItem
                {
                    IdEstudiante = Int(r, "id_estudiante"),
                    Identificacion = Txt(r, "identificacion"),
                    Estudiante = Txt(r, "estudiante"),
                    GrupoActual = Txt(r, "grupo_actual")
                });
            }
            return lista;
        }

        // Notas de escuelas anteriores (se muestran junto al historial)
        public List<NotaExterna> ObtenerNotasExternas(int idEstudiante)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_MON_Notas_Externas",
                new[] { "@id_estudiante" },
                new[] { S(idEstudiante) });

            var lista = new List<NotaExterna>();
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new NotaExterna
                {
                    IdEstudiante = Int(r, "id_estudiante"),
                    Escuela = Txt(r, "escuela_procedencia"),
                    Asignatura = Txt(r, "asignatura"),
                    Periodo = Txt(r, "periodo"),
                    Anio = Int(r, "anio"),
                    Nota = Dec(r, "nota")
                });
            }
            return lista;
        }
    }
}