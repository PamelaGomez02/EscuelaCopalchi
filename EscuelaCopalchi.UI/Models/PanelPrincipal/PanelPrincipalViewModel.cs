using System.Collections.Generic;

namespace EscuelaCopalchi.UI.Models.PanelPrincipal
{
    public class AccesoPanel
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Icono { get; set; }
        public string Controlador { get; set; }
        public string Accion { get; set; }
    }

    public class PanelPrincipalViewModel
    {
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public bool EsAdministrador { get; set; }
        public int TotalUsuariosActivos { get; set; }
        public int TotalRolesActivos { get; set; }
        public int TotalPermisosActivos { get; set; }
        public int TotalUsuarios { get; set; }
        public bool MostrarIndicadoresAdministrativos { get; set; }
        public List<AccesoPanel> Accesos { get; set; } = new List<AccesoPanel>();
    }
}
