using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Notificaciones
{
    public class Notificacion
    {
        public int IdNotificacion { get; set; }

        public int IdUsuario { get; set; }

        public string Tipo { get; set; }

        public string Titulo { get; set; }

        public string Mensaje { get; set; }

        public DateTime Fecha { get; set; }

        public bool Leida { get; set; }

        public DateTime? FechaLectura { get; set; }

        public string EstadoEnvio { get; set; }

       
    }
}