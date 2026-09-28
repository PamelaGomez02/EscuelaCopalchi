using System.Collections.Generic;
using System.Linq;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>Reglas de las aulas.</summary>
    public class AulaService
    {
        /// <summary>Aulas con los grupos que las usan y su ocupación.</summary>
        public List<Aula> Listar()
        {
            Repositorios datos = Repositorios.Consulta();
            List<Grupo> grupos = datos.Grupos.Listar().Where(g => g.Estado).ToList();
            List<Aula> aulas = datos.Aulas.Listar();

            foreach (Aula aula in aulas)
            {
                List<Grupo> delAula = grupos.Where(g => g.IdAula == aula.IdAula).OrderBy(g => g.Nombre).ToList();

                aula.Grupos = string.Join(", ", delAula.Select(g => g.Nombre + " (" + g.TotalEstudiantes + "/" + aula.Capacidad + ")"));
                aula.OcupacionMaxima = delAula.Count == 0 ? 0 : delAula.Max(g => g.TotalEstudiantes);
            }

            return aulas;
        }

        public List<Aula> ListarActivas()
        {
            return Repositorios.Consulta().Aulas.Listar().Where(a => a.Estado).ToList();
        }

        /// <summary>Crea (IdAula = 0) o actualiza un aula.</summary>
        public ResultadoOperacion Guardar(Aula aula)
        {
            return Operacion.Ejecutar(() =>
            {
                aula.Nombre = (aula.Nombre ?? "").Trim();
                aula.Ubicacion = (aula.Ubicacion ?? "").Trim();

                if (aula.Nombre == "")
                    throw new ReglaNegocioException("El nombre del aula es obligatorio.");

                if (aula.Nombre.Length > 50)
                    throw new ReglaNegocioException("El nombre del aula admite máximo 50 caracteres.");

                if (aula.Capacidad <= 0)
                    throw new ReglaNegocioException("La capacidad debe ser mayor a cero.");

                Repositorios datos = Repositorios.Consulta();

                bool nombreRepetido = datos.Aulas.Listar()
                    .Any(a => a.IdAula != aula.IdAula && string.Equals(a.Nombre, aula.Nombre, System.StringComparison.OrdinalIgnoreCase));

                if (nombreRepetido)
                    throw new ReglaNegocioException("Ya existe un aula con ese nombre.");

                if (aula.IdAula == 0)
                {
                    datos.Aulas.Insertar(aula);
                    return "Aula registrada correctamente.";
                }

                if (datos.Aulas.Obtener(aula.IdAula) == null)
                    throw new ReglaNegocioException("El aula no existe.");

                // No se puede bajar la capacidad por debajo de un grupo que ya usa el aula
                Grupo masGrande = datos.Grupos.Listar()
                                       .Where(g => g.Estado && g.IdAula == aula.IdAula)
                                       .OrderByDescending(g => g.TotalEstudiantes)
                                       .FirstOrDefault();

                if (masGrande != null && masGrande.TotalEstudiantes > aula.Capacidad)
                    throw new ReglaNegocioException("No se puede reducir la capacidad a " + aula.Capacidad + ": el grupo " +
                                                    masGrande.Nombre + " tiene " + masGrande.TotalEstudiantes + " estudiantes en esta aula.");

                datos.Aulas.Actualizar(aula);
                return "Aula actualizada correctamente.";
            });
        }
    }
}