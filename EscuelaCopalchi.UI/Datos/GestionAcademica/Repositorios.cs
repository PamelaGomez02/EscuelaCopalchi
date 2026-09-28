namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>
    /// Agrupa todos los repositorios del módulo sobre un mismo acceso a datos.
    ///   Repositorios.Consulta()        -> consultas normales
    ///   new Repositorios(transaccion)  -> todo dentro de la misma transacción
    /// </summary>
    public class Repositorios
    {
        public Repositorios(IAccesoDatos db)
        {
            Usuarios = new UsuarioRepository(db);
            Aulas = new AulaRepository(db);
            Grupos = new GrupoRepository(db);
            Matriculas = new MatriculaRepository(db);
            Auditoria = new AuditoriaRepository(db);
            Notificaciones = new NotificacionRepository(db);
        }

        public IUsuarioRepository Usuarios { get; private set; }
        public IAulaRepository Aulas { get; private set; }
        public IGrupoRepository Grupos { get; private set; }
        public IMatriculaRepository Matriculas { get; private set; }
        public IAuditoriaRepository Auditoria { get; private set; }
        public INotificacionRepository Notificaciones { get; private set; }

        /// <summary>Repositorios para consultas (sin transacción).</summary>
        public static Repositorios Consulta()
        {
            return new Repositorios(new AccesoDatos());
        }
    }
}