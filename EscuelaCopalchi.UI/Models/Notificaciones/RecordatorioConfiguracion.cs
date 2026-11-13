using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Notificaciones
{
    public class RecordatorioConfiguracion
    {
        public string TipoRecordatorio { get; set; }

        public int FrecuenciaDias { get; set; }

        public bool Activo { get; set; }
    }
}