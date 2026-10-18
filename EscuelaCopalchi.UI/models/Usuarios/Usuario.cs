using System;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models.Usuarios
{
    public class Usuario
    {
        // Solo letras (con tildes y ñ), permite espacios entre palabras: "María Fernanda"
        private const string PatronLetras = @"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+(\s[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*$";

        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int IdRol { get; set; }

        // Solo para mostrarlo en el listado (viene del JOIN con ROL)
        public string NombreRol { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [RegularExpression(@"^(\d-\d{4}-\d{4}|\d{11,12})$",
            ErrorMessage = "La identificación debe tener el formato de cédula (1-1111-1111) o, si es extranjero, 11 o 12 dígitos sin guiones.")]
        public string Identificacion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [RegularExpression(PatronLetras,
            ErrorMessage = "El nombre solo puede contener letras.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [RegularExpression(PatronLetras,
            ErrorMessage = "El primer apellido solo puede contener letras.")]
        public string Apellido1 { get; set; }

        [RegularExpression(PatronLetras,
            ErrorMessage = "El segundo apellido solo puede contener letras.")]
        public string Apellido2 { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Correo { get; set; }

        [RegularExpression(@"^\d{4}-\d{4}$",
            ErrorMessage = "El teléfono debe tener el formato 8888-8888.")]
        public string Telefono { get; set; }

        // Viene del formulario en texto plano; NO se guarda así en la BD.
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarContrasena { get; set; }

        // Se llena en el Repository (BCrypt), nunca desde el formulario.
        public string ContrasenaHash { get; set; }

        public bool Estado { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public DateTime? FechaCreacion { get; set; }
    }
}
