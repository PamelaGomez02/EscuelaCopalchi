using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    public class FichaDatos
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Direccion { get; set; }
        public string Encargado { get; set; }
        public string TelefonoEncargado { get; set; }
        public bool Activo { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public int Anio { get; set; }
        public string Docente { get; set; }
        public string EstadoMatricula { get; set; }
    }

    public class FichaNota
    {
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public int Anio { get; set; }
        public string Asignatura { get; set; }
        public int Trimestre { get; set; }
        public decimal Nota { get; set; }
    }

    public class FichaAsistencia
    {
        public int Clases { get; set; }
        public int Presentes { get; set; }
        public int Tardanzas { get; set; }
        public int Justificados { get; set; }
        public int Ausentes { get; set; }

        // se descuentan solo las ausencias injustificadas
        public decimal? Porcentaje
        {
            get { return Clases == 0 ? (decimal?)null : Math.Round(100m * (Clases - Ausentes) / Clases, 1); }
        }
    }

    public class FichaExterna
    {
        public string Escuela { get; set; }
        public string Asignatura { get; set; }
        public string Periodo { get; set; }
        public int Anio { get; set; }
        public decimal Nota { get; set; }
    }

    public class FichaRepository
    {
        private readonly ConexionBD db = new ConexionBD();

        private static string S(int v) { return v.ToString(CultureInfo.InvariantCulture); }
        private static string Txt(DataRow r, string c) { return r[c] == DBNull.Value ? "" : Convert.ToString(r[c]); }
        private static int Int(DataRow r, string c) { return r[c] == DBNull.Value ? 0 : Convert.ToInt32(r[c]); }

        // null si el estudiante no existe o el docente no tiene acceso a él
        public FichaDatos Datos(int idDocente, int idEstudiante)
        {
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Ficha_Datos",
                new[] { "@id_docente", "@id_estudiante" }, new[] { S(idDocente), S(idEstudiante) });

            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];

            return new FichaDatos
            {
                IdEstudiante = Int(r, "id_estudiante"),
                Identificacion = Txt(r, "identificacion"),
                NombreCompleto = (Txt(r, "nombre") + " " + Txt(r, "apellido1") + " " + Txt(r, "apellido2")).Trim(),
                FechaNacimiento = r["fecha_nacimiento"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["fecha_nacimiento"]),
                Direccion = Txt(r, "direccion"),
                Encargado = Txt(r, "nombre_encargado"),
                TelefonoEncargado = Txt(r, "telefono_encargado"),
                Activo = r["estado"] != DBNull.Value && Convert.ToBoolean(r["estado"]),
                Grupo = Txt(r, "grupo"),
                Nivel = Txt(r, "nivel"),
                Anio = Int(r, "anio"),
                Docente = Txt(r, "docente"),
                EstadoMatricula = Txt(r, "estado_matricula")
            };
        }

        public List<FichaNota> Notas(int idEstudiante)
        {
            var lista = new List<FichaNota>();
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Ficha_Notas", new[] { "@id_estudiante" }, new[] { S(idEstudiante) });
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new FichaNota
                {
                    IdGrupo = Int(r, "id_grupo"),
                    Grupo = Txt(r, "grupo"),
                    Anio = Int(r, "anio"),
                    Asignatura = Txt(r, "asignatura"),
                    Trimestre = Int(r, "trimestre"),
                    Nota = Convert.ToDecimal(r["nota_final"])
                });
            }
            return lista;
        }

        public FichaAsistencia Asistencia(int idEstudiante)
        {
            var a = new FichaAsistencia();
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Ficha_Asistencia", new[] { "@id_estudiante" }, new[] { S(idEstudiante) });
            if (dt == null || dt.Rows.Count == 0) return a;

            DataRow r = dt.Rows[0];
            a.Clases = Int(r, "clases");
            a.Presentes = Int(r, "presentes");
            a.Tardanzas = Int(r, "tardanzas");
            a.Justificados = Int(r, "justificados");
            a.Ausentes = Int(r, "ausentes");
            return a;
        }

        public List<FichaExterna> Externas(int idEstudiante)
        {
            var lista = new List<FichaExterna>();
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Ficha_Externas", new[] { "@id_estudiante" }, new[] { S(idEstudiante) });
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new FichaExterna
                {
                    Escuela = Txt(r, "escuela"),
                    Asignatura = Txt(r, "asignatura"),
                    Periodo = Txt(r, "periodo"),
                    Anio = Int(r, "anio"),
                    Nota = Convert.ToDecimal(r["nota"])
                });
            }
            return lista;
        }
    }
}
