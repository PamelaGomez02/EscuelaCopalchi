using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Estudiantes
{
    public class Alergia
    {
        public int IdAlergia { get; set; }

        public int IdEstudiante { get; set; }

        public string NombreAlergia { get; set; }

        public string Descripcion { get; set; }

        public DateTime FechaRegistro { get; set; }
    } 
}