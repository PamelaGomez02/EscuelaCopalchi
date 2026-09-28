using System.Collections.Generic;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Reglas de asignación de estudiantes a grupos (tabla MATRICULA): asignar, trasladar y retirar.
    /// Cada operación valida la capacidad, queda en la bitácora y notifica a los docentes.
    /// </summary>
    public class AsignacionService
    {
        /// <param name="idGrupo">0 = todos, -1 = solo sin grupo.</param>
        public List<AsignacionEstudiante> Listar(int idGrupo = 0, string busqueda = "")
        {
            return Repositorios.Consulta().Matriculas.Listar(idGrupo, busqueda);
        }

        public List<EstudianteGrupo> ListarEstudiantesDelGrupo(int idGrupo)
        {
            return Repositorios.Consulta().Matriculas.ListarEstudiantesDelGrupo(idGrupo);
        }

        /// <summary>Asigna un estudiante a un grupo, o lo traslada si ya tenía uno.</summary>
        public ResultadoOperacion Asignar(int idEstudiante, int idGrupo, string motivo, int idResponsable)
        {
            return Operacion.Ejecutar(() =>
            {
                using (var tx = new Transaccion())
                {
                    var datos = new Repositorios(tx);

                    // ---------- Validaciones ----------
                    AsignacionEstudiante estudiante = datos.Matriculas.ObtenerEstudiante(idEstudiante);
                    if (estudiante == null)
                        throw new ReglaNegocioException("El estudiante no existe o está inactivo.");

                    Grupo destino = datos.Grupos.Obtener(idGrupo);
                    if (destino == null || !destino.Estado)
                        throw new ReglaNegocioException("El grupo seleccionado no existe o está inactivo.");

                    if (destino.IdAula == 0)
                        throw new ReglaNegocioException("El grupo " + destino.Nombre + " no tiene un aula asignada.");

                    if (estudiante.IdGrupo == destino.IdGrupo)
                        throw new ReglaNegocioException("El estudiante ya pertenece al grupo " + destino.Nombre + ".");

                    // Aula sin capacidad (o grupo sin cupo): se impide la operación
                    if (destino.TotalEstudiantes >= destino.Capacidad)
                    {
                        string limite = destino.Capacidad == destino.CapacidadAula
                            ? "El aula " + destino.Aula + " no tiene capacidad disponible"
                            : "El grupo " + destino.Nombre + " ya alcanzó su cupo";

                        throw new ReglaNegocioException(limite + " (" + destino.TotalEstudiantes + "/" + destino.Capacidad +
                                                        "). No se realizó la asignación.");
                    }

                    Grupo origen = estudiante.IdGrupo > 0 ? datos.Grupos.Obtener(estudiante.IdGrupo) : null;

                    // ---------- Cambio ----------
                    if (estudiante.IdMatricula > 0)
                        datos.Matriculas.CambiarEstado(estudiante.IdMatricula, "Trasladada");

                    datos.Matriculas.Matricular(idEstudiante, idGrupo);

                    // ---------- Bitácora ----------
                    string detalle = origen == null
                        ? estudiante.NombreCompleto + ": ingreso al grupo " + destino.Nombre + " (" + destino.Aula + ")."
                        : estudiante.NombreCompleto + ": grupo " + origen.Nombre + " → " + destino.Nombre +
                          " (" + Texto(origen.Aula, "sin aula") + " → " + destino.Aula + ").";

                    datos.Auditoria.Registrar(Modulos.GestionAcademica,
                        origen == null ? TiposCambio.Asignacion : TiposCambio.Traslado,
                        "MATRICULA", idEstudiante, detalle + Motivo(motivo), idResponsable);

                    // ---------- Notificaciones ----------
                    if (destino.IdUsuarioDocente > 0)
                        datos.Notificaciones.Crear(destino.IdUsuarioDocente, TiposNotificacion.Incorporacion, "Nuevo estudiante en su grupo",
                            "El estudiante " + estudiante.NombreCompleto + " se incorporó al grupo " + destino.Nombre +
                            " (" + destino.Aula + ")." + (origen != null ? " Proviene del grupo " + origen.Nombre + "." : ""));

                    if (origen != null && origen.IdUsuarioDocente > 0)
                        datos.Notificaciones.Crear(origen.IdUsuarioDocente, TiposNotificacion.RetiroEstudiante, "Estudiante retirado de su grupo",
                            "El estudiante " + estudiante.NombreCompleto + " fue trasladado del grupo " + origen.Nombre +
                            " al grupo " + destino.Nombre + ".");

                    tx.Confirmar();

                    return origen == null
                        ? "Estudiante asignado al grupo " + destino.Nombre + "."
                        : "Estudiante trasladado de " + origen.Nombre + " a " + destino.Nombre + ".";
                }
            });
        }

        /// <summary>Retira a un estudiante de su grupo actual (matrícula "Retirada").</summary>
        public ResultadoOperacion Retirar(int idEstudiante, string motivo, int idResponsable)
        {
            return Operacion.Ejecutar(() =>
            {
                using (var tx = new Transaccion())
                {
                    var datos = new Repositorios(tx);

                    AsignacionEstudiante estudiante = datos.Matriculas.ObtenerEstudiante(idEstudiante);
                    if (estudiante == null || estudiante.IdMatricula == 0)
                        throw new ReglaNegocioException("El estudiante no tiene un grupo asignado.");

                    Grupo grupo = datos.Grupos.Obtener(estudiante.IdGrupo);

                    datos.Matriculas.CambiarEstado(estudiante.IdMatricula, "Retirada");

                    datos.Auditoria.Registrar(Modulos.GestionAcademica, TiposCambio.Retiro, "MATRICULA", idEstudiante,
                        estudiante.NombreCompleto + ": retiro del grupo " + estudiante.Grupo + "." + Motivo(motivo), idResponsable);

                    if (grupo != null && grupo.IdUsuarioDocente > 0)
                        datos.Notificaciones.Crear(grupo.IdUsuarioDocente, TiposNotificacion.RetiroEstudiante, "Estudiante retirado de su grupo",
                            "El estudiante " + estudiante.NombreCompleto + " fue retirado del grupo " + estudiante.Grupo + "." + Motivo(motivo));

                    tx.Confirmar();
                    return "Estudiante retirado del grupo " + estudiante.Grupo + ".";
                }
            });
        }

        private static string Motivo(string motivo)
        {
            return string.IsNullOrWhiteSpace(motivo) ? "" : " Motivo: " + motivo.Trim();
        }

        private static string Texto(string valor, string siVacio)
        {
            return string.IsNullOrEmpty(valor) ? siVacio : valor;
        }
    }
}