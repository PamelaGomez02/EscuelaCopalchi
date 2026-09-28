using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>
    /// Bitácora de cambios e incidentes, guardados en la tabla AUDITORIA de la base principal.
    /// Cada registro indica el módulo, la acción y una descripción en texto.
    /// </summary>
    public interface IAuditoriaRepository
    {
        void Registrar(string modulo, string accion, string tabla, int idRegistro, string descripcion, int idUsuario);

        /// <param name="accion">Vacío = todas.</param>
        List<RegistroBitacora> Listar(string modulo, string accion, DateTime? desde, DateTime? hasta);
    }

    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly IAccesoDatos db;

        public AuditoriaRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public void Registrar(string modulo, string accion, string tabla, int idRegistro, string descripcion, int idUsuario)
        {
            if (descripcion != null && descripcion.Length > 1000) descripcion = descripcion.Substring(0, 1000);

            db.Ejecutar("SP_GA_Auditoria_Insertar",
                Id("@id_usuario", idUsuario), P("@accion", accion), P("@modulo", modulo), P("@tabla_afectada", tabla),
                P("@registro_afectado", idRegistro > 0 ? idRegistro.ToString() : null), P("@descripcion", descripcion));
        }

        public List<RegistroBitacora> Listar(string modulo, string accion, DateTime? desde, DateTime? hasta)
        {
            return db.Consultar("SP_GA_Auditoria_Listar",
                        P("@modulo", modulo), P("@accion", accion ?? ""), P("@desde", desde), P("@hasta", hasta))
                     .AsEnumerable()
                     .Select(r => new RegistroBitacora
                     {
                         IdAuditoria = Convert.ToInt64(r["id_auditoria"]),
                         Fecha = Fila.Fecha(r, "fecha") ?? DateTime.MinValue,
                         TipoCambio = Fila.Texto(r, "accion"),
                         Detalle = Fila.Texto(r, "descripcion"),
                         Responsable = Fila.Texto(r, "responsable")
                     }).ToList();
        }
    }
}