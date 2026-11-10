using System;
using System.Globalization;
using System.Text;
using System.Web;
using global::EscuelaCopalchi.UI.Models.GestionNotas;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    // Usuario que trabaja en Reportes: se toma del inicio de sesión general (Account/Login)
    public class ReportesSesion
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public int IdDocente { get; set; }

        private static string Norm(string s)
        {
            string d = (s ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString();
        }

        public bool EsDocente { get { return Norm(Rol) == "docente"; } }
        public bool EsDirector { get { return Norm(Rol) == "director"; } }
        public bool EsEvaluacion { get { return Norm(Rol).StartsWith("evaluacion"); } }

        public static ReportesSesion Obtener(HttpSessionStateBase session)
        {
            if (session == null || session["IdUsuario"] == null) return null;

            int idUsuario = Convert.ToInt32(session["IdUsuario"]);

            var s = new ReportesSesion
            {
                IdUsuario = idUsuario,
                Nombre = Convert.ToString(session["NombreUsuario"]),
                Rol = Convert.ToString(session["RolUsuario"])
            };

            if (s.EsDocente)
            {
                // el id de docente se consulta una vez por usuario y se guarda en la sesión
                if (session["RP_DocenteUsuario"] == null || Convert.ToInt32(session["RP_DocenteUsuario"]) != idUsuario)
                {
                    int idDocente = 0;
                    try { idDocente = new NotasRepository().ObtenerIdDocente(idUsuario); } catch (Exception) { }
                    session["RP_DocenteUsuario"] = idUsuario;
                    session["RP_Docente"] = idDocente;
                }
                s.IdDocente = Convert.ToInt32(session["RP_Docente"]);
            }

            return s;
        }
    }
}
