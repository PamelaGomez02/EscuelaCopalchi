using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Estudiantes
{
    public class AdecuacionAcademica
    {
        public int IdAdecuacion { get; set; }

        public int IdEstudiante { get; set; }

        public int IdUsuarioRegistro { get; set; }

        public int TipoAdecuacion { get; set; }

        public string NombreTipoAdecuacion { get; set; }

        public string Descripcion { get; set; }

        public string Medidas { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public string Estado { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}
