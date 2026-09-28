using System.Collections.Generic;

namespace EscuelaCopalchi.UI.Models
{
    public class RolPermisosViewModel
    {
        public int IdRol { get; set; }

        public string NombreRol { get; set; }

        public string DescripcionRol { get; set; }

        public List<PermisoViewModel> Permisos { get; set; }
            = new List<PermisoViewModel>();
    }

    public class PermisoViewModel
    {
        public int IdPermiso { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public string Modulo { get; set; }

        public bool Seleccionado { get; set; }
    }
}