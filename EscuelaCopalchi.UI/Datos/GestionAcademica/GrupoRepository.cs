using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>Grupos y sus horarios (tablas GRUPO y HORARIO).</summary>
    public interface IGrupoRepository
    {
        /// <summary>Grupos con aula, docente, total de estudiantes y horario. 0 = sin filtro.</summary>
        /// <param name="anio">Año del curso lectivo.</param>
        /// <param name="idDocente">Id de la tabla DOCENTE.</param>
        List<Grupo> Listar(int anio = 0, int idDocente = 0);
        Grupo Obtener(int idGrupo);
        int Insertar(Grupo grupo);
        void Actualizar(Grupo grupo);

        /// <summary>Borra el horario del grupo y guarda los días indicados con la misma hora.</summary>
        void ReemplazarHorario(int idGrupo, IEnumerable<int> dias, TimeSpan? horaInicio, TimeSpan? horaFin);
    }

    public class GrupoRepository : IGrupoRepository
    {
        private readonly IAccesoDatos db;

        public GrupoRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public List<Grupo> Listar(int anio = 0, int idDocente = 0)
        {
            List<Grupo> grupos = Mapear(db.Consultar("SP_GA_Grupo_Listar",
                P("@anio", anio), P("@id_docente", idDocente)));

            AgregarHorarios(grupos, 0);
            return grupos;
        }

        public Grupo Obtener(int idGrupo)
        {
            Grupo grupo = Mapear(db.Consultar("SP_GA_Grupo_Listar", P("@id_grupo", idGrupo))).FirstOrDefault();

            if (grupo != null)
                AgregarHorarios(new List<Grupo> { grupo }, idGrupo);

            return grupo;
        }

        public int Insertar(Grupo g)
        {
            return Convert.ToInt32(db.Escalar("SP_GA_Grupo_Insertar",
                P("@nombre", g.Nombre), P("@nivel", g.Nivel), P("@anio", g.Periodo), P("@capacidad", g.Cupo),
                P("@horario", HorarioTexto(g)), Id("@id_aula", g.IdAula), Id("@id_docente", g.IdDocente)));
        }

        public void Actualizar(Grupo g)
        {
            db.Ejecutar("SP_GA_Grupo_Actualizar",
                P("@id_grupo", g.IdGrupo), P("@nombre", g.Nombre), P("@nivel", g.Nivel), P("@anio", g.Periodo),
                P("@capacidad", g.Cupo), P("@horario", HorarioTexto(g)),
                Id("@id_aula", g.IdAula), Id("@id_docente", g.IdDocente), P("@estado", g.Estado));
        }

        /// <summary>Texto para GRUPO.horario (lo leen otros módulos). Máximo 150 caracteres.</summary>
        private static string HorarioTexto(Grupo g)
        {
            string texto = g.HorarioDias.Count == 0 ? null : g.Horario;
            return texto != null && texto.Length > 150 ? texto.Substring(0, 150) : texto;
        }

        public void ReemplazarHorario(int idGrupo, IEnumerable<int> dias, TimeSpan? horaInicio, TimeSpan? horaFin)
        {
            db.Ejecutar("SP_GA_Horario_Eliminar", P("@id_grupo", idGrupo));

            if (!horaInicio.HasValue || !horaFin.HasValue) return;

            foreach (int dia in dias)
            {
                db.Ejecutar("SP_GA_Horario_Insertar",
                    P("@id_grupo", idGrupo), P("@dia_semana", (byte)dia),
                    P("@hora_inicio", horaInicio.Value), P("@hora_fin", horaFin.Value));
            }
        }

        /// <summary>Carga los horarios con una sola consulta y los reparte entre los grupos.</summary>
        private void AgregarHorarios(List<Grupo> grupos, int idGrupo)
        {
            if (grupos.Count == 0) return;

            var horarios = db.Consultar("SP_GA_Horario_Listar", P("@id_grupo", idGrupo))
                             .AsEnumerable()
                             .Select(r => new HorarioDia
                             {
                                 IdGrupo = Fila.Entero(r, "id_grupo"),
                                 DiaSemana = Fila.Entero(r, "dia_semana"),
                                 HoraInicio = Fila.Hora(r, "hora_inicio"),
                                 HoraFin = Fila.Hora(r, "hora_fin"),
                                 FechaModificacion = Fila.Fecha(r, "fecha_modificacion") ?? DateTime.MinValue
                             })
                             .ToLookup(h => h.IdGrupo);

            foreach (Grupo g in grupos)
            {
                g.HorarioDias = horarios[g.IdGrupo].OrderBy(h => h.DiaSemana).ToList();
                g.CargarDatosHorario();
            }
        }

        private static List<Grupo> Mapear(DataTable dt)
        {
            return dt.AsEnumerable().Select(r => new Grupo
            {
                IdGrupo = Fila.Entero(r, "id_grupo"),
                Nombre = Fila.Texto(r, "nombre"),
                Nivel = Fila.Texto(r, "nivel"),
                Periodo = Fila.Entero(r, "anio"),
                Cupo = Fila.Entero(r, "cupo"),
                Estado = Fila.Bool(r, "estado"),
                IdAula = Fila.Entero(r, "id_aula"),
                Aula = Fila.Texto(r, "aula"),
                CapacidadAula = Fila.Entero(r, "capacidad_aula"),
                Ubicacion = Fila.Texto(r, "ubicacion"),
                IdDocente = Fila.Entero(r, "id_docente"),
                IdUsuarioDocente = Fila.Entero(r, "id_usuario_docente"),
                Docente = Fila.Texto(r, "docente"),
                TotalEstudiantes = Fila.Entero(r, "total_estudiantes")
            }).ToList();
        }
    }
}