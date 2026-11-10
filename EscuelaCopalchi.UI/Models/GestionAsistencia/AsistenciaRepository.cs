using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.GestionAsistencia
{
    public class AsistenciaRepository
    {
        public const string Ok = "Se ha guardado correctamente";
        private const string PrefijoError = "Error al ejecutar el procedimiento: ";

        private readonly ConexionBD db = new ConexionBD();

        // Quita el prefijo técnico para mostrar solo el mensaje del procedimiento (ej. "La fecha es obligatoria.")
        public static string Mensaje(Exception ex)
        {
            string m = ex.Message ?? "";
            return m.StartsWith(PrefijoError) ? m.Substring(PrefijoError.Length) : m;
        }

        // ---------- utilidades ----------
        private static string S(int n) { return n.ToString(CultureInfo.InvariantCulture); }
        private static string F(DateTime d) { return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }

        private static string Txt(DataRow r, string col)
        {
            return !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? "" : Convert.ToString(r[col]);
        }

        private static int Int(DataRow r, string col)
        {
            return !r.Table.Columns.Contains(col) || r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        }

        // Arma los parámetros. Los opcionales (fechas) solo se agregan si traen valor:
        // enviar una fecha vacía a SQL Server la convertiría en 1900-01-01.
        private class Parametros
        {
            public readonly List<string> Nombres = new List<string>();
            public readonly List<string> Valores = new List<string>();

            public Parametros Add(string nombre, int valor)
            {
                Nombres.Add(nombre); Valores.Add(S(valor)); return this;
            }

            public Parametros Add(string nombre, string valor)
            {
                Nombres.Add(nombre); Valores.Add(valor ?? ""); return this;
            }

            public Parametros Add(string nombre, DateTime valor)
            {
                Nombres.Add(nombre); Valores.Add(F(valor)); return this;
            }

            public Parametros AddSiHay(string nombre, DateTime? valor)
            {
                if (valor.HasValue) Add(nombre, valor.Value);
                return this;
            }
        }

        private DataTable Consultar(string procedimiento, Parametros p)
        {
            return db.ejecutarProcedimiento(procedimiento, p.Nombres.ToArray(), p.Valores.ToArray());
        }

        private string Guardar(string procedimiento, Parametros p)
        {
            return db.ejecutarProcedimiento_Save(procedimiento, p.Nombres.ToArray(), p.Valores.ToArray());
        }

        // ---------- grupos ----------
        // idDocente = 0 -> todos los grupos; > 0 -> solo los de ese docente
        public List<GrupoAsistenciaItem> ListarGrupos(int idDocente)
        {
            DataTable dt = Consultar("SP_AS_Grupo_Listar", new Parametros().Add("@id_docente", idDocente));
            var l = new List<GrupoAsistenciaItem>();

            foreach (DataRow r in dt.Rows)
            {
                l.Add(new GrupoAsistenciaItem
                {
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = r.Table.Columns.Contains("grupo") ? Txt(r, "grupo") : Txt(r, "nombre_grupo"),
                    Nivel = Txt(r, "nivel"),
                    Anio = Int(r, "anio"),
                    Periodo = Txt(r, "periodo"),
                    Docente = Txt(r, "docente"),
                    TotalEstudiantes = Int(r, "total_estudiantes")
                });
            }
            return l;
        }

        // ---------- registrar ----------
        // Todos los estudiantes activos del grupo, con su registro del día si ya existe.
        public List<AsistenciaItem> ListaClase(int idGrupo, DateTime fecha, int idDocente)
        {
            DataTable dt = Consultar("SP_AS_Clase_Estudiantes", new Parametros()
                .Add("@id_grupo", idGrupo).Add("@fecha", fecha).Add("@id_docente", idDocente));

            return MapearFilas(dt, idGrupo, fecha);
        }

        // Devuelve Ok si se guardó; si no, el mensaje de error del procedimiento.
        public string Registrar(int idUsuario, int idDocente, int idGrupo, DateTime fecha,
                                List<AsistenciaGuardarItem> filas)
        {
            var lote = new List<object>();
            foreach (var f in filas ?? new List<AsistenciaGuardarItem>())
            {
                lote.Add(new
                {
                    id_estudiante = f.IdEstudiante,
                    estado = f.Estado ?? "",
                    hora_llegada = f.HoraLlegada ?? "",
                    observacion = f.Observacion ?? ""
                });
            }

            return Guardar("SP_AS_Asistencia_Registrar", new Parametros()
                .Add("@id_usuario", idUsuario)
                .Add("@id_docente", idDocente)
                .Add("@id_grupo", idGrupo)
                .Add("@fecha", fecha)
                .Add("@json", JsonConvert.SerializeObject(lote)));
        }

        // ---------- modificar ----------
        public AsistenciaItem Obtener(int idAsistencia, int idDocente)
        {
            DataTable dt = Consultar("SP_AS_Asistencia_Obtener", new Parametros()
                .Add("@id_asistencia", idAsistencia).Add("@id_docente", idDocente));

            var l = MapearFilas(dt, 0, null);
            return l.Count == 0 ? null : l[0];
        }

        public string Modificar(int idUsuario, int idDocente, int idAsistencia,
                                string estado, string hora, string observacion)
        {
            return Guardar("SP_AS_Asistencia_Modificar", new Parametros()
                .Add("@id_usuario", idUsuario)
                .Add("@id_docente", idDocente)
                .Add("@id_asistencia", idAsistencia)
                .Add("@estado", estado)
                .Add("@hora", hora)
                .Add("@observacion", observacion));
        }

        // ---------- consultas ----------
        public List<AsistenciaItem> Consultar(int idGrupo, DateTime fecha, int idDocente)
        {
            DataTable dt = Consultar("SP_AS_Asistencia_Consultar", new Parametros()
                .Add("@id_grupo", idGrupo).Add("@fecha", fecha).Add("@id_docente", idDocente));

            return MapearFilas(dt, idGrupo, fecha);
        }

        // idEstudiante = 0 -> todo el grupo. desde / hasta null -> sin límite.
        public List<AsistenciaItem> Historial(int idGrupo, int idEstudiante,
                                              DateTime? desde, DateTime? hasta, int idDocente)
        {
            DataTable dt = Consultar("SP_AS_Asistencia_Historial", new Parametros()
                .Add("@id_grupo", idGrupo)
                .Add("@id_estudiante", idEstudiante)
                .AddSiHay("@desde", desde)
                .AddSiHay("@hasta", hasta)
                .Add("@id_docente", idDocente));

            return MapearFilas(dt, idGrupo, null);
        }


        public List<PorcentajeItem> Porcentaje(
            int idGrupo,
            int idEstudiante,
            DateTime? desde,
            DateTime? hasta,
            int idDocente)
        {
            var parametros = new Parametros()
                .Add("@id_grupo", idGrupo)
                .Add("@id_estudiante", idEstudiante)
                .AddSiHay("@desde", desde)
                .AddSiHay("@hasta", hasta);

            DataTable dt = Consultar(
                "SP_AS_Asistencia_Porcentaje",
                parametros
            );

            var lista = new List<PorcentajeItem>();

            if (dt == null || dt.Rows.Count == 0)
                return lista;

            foreach (DataRow r in dt.Rows)
            {
                decimal? porcentaje = null;

                if (dt.Columns.Contains("porcentaje_asistencia")
                    && r["porcentaje_asistencia"] != DBNull.Value)
                {
                    porcentaje = Convert.ToDecimal(
                        r["porcentaje_asistencia"]
                    );
                }
                else if (dt.Columns.Contains("porcentaje")
                         && r["porcentaje"] != DBNull.Value)
                {
                    porcentaje = Convert.ToDecimal(
                        r["porcentaje"]
                    );
                }

                lista.Add(new PorcentajeItem
                {
                    IdEstudiante = Int(r, "id_estudiante"),

                    Estudiante = !string.IsNullOrWhiteSpace(
                        Txt(r, "nombre_completo"))
                        ? Txt(r, "nombre_completo")
                        : Txt(r, "estudiante"),

                    TotalRegistros = Int(r, "total_registros"),
                    Presentes = Int(r, "presentes"),
                    Ausentes = Int(r, "ausentes"),
                    Tardanzas = Int(r, "tardanzas"),
                    Justificados = Int(r, "justificados"),
                    Porcentaje = porcentaje
                });
            }

            return lista;
        }


        // ---------- mapeo común ----------
        // Los procedimientos devuelven las columnas que existan; las que no vienen quedan vacías.
        private static List<AsistenciaItem> MapearFilas(DataTable dt, int idGrupoPorDefecto, DateTime? fechaPorDefecto)
        {
            var l = new List<AsistenciaItem>();
            if (dt == null) return l;

            foreach (DataRow r in dt.Rows)
            {
                var item = new AsistenciaItem
                {
                    IdGrupo = dt.Columns.Contains("id_grupo") ? Int(r, "id_grupo") : idGrupoPorDefecto,
                    Grupo = dt.Columns.Contains("grupo") ? Txt(r, "grupo") : "",
                    IdEstudiante = Int(r, "id_estudiante"),
                    Estudiante = Txt(r, "estudiante") != "" ? Txt(r, "estudiante") : Txt(r, "nombre_completo"),
                    Estado = dt.Columns.Contains("estado") ? Txt(r, "estado") : "",
                    HoraLlegada = dt.Columns.Contains("hora_llegada") ? Txt(r, "hora_llegada") : "",
                    Observacion = dt.Columns.Contains("observacion") ? Txt(r, "observacion") : ""
                };

                if (dt.Columns.Contains("id_asistencia") && r["id_asistencia"] != DBNull.Value)
                    item.IdAsistencia = Convert.ToInt32(r["id_asistencia"]);

                if (dt.Columns.Contains("fecha") && r["fecha"] != DBNull.Value)
                    item.Fecha = Convert.ToDateTime(r["fecha"]);
                else
                    item.Fecha = fechaPorDefecto;

                l.Add(item);
            }
            return l;
        }
    }
}