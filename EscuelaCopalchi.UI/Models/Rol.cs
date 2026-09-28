using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models
{
    public class Rol
    {
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Nombre del rol")]
        public string Nombre { get; set; }

        [StringLength(250)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; }
    }
}
