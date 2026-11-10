using System;
using System.Globalization;
namespace EscuelaCopalchi.UI.Models.PlanesEstudio
{
    public class PlanEstudio
    {
        public int IdPlan { get; set; }
        public int IdDocente { get; set; }
        public int IdGrupo { get; set; }
        public string Titulo { get; set; }
        public byte Mes { get; set; }
        public string Periodo { get { return Mes >= 1 && Mes <= 12 ? new DateTime(2000, Mes, 1).ToString("MMMM", new CultureInfo("es-CR")) : ""; } }
        public int Anio { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public string Archivo { get; set; }
        public DateTime FechaCarga { get; set; }
        public DateTime? FechaRevision { get; set; }
        public int? IdUsuarioRevision { get; set; }
        public string Asignatura { get; set; }
        public string NombreDocente { get; set; }
        public string Nivel { get; set; }
    }
}
