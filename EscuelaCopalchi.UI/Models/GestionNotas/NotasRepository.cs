using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace EscuelaCopalchi.UI.Models.GestionNotas
{
    public class NotasRepository
    {
        public const string Ok = "Se ha guardado correctamente";

        private readonly ConexionBD db;

        public NotasRepository()
        {
            db = new ConexionBD();
        }

        // ---------- utilidades ----------
        private static string S(int n) { return n.ToString(CultureInfo.InvariantCulture); }
        private static string S(decimal n) { return n.ToString(CultureInfo.InvariantCulture); }

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

        private static DateTime? FechaN(DataRow r, string col)
        {
            return r[col] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r[col]);
        }

        // ---------- docente ----------
        // Devuelve el id de DOCENTE del usuario, o 0 si el usuario no es docente.
        public int ObtenerIdDocente(int idUsuario)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GA_Usuario_Listar",
                new[] { "@id_usuario" },
                new[] { S(idUsuario) });

            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["id_docente"] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(dt.Rows[0]["id_docente"]);
        }

        // ---------- grupos ----------
        // idDocente = 0 -> todos los grupos (solo para quien puede ver todas las notas).
        public List<GrupoNotasItem> ListarGrupos(int idDocente)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Grupo_Resumen",
                new[] { "@id_docente" },
                new[] { S(idDocente) });

            var lista = new List<GrupoNotasItem>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new GrupoNotasItem
                {
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = Txt(r, "grupo"),
                    Nivel = Txt(r, "nivel"),
                    Anio = Int(r, "anio"),
                    Docente = Txt(r, "docente"),
                    TotalEstudiantes = Int(r, "total_estudiantes"),
                    TotalEvaluaciones = Int(r, "total_evaluaciones"),
                    NotasRegistradas = Int(r, "notas_registradas")
                });
            }

            return lista;
        }

        public List<EstudianteItem> ListarEstudiantesGrupo(int idGrupo)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GA_Matricula_Listar",
                new[] { "@id_grupo" },
                new[] { S(idGrupo) });

            var lista = new List<EstudianteItem>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new EstudianteItem
                {
                    IdEstudiante = Int(r, "id_estudiante"),
                    Nombre = Txt(r, "nombre_completo")
                });
            }

            return lista;
        }

        // ---------- evaluaciones ----------
        public List<string> ListarAsignaturas(int idGrupo)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Asignatura_Listar",
                new[] { "@id_grupo" },
                new[] { S(idGrupo) });

            var lista = new List<string>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(Txt(r, "asignatura"));
            }

            return lista;
        }

        public List<EvaluacionItem> ListarEvaluaciones(int idGrupo, string asignatura, int trimestre)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Evaluacion_Listar",
                new[] { "@id_grupo", "@asignatura", "@trimestre" },
                new[] { S(idGrupo), asignatura ?? "", S(trimestre) });

            var lista = new List<EvaluacionItem>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new EvaluacionItem
                {
                    IdEvaluacion = Int(r, "id_evaluacion"),
                    IdGrupo = Int(r, "id_grupo"),
                    Asignatura = Txt(r, "asignatura"),
                    Trimestre = Int(r, "trimestre"),
                    Nombre = Txt(r, "nombre"),
                    Porcentaje = Dec(r, "porcentaje"),
                    Fecha = FechaN(r, "fecha"),
                    NotaMaxima = Dec(r, "nota_maxima")
                });
            }

            return lista;
        }

        public string CrearEvaluacion(
            int idGrupo, string asignatura, int trimestre, string nombre,
            decimal porcentaje, DateTime? fecha, decimal notaMaxima,
            int idUsuario, int idDocente)
        {
            var nombres = new List<string>
            {
                "@id_grupo", "@asignatura", "@trimestre", "@nombre",
                "@porcentaje", "@nota_maxima", "@id_usuario", "@id_docente"
            };

            var valores = new List<string>
            {
                S(idGrupo), asignatura, S(trimestre), nombre,
                S(porcentaje), S(notaMaxima), S(idUsuario), S(idDocente)
            };

            if (fecha.HasValue)
            {
                nombres.Add("@fecha");
                valores.Add(fecha.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            return db.ejecutarProcedimiento_Save(
                "SP_GN_Evaluacion_Insertar", nombres.ToArray(), valores.ToArray());
        }

        // ---------- calificaciones ----------
        // Una fila por estudiante con sus notas por evaluación.
        public List<EstudianteNotas> ObtenerMatriz(int idGrupo, string asignatura, int trimestre)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Calificacion_Matriz",
                new[] { "@id_grupo", "@asignatura", "@trimestre" },
                new[] { S(idGrupo), asignatura ?? "", S(trimestre) });

            var porId = new Dictionary<int, EstudianteNotas>();
            var orden = new List<EstudianteNotas>();

            foreach (DataRow r in dt.Rows)
            {
                int idEstudiante = Int(r, "id_estudiante");
                EstudianteNotas est;

                if (!porId.TryGetValue(idEstudiante, out est))
                {
                    est = new EstudianteNotas
                    {
                        IdEstudiante = idEstudiante,
                        Identificacion = Txt(r, "identificacion"),
                        NombreCompleto = Txt(r, "nombre_completo")
                    };
                    porId[idEstudiante] = est;
                    orden.Add(est);
                }

                if (r["id_evaluacion"] != DBNull.Value)
                {
                    est.Notas.Add(new NotaCelda
                    {
                        IdEvaluacion = Int(r, "id_evaluacion"),
                        Evaluacion = Txt(r, "evaluacion"),
                        Porcentaje = Dec(r, "porcentaje"),
                        NotaMaxima = Dec(r, "nota_maxima"),
                        IdCalificacion = IntN(r, "id_calificacion"),
                        Nota = DecN(r, "nota")
                    });
                }
            }

            return orden;
        }

        // Guarda (inserta o modifica) varias notas en una sola operación: todo o nada.
        // idDocente debe ser > 0 (0 significa "sin restricción" en el procedimiento).
        public string GuardarLote(int idUsuario, int idDocente, IEnumerable<NotaGuardar> notas)
        {
            var datos = notas
                .Where(n => n.Nota.HasValue)
                .Select(n => new
                {
                    id_evaluacion = n.IdEvaluacion,
                    id_estudiante = n.IdEstudiante,
                    nota = n.Nota,
                    observacion = n.Observacion
                })
                .ToList();

            if (datos.Count == 0)
            {
                return "No hay notas para guardar.";
            }

            string json = JsonConvert.SerializeObject(datos);

            return db.ejecutarProcedimiento_Save(
                "SP_GN_Calificacion_GuardarLote",
                new[] { "@id_usuario", "@id_docente", "@json" },
                new[] { S(idUsuario), S(idDocente), json });
        }

        public CalificacionDetalle ObtenerCalificacion(int idCalificacion)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Calificacion_Obtener",
                new[] { "@id_calificacion" },
                new[] { S(idCalificacion) });

            if (dt == null || dt.Rows.Count == 0)
            {
                return null;
            }

            DataRow r = dt.Rows[0];

            return new CalificacionDetalle
            {
                IdCalificacion = Int(r, "id_calificacion"),
                IdEvaluacion = Int(r, "id_evaluacion"),
                IdEstudiante = Int(r, "id_estudiante"),
                Estudiante = Txt(r, "estudiante"),
                IdGrupo = Int(r, "id_grupo"),
                Grupo = Txt(r, "grupo"),
                IdDocente = IntN(r, "id_docente"),
                Evaluacion = Txt(r, "evaluacion"),
                Asignatura = Txt(r, "asignatura"),
                Trimestre = Int(r, "trimestre"),
                Porcentaje = Dec(r, "porcentaje"),
                NotaMaxima = Dec(r, "nota_maxima"),
                Nota = Dec(r, "nota"),
                Observacion = Txt(r, "observacion"),
                FechaRegistro = Convert.ToDateTime(r["fecha_registro"])
            };
        }

        // idDocente = 0 -> todos los grupos. Los demás filtros: 0 / "" = sin filtro.
        public List<HistorialItem> Historial(
            int idDocente, int idGrupo, int idEstudiante, string asignatura, int trimestre)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Calificacion_Historial",
                new[] { "@id_docente", "@id_grupo", "@id_estudiante", "@asignatura", "@trimestre" },
                new[] { S(idDocente), S(idGrupo), S(idEstudiante), asignatura ?? "", S(trimestre) });

            var lista = new List<HistorialItem>();

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new HistorialItem
                {
                    Origen = Txt(r, "origen"),
                    IdCalificacion = IntN(r, "id_calificacion"),
                    IdEstudiante = Int(r, "id_estudiante"),
                    Estudiante = Txt(r, "estudiante"),
                    Grupo = Txt(r, "grupo"),
                    Asignatura = Txt(r, "asignatura"),
                    Periodo = Txt(r, "periodo"),
                    Anio = Int(r, "anio"),
                    Evaluacion = Txt(r, "evaluacion"),
                    Nota = Dec(r, "nota"),
                    NotaMaxima = Dec(r, "nota_maxima"),
                    EscuelaProcedencia = Txt(r, "escuela_procedencia"),
                    Fecha = Convert.ToDateTime(r["fecha"]),
                    IdDocente = IntN(r, "id_docente")
                });
            }

            return lista;
        }

        // ---------- importación Excel ----------
        // Devuelve identificación (solo letras y dígitos) -> id_estudiante
        public Dictionary<string, int> BuscarEstudiantes(int idGrupo)
        {
            DataTable dt = db.ejecutarProcedimiento(
                "SP_GN_Estudiante_Buscar",
                new[] { "@id_grupo" },
                new[] { S(idGrupo) });

            var d = new Dictionary<string, int>();

            foreach (DataRow r in dt.Rows)
            {
                d[NormalizarId(Txt(r, "identificacion"))] = Int(r, "id_estudiante");
            }

            return d;
        }

        public static string NormalizarId(string texto)
        {
            return new string((texto ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        public string GuardarExternas(int idUsuario, int idDocente, IEnumerable<NotaExterna> filas)
        {
            var datos = filas.Select(f => new
            {
                id_estudiante = f.IdEstudiante,
                escuela_procedencia = f.Escuela,
                asignatura = f.Asignatura,
                periodo = f.Periodo,
                anio = f.Anio,
                nota = f.Nota
            }).ToList();

            if (datos.Count == 0) return "No hay notas para guardar.";

            return db.ejecutarProcedimiento_Save(
                "SP_GN_CalificacionExterna_GuardarLote",
                new[] { "@id_usuario", "@id_docente", "@json" },
                new[] { S(idUsuario), S(idDocente), JsonConvert.SerializeObject(datos) });
        }
    }
}
