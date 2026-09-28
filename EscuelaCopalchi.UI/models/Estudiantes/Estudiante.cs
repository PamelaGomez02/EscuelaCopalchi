using System;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models.Estudiantes
{
    public class Estudiante
    {
        public int IdEstudiante { get; set; }

        [Required]
        public string Identificacion { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido1 { get; set; }

        public string Apellido2 { get; set; }

        [Required]
        public DateTime FechaNacimiento { get; set; }

        public string Direccion { get; set; }

        [Required]
        public string NombreEncargado { get; set; }

        [Required]
        public string Parentesco { get; set; }

        [Required]
        public string TelefonoEncargado { get; set; }

        [Required]
        public string CorreoEncargado { get; set; }

        public bool Estado { get; set; }
    }
}