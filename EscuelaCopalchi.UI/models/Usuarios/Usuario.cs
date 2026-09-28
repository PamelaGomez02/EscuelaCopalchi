using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models.Usuarios
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        [Required]
        public int IdRol { get; set; }

        // Solo para mostrarlo en el listado (viene del JOIN con ROL)
        public string NombreRol { get; set; }

        [Required]
        public string Identificacion { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        public string Apellido1 { get; set; }

        public string Apellido2 { get; set; }

        [Required]
        [EmailAddress]
        public string Correo { get; set; }

        public string Telefono { get; set; }

        // Viene del formulario en texto plano; NO se guarda así en la BD.
        [Required]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        // Se llena en el Repository (BCrypt), nunca desde el formulario.
        public string ContrasenaHash { get; set; }

        public bool Estado { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public DateTime? FechaCreacion { get; set; }
    }
}
