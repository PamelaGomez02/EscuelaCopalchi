
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese su correo.")]
        [EmailAddress(ErrorMessage = "Correo no válido.")]
        [Display(Name = "Correo institucional")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }
    }
}
