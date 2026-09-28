using System;
using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.GestionAcademica
{
    /// <summary>
    /// Permisos del módulo (tabla PERMISO). Se asignan a cada rol desde Roles → Permisos,
    /// así el módulo no depende del nombre de los roles.
    /// </summary>
    public static class Permisos
    {
        /// <summary>Director: aulas, grupos, asignaciones, importación y bitácora.</summary>
        public const string GestionarAcademica = "GESTIONAR_ACADEMICA";

        /// <summary>Docente: consultar sus grupos, estudiantes, aula y horario.</summary>
        public const string VerMiGrupo = "VER_MI_GRUPO";

        /// <summary>Reportes de distribución (permiso que ya existe en la base).</summary>
        public const string VerReportes = "VER_REPORTES";

        public static readonly string[] DelModulo = { GestionarAcademica, VerMiGrupo, VerReportes };
    }

    /// <summary>Resultado de un procedimiento que modifica datos.</summary>
    public class ResultadoOperacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public int Id { get; set; }

        public static ResultadoOperacion Error(string mensaje)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = mensaje };
        }
    }

    /// <summary>Docente o director (tabla USUARIO).</summary>
    public class UsuarioSistema
    {
        public int IdUsuario { get; set; }

        /// <summary>Id en la tabla DOCENTE (0 si el usuario no está registrado como docente activo).</summary>
        public int IdDocente { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Rol { get; set; }
        public bool Estado { get; set; }
        public int TotalGrupos { get; set; }

        /// <summary>Permisos de su rol (tabla ROL_PERMISO).</summary>
        public List<string> Permisos { get; set; } = new List<string>();

        /// <summary>Está en la tabla DOCENTE: se le pueden asignar grupos.</summary>
        public bool EsDocente { get { return IdDocente > 0; } }

        public bool TienePermiso(string permiso) { return Permisos.Contains(permiso); }

        /// <summary>Tiene al menos un permiso del módulo de Gestión Académica.</summary>
        public bool UsaModulo { get { return Permisos.Any(p => GestionAcademica.Permisos.DelModulo.Contains(p)); } }
    }

    public class ResumenAcademico
    {
        public int Aulas { get; set; }
        public int Grupos { get; set; }
        public int EstudiantesAsignados { get; set; }
        public int DocentesAsignados { get; set; }
    }

    public class Aula
    {
        public int IdAula { get; set; }
        public string Nombre { get; set; }
        public int Capacidad { get; set; }
        public string Ubicacion { get; set; }
        public bool Estado { get; set; }

        /// <summary>Texto con los grupos que usan el aula. Ej: "1-A (28/30), 2-B (20/30)".</summary>
        public string Grupos { get; set; }

        /// <summary>Mayor cantidad de estudiantes entre los grupos que usan el aula.</summary>
        public int OcupacionMaxima { get; set; }

        public bool Completa { get { return Capacidad > 0 && OcupacionMaxima >= Capacidad; } }
    }

    public class Grupo
    {
        public Grupo()
        {
            HorarioDias = new List<HorarioDia>();
        }

        public int IdGrupo { get; set; }
        public string Nombre { get; set; }
        public string Nivel { get; set; }
        public int Periodo { get; set; }
        public int IdAula { get; set; }
        public string Aula { get; set; }
        public int CapacidadAula { get; set; }
        public string Ubicacion { get; set; }

        /// <summary>Cupo máximo del grupo (GRUPO.capacidad).</summary>
        public int Cupo { get; set; }

        /// <summary>Id en la tabla DOCENTE.</summary>
        public int IdDocente { get; set; }

        /// <summary>Id del usuario del docente (para enviarle notificaciones).</summary>
        public int IdUsuarioDocente { get; set; }
        public string Docente { get; set; }
        public int TotalEstudiantes { get; set; }
        public bool Estado { get; set; }

        /// <summary>Cupo real: el menor entre el cupo del grupo y la capacidad del aula.</summary>
        public int Capacidad
        {
            get { return IdAula > 0 && CapacidadAula > 0 ? Math.Min(Cupo, CapacidadAula) : Cupo; }
        }

        /// <summary>Días y horas del grupo (tabla HORARIO).</summary>
        public List<HorarioDia> HorarioDias { get; set; }

        // Datos del formulario al crear/editar: días "1,2,3,4,5" (1 = lunes ... 7 = domingo) y horas "07:00".
        public string Dias { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }

        /// <summary>Horario en texto. Ej: "Lunes a Viernes 07:00 - 12:00".</summary>
        public string Horario { get { return HorarioDia.Texto(HorarioDias); } }

        public DateTime? HorarioActualizado
        {
            get { return HorarioDias.Count == 0 ? (DateTime?)null : HorarioDias.Max(h => h.FechaModificacion); }
        }

        public int EspaciosDisponibles { get { return Math.Max(0, Capacidad - TotalEstudiantes); } }
        public bool TieneCapacidad { get { return IdAula > 0 && TotalEstudiantes < Capacidad; } }

        public int PorcentajeOcupacion
        {
            get { return Capacidad <= 0 ? 0 : (int)Math.Min(100, Math.Round(100.0 * TotalEstudiantes / Capacidad)); }
        }

        public List<int> ListaDias
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Dias)) return new List<int>();
                return Dias.Split(',')
                           .Select(d => { int n; return int.TryParse(d.Trim(), out n) ? n : 0; })
                           .Where(n => n >= 1 && n <= 7)
                           .Distinct()
                           .OrderBy(n => n)
                           .ToList();
            }
        }

        /// <summary>Llena Dias, HoraInicio y HoraFin a partir de HorarioDias (para el formulario de edición).</summary>
        public void CargarDatosHorario()
        {
            Dias = string.Join(",", HorarioDias.Select(h => h.DiaSemana));
            HoraInicio = HorarioDias.Count > 0 ? HorarioDias[0].HoraInicio : "";
            HoraFin = HorarioDias.Count > 0 ? HorarioDias[0].HoraFin : "";
        }
    }

    public class HorarioDia
    {
        private static readonly string[] Nombres =
            { "", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };

        public int IdGrupo { get; set; }
        public int DiaSemana { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }
        public DateTime FechaModificacion { get; set; }

        public string Dia { get { return NombreDia(DiaSemana); } }

        public static string NombreDia(int dia)
        {
            return dia >= 1 && dia <= 7 ? Nombres[dia] : "";
        }

        /// <summary>
        /// "Lunes a Viernes 07:00 - 12:00" si son días seguidos con la misma hora;
        /// si no, cada día por separado: "Lunes 07:00 - 12:00; Miércoles 07:00 - 12:00".
        /// </summary>
        public static string Texto(IEnumerable<HorarioDia> dias)
        {
            var lista = (dias ?? Enumerable.Empty<HorarioDia>()).OrderBy(d => d.DiaSemana).ToList();
            if (lista.Count == 0) return "Sin horario";

            bool mismaHora = lista.All(d => d.HoraInicio == lista[0].HoraInicio && d.HoraFin == lista[0].HoraFin);
            bool seguidos = lista.Last().DiaSemana - lista[0].DiaSemana + 1 == lista.Count;

            if (lista.Count > 1 && mismaHora && seguidos)
                return NombreDia(lista[0].DiaSemana) + " a " + NombreDia(lista.Last().DiaSemana) + " "
                       + lista[0].HoraInicio + " - " + lista[0].HoraFin;

            return string.Join("; ", lista.Select(d => d.Dia + " " + d.HoraInicio + " - " + d.HoraFin));
        }
    }

    /// <summary>Estudiante dentro de un grupo, con datos completos para el docente.</summary>
    public class EstudianteGrupo
    {
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Direccion { get; set; }
        public string NombreEncargado { get; set; }
        public string Parentesco { get; set; }
        public string TelefonoEncargado { get; set; }
        public string CorreoEncargado { get; set; }
        public bool Estado { get; set; }
        public DateTime? FechaAsignacion { get; set; }

        public int? Edad
        {
            get
            {
                if (!FechaNacimiento.HasValue) return null;
                var hoy = DateTime.Today;
                int edad = hoy.Year - FechaNacimiento.Value.Year;
                if (FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;
                return edad;
            }
        }
    }

    /// <summary>Estudiante con su grupo actual (o sin grupo).</summary>
    public class AsignacionEstudiante
    {
        /// <summary>Id de la matrícula activa (0 = sin grupo).</summary>
        public int IdMatricula { get; set; }
        public int IdEstudiante { get; set; }
        public string Identificacion { get; set; }
        public string NombreCompleto { get; set; }
        public int IdGrupo { get; set; }
        public string Grupo { get; set; }
        public string Aula { get; set; }
        public string Docente { get; set; }
        public DateTime? FechaAsignacion { get; set; }

        public bool TieneGrupo { get { return IdGrupo > 0; } }
    }

    /// <summary>Registro de la bitácora (tabla AUDITORIA, módulo "Gestión académica").</summary>
    public class RegistroBitacora
    {
        public long IdAuditoria { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoCambio { get; set; }
        public string Detalle { get; set; }
        public string Responsable { get; set; }
    }

    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public int IdUsuario { get; set; }
        public string Correo { get; set; }
        public string Destinatario { get; set; }
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
        public string EstadoEnvio { get; set; }
    }

    /// <summary>Error al enviar una notificación (tabla AUDITORIA, módulo "Notificaciones").</summary>
    public class IncidenteNotificacion
    {
        public DateTime Fecha { get; set; }
        public string Detalle { get; set; }
    }

    /// <summary>Resultado genérico de un reporte: columnas + filas en texto.</summary>
    public class ReporteResultado
    {
        public ReporteResultado()
        {
            Columnas = new List<string>();
            Filas = new List<string[]>();
        }

        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public int Periodo { get; set; }
        public List<string> Columnas { get; set; }
        public List<string[]> Filas { get; set; }

        public bool TieneDatos { get { return Filas.Count > 0; } }

        public string Subtitulo
        {
            get
            {
                return (Periodo > 0 ? "Período " + Periodo : "Todos los períodos")
                       + " · Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
        }
    }

    /// <summary>Una fila leída del Excel de importación, con su validación.</summary>
    [Serializable]
    public class FilaImportacion
    {
        public FilaImportacion()
        {
            Errores = new List<string>();
        }

        public int NumeroFila { get; set; }
        public string Grupo { get; set; }
        public string Nivel { get; set; }
        public string Periodo { get; set; }
        public string Aula { get; set; }
        public string IdentificacionDocente { get; set; }
        public string NombreDocente { get; set; }

        /// <summary>Días tal como venían en el Excel.</summary>
        public string DiasTexto { get; set; }

        /// <summary>Días normalizados: "1,2,3,4,5".</summary>
        public string Dias { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin { get; set; }

        public bool EsDuplicado { get; set; }
        public List<string> Errores { get; set; }

        public bool Valida { get { return Errores.Count == 0; } }

        public string Estado
        {
            get
            {
                if (Valida) return "Válido";
                return EsDuplicado ? "Duplicado" : "Con errores";
            }
        }
    }

    /// <summary>Usuario con el que se está trabajando (guardado en Session).</summary>
    public class UsuarioSesion
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public List<string> Permisos { get; set; } = new List<string>();

        public bool PuedeGestionar { get { return Permisos.Contains(GestionAcademica.Permisos.GestionarAcademica); } }
        public bool PuedeVerMiGrupo { get { return Permisos.Contains(GestionAcademica.Permisos.VerMiGrupo); } }
        public bool PuedeVerReportes { get { return Permisos.Contains(GestionAcademica.Permisos.VerReportes); } }
    }
}