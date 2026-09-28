using System;
using System.Collections.Generic;
using System.Globalization;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>Consulta del historial de cambios del módulo (tabla AUDITORIA).</summary>
    public class BitacoraService
    {
        /// <param name="desde">Fecha "yyyy-MM-dd" o vacío.</param>
        /// <param name="hasta">Fecha "yyyy-MM-dd" o vacío.</param>
        /// <param name="tipo">Tipo de cambio o vacío para todos.</param>
        public List<RegistroBitacora> Listar(string desde, string hasta, string tipo)
        {
            return Repositorios.Consulta().Auditoria.Listar(Modulos.GestionAcademica, tipo, LeerFecha(desde), LeerFecha(hasta));
        }

        private static DateTime? LeerFecha(string texto)
        {
            DateTime fecha;
            return DateTime.TryParseExact(texto ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
                ? fecha
                : (DateTime?)null;
        }
    }
}