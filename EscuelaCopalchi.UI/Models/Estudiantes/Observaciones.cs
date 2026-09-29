using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Estudiantes
{
    public class Observaciones
    {
        public int IdObservacion { get; set; }

        public int IdEstudiante { get; set; }

        public string Titulo { get; set; }

        public string Observacion { get; set; }

        public DateTime FechaRegistro { get; set; }
    } 
}