using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>Matrícula de estudiantes en grupos (tabla MATRICULA). La matrícula "Activa" es el grupo actual.</summary>
    public interface IMatriculaRepository
    {
        /// <param name="idGrupo">0 = todos, -1 = solo sin grupo.</param>
        List<AsignacionEstudiante> Listar(int idGrupo = 0, string busqueda = "");

        /// <summary>Estudiante activo con su matrícula actual, o null si no existe o está inactivo.</summary>
        AsignacionEstudiante ObtenerEstudiante(int idEstudiante);

        /// <summary>Estudiantes de un grupo con todos sus datos (para el docente).</summary>
        List<EstudianteGrupo> ListarEstudiantesDelGrupo(int idGrupo);

        /// <summary>Deja al estudiante con matrícula "Activa" en el grupo.</summary>
        void Matricular(int idEstudiante, int idGrupo);

        /// <param name="estado">"Trasladada", "Retirada" o "Finalizada".</param>
        void CambiarEstado(int idMatricula, string estado);
    }

    public class MatriculaRepository : IMatriculaRepository
    {
        private readonly IAccesoDatos db;

        public MatriculaRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public List<AsignacionEstudiante> Listar(int idGrupo = 0, string busqueda = "")
        {
            return Consultar(idGrupo, 0, busqueda).AsEnumerable().Select(MapAsignacion).ToList();
        }

        public AsignacionEstudiante ObtenerEstudiante(int idEstudiante)
        {
            return Consultar(0, idEstudiante, "").AsEnumerable().Select(MapAsignacion).FirstOrDefault();
        }

        public List<EstudianteGrupo> ListarEstudiantesDelGrupo(int idGrupo)
        {
            return Consultar(idGrupo, 0, "").AsEnumerable().Select(r => new EstudianteGrupo
            {
                IdEstudiante = Fila.Entero(r, "id_estudiante"),
                Identificacion = Fila.Texto(r, "identificacion"),
                NombreCompleto = Fila.Texto(r, "nombre_completo"),
                FechaNacimiento = Fila.Fecha(r, "fecha_nacimiento"),
                Direccion = Fila.Texto(r, "direccion"),
                NombreEncargado = Fila.Texto(r, "nombre_encargado"),
                Parentesco = Fila.Texto(r, "parentesco"),
                TelefonoEncargado = Fila.Texto(r, "telefono_encargado"),
                CorreoEncargado = Fila.Texto(r, "correo_encargado"),
                Estado = Fila.Bool(r, "estado"),
                FechaAsignacion = Fila.Fecha(r, "fecha_asignacion")
            }).ToList();
        }

        public void Matricular(int idEstudiante, int idGrupo)
        {
            db.Ejecutar("SP_GA_Matricula_Activar", P("@id_estudiante", idEstudiante), P("@id_grupo", idGrupo));
        }

        public void CambiarEstado(int idMatricula, string estado)
        {
            db.Ejecutar("SP_GA_Matricula_CambiarEstado", P("@id_matricula", idMatricula), P("@estado", estado));
        }

        private DataTable Consultar(int idGrupo, int idEstudiante, string busqueda)
        {
            return db.Consultar("SP_GA_Matricula_Listar",
                P("@id_grupo", idGrupo), P("@id_estudiante", idEstudiante), P("@busqueda", (busqueda ?? "").Trim()));
        }

        private static AsignacionEstudiante MapAsignacion(DataRow r)
        {
            return new AsignacionEstudiante
            {
                IdMatricula = Fila.Entero(r, "id_matricula"),
                IdEstudiante = Fila.Entero(r, "id_estudiante"),
                Identificacion = Fila.Texto(r, "identificacion"),
                NombreCompleto = Fila.Texto(r, "nombre_completo"),
                IdGrupo = Fila.Entero(r, "id_grupo"),
                Grupo = Fila.Texto(r, "grupo"),
                Aula = Fila.Texto(r, "aula"),
                Docente = Fila.Texto(r, "docente"),
                FechaAsignacion = Fila.Fecha(r, "fecha_asignacion")
            };
        }
    }
}