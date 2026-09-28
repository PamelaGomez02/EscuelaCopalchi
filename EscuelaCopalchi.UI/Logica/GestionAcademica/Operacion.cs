using System;
using System.Data.SqlClient;
using System.Diagnostics;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Error de una regla del negocio (ej. "El aula no tiene capacidad").
    /// Su mensaje se muestra tal cual al usuario.
    /// </summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje) { }
    }

    /// <summary>Módulos con los que se guardan los registros en la tabla AUDITORIA.</summary>
    public static class Modulos
    {
        public const string GestionAcademica = "Gestión académica";
        public const string Notificaciones = "Notificaciones";
    }

    /// <summary>Tipos usados en la bitácora (AUDITORIA.accion) y en las notificaciones.</summary>
    public static class TiposCambio
    {
        public const string Asignacion = "Asignación";
        public const string Traslado = "Traslado";
        public const string Retiro = "Retiro";
        public const string CambioAula = "Cambio de aula";
        public const string CambioHorario = "Cambio de horario";
        public const string AsignacionDocente = "Asignación docente";
        public const string CreacionGrupo = "Creación de grupo";
        public const string Importacion = "Importación";
        public const string ErrorEnvio = "Error de envío";
    }

    public static class TiposNotificacion
    {
        public const string NuevaAsignacion = "Nueva asignación";
        public const string Incorporacion = "Incorporación de estudiante";
        public const string RetiroEstudiante = "Retiro de estudiante";
        public const string CambioAula = "Cambio de aula";
        public const string CambioHorario = "Cambio de horario";
        public const string RetiroGrupo = "Retiro de grupo";
    }

    /// <summary>Convierte el resultado de una operación (o su error) en un ResultadoOperacion.</summary>
    public static class Operacion
    {
        /// <param name="accion">Hace el trabajo y devuelve el mensaje de éxito.</param>
        public static ResultadoOperacion Ejecutar(Func<string> accion)
        {
            try
            {
                return new ResultadoOperacion { Exito = true, Mensaje = accion() };
            }
            catch (ReglaNegocioException ex)
            {
                return ResultadoOperacion.Error(ex.Message);
            }
            catch (SqlException ex)
            {
                Trace.TraceError("Gestión académica - error de base de datos: " + ex);
                return ResultadoOperacion.Error("Error de base de datos: " + ex.Message);
            }
        }
    }
}