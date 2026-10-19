
using System.Collections.Generic;

namespace EscuelaCopalchi.UI.Models
{
    public class AsignarRolViewModel
    {
        public int IdRol { get; set; }

        public string NombreRol { get; set; }

        public string DescripcionRol { get; set; }

        public List<UsuarioAsignableViewModel> Usuarios
        { get; set; } =
            new List<UsuarioAsignableViewModel>();
    }

    public class UsuarioAsignableViewModel
    {
        public int IdUsuario { get; set; }

        public string NombreCompleto { get; set; }

        public string Correo { get; set; }

        public bool Seleccionado { get; set; }
    }
}
