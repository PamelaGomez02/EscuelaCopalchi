
using EscuelaCopalchi.UI.Controllers;
using EscuelaCopalchi.UI.Logica.GestionAcademica;
using System;
using System.Web;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // Contexto académico del usuario autenticado.
    // No comprueba roles ni permisos del módulo.
    public class MonitoreoSesion
    {
        public const string ClaveItems = "MonitoreoSesion";

        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public int IdDocente { get; set; }

        // Mantener estas propiedades para compatibilidad
        // con otras partes del proyecto.
        public bool TieneDocente { get; set; }
        public bool TieneInstitucional { get; set; }

        public bool PuedeDocente
        {
            get { return IdDocente > 0; }
        }

        public bool PuedeInstitucional
        {
            get { return TieneInstitucional; }
        }

        // Nunca usar 0 como alcance desconocido.
        public int AlcanceDocente
        {
            get { return IdDocente > 0 ? IdDocente : -1; }
        }

        public static MonitoreoSesion Actual(
            HttpContextBase contexto)
        {
            if (contexto == null)
                return null;

            // Reutilizar el contexto durante la solicitud.
            var existente =
                contexto.Items[ClaveItems] as MonitoreoSesion;

            if (existente != null)
                return existente;

            var actual =
                SesionUsuario.Obtener(contexto.Session);

            if (actual == null)
                return null;

            var usuario =
                new UsuarioService().Obtener(actual.IdUsuario);

            if (usuario == null || !usuario.Estado)
                return null;

            var sesion = new MonitoreoSesion
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.NombreCompleto,
                Rol = usuario.Rol,
                IdDocente = usuario.IdDocente
            };

            contexto.Items[ClaveItems] = sesion;

            return sesion;
        }
    }
}
