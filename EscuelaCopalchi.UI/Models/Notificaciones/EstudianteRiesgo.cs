using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Notificaciones
{
    public class EstudianteRiesgo
    {
        public int IdEstudiante { get; set; }

        public string NombreCompleto { get; set; }

        public string Grupo { get; set; }

        public string Motivo { get; set; }

        public string Docente { get; set; }

        public decimal? Promedio { get; set; }

        public int Ausencias { get; set; }

        public int Tardias { get; set; }
    }
}