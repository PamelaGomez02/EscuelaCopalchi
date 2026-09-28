using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCopalchi.UI.Models
{
    public class AsignarRolViewModel
    {
        public int IdRol { get; set; }

        public string NombreRol { get; set; }

        public string DescripcionRol { get; set; }

        public List<UsuarioAsignacionViewModel> Usuarios { get; set; }
            = new List<UsuarioAsignacionViewModel>();
    }

    public class UsuarioAsignacionViewModel
    {
        public int IdUsuario { get; set; }

        public string NombreCompleto { get; set; }

        public string Correo { get; set; }

        public bool Seleccionado { get; set; }
    }
}