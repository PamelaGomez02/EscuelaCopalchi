using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Notificaciones
{
    public class PreferenciaNotificacion
    {
        public int IdTipo { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public bool PermiteDesactivar { get; set; }
        public bool RecibirSistema { get; set; }
        public bool RecibirCorreo { get; set; }

        public string FrecuenciaCorreo { get; set; }
    }
}