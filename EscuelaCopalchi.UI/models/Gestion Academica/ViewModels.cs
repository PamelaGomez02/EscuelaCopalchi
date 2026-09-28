using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.GestionAcademica
{
    public class GestionAcademicaIndexViewModel
    {
        public GestionAcademicaIndexViewModel()
        {
            Resumen = new ResumenAcademico();
            Usuarios = new List<UsuarioSistema>();
        }

        public ResumenAcademico Resumen { get; set; }
        public List<UsuarioSistema> Usuarios { get; set; }
    }

    public class MiGrupoViewModel
    {
        public MiGrupoViewModel()
        {
            Grupos = new List<Grupo>();
            Estudiantes = new List<EstudianteGrupo>();
            Horario = new List<HorarioDia>();
        }

        public UsuarioSistema Docente { get; set; }

        /// <summary>Todos los grupos a cargo del docente.</summary>
        public List<Grupo> Grupos { get; set; }

        /// <summary>Grupo que se está consultando.</summary>
        public Grupo GrupoSeleccionado { get; set; }
        public List<EstudianteGrupo> Estudiantes { get; set; }
        public List<HorarioDia> Horario { get; set; }

        public bool TieneGrupos { get { return Grupos.Count > 0; } }
    }

    public class AsignacionesViewModel
    {
        public AsignacionesViewModel()
        {
            Estudiantes = new List<AsignacionEstudiante>();
            Grupos = new List<Grupo>();
        }

        public List<AsignacionEstudiante> Estudiantes { get; set; }
        public List<Grupo> Grupos { get; set; }
        public int FiltroGrupo { get; set; }
        public string Busqueda { get; set; }

        public int SinAsignar { get { return Estudiantes.Count(e => !e.TieneGrupo); } }
    }

    public class DocentesViewModel
    {
        public DocentesViewModel()
        {
            Grupos = new List<Grupo>();
            Docentes = new List<UsuarioSistema>();
            Aulas = new List<Aula>();
            Periodos = new List<int>();
        }

        public List<Grupo> Grupos { get; set; }
        public List<UsuarioSistema> Docentes { get; set; }
        public List<Aula> Aulas { get; set; }
        public List<int> Periodos { get; set; }
        public int Periodo { get; set; }
    }

    public class ReportesViewModel
    {
        public ReportesViewModel()
        {
            Periodos = new List<int>();
        }

        public string Tipo { get; set; }
        public int Periodo { get; set; }
        public List<int> Periodos { get; set; }
        public ReporteResultado Resultado { get; set; }
    }

    public class ImportacionViewModel
    {
        public ImportacionViewModel()
        {
            Filas = new List<FilaImportacion>();
        }

        public string NombreArchivo { get; set; }
        public List<FilaImportacion> Filas { get; set; }

        public bool HayVistaPrevia { get { return Filas.Count > 0; } }
        public int Validas { get { return Filas.Count(f => f.Valida); } }
        public int Duplicadas { get { return Filas.Count(f => f.EsDuplicado); } }
        public int ConErrores { get { return Filas.Count(f => !f.Valida && !f.EsDuplicado); } }
    }

    public class BitacoraViewModel
    {
        public BitacoraViewModel()
        {
            Registros = new List<RegistroBitacora>();
            Incidentes = new List<IncidenteNotificacion>();
        }

        public List<RegistroBitacora> Registros { get; set; }
        public List<IncidenteNotificacion> Incidentes { get; set; }
        public string Desde { get; set; }
        public string Hasta { get; set; }
        public string Tipo { get; set; }

        public static readonly string[] TiposCambio =
        {
            "Asignación", "Traslado", "Retiro", "Cambio de aula", "Cambio de horario",
            "Asignación docente", "Creación de grupo", "Importación"
        };
    }

    public class NotificacionesViewModel
    {
        public NotificacionesViewModel()
        {
            Notificaciones = new List<Notificacion>();
        }

        public List<Notificacion> Notificaciones { get; set; }
        public bool SoloNoLeidas { get; set; }
        public int NoLeidas { get { return Notificaciones.Count(n => !n.Leida); } }
    }
}