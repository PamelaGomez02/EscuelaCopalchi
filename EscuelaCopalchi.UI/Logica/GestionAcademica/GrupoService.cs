using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>
    /// Reglas de los grupos: creación, edición, horario, aula y docente responsable.
    /// Cada cambio queda en la bitácora y se notifica al docente.
    /// </summary>
    public class GrupoService
    {
        // ================================================================
        // Consultas
        // ================================================================

        /// <param name="periodo">Año del curso lectivo. 0 = todos.</param>
        public List<Grupo> Listar(int periodo = 0)
        {
            return Repositorios.Consulta().Grupos.Listar(periodo);
        }

        public List<Grupo> ListarActivos()
        {
            return Listar().Where(g => g.Estado).ToList();
        }

        /// <summary>Grupos activos a cargo de un docente. Lista vacía = sin grupos asignados.</summary>
        /// <param name="idDocente">Id de la tabla DOCENTE (0 = el usuario no está registrado como docente).</param>
        public List<Grupo> ListarDelDocente(int idDocente)
        {
            if (idDocente <= 0) return new List<Grupo>();
            return Repositorios.Consulta().Grupos.Listar(0, idDocente).Where(g => g.Estado).ToList();
        }

        /// <summary>Períodos con grupos, más el año actual, del más reciente al más antiguo.</summary>
        public List<int> ListarPeriodos()
        {
            return Listar().Select(g => g.Periodo)
                           .Union(new[] { DateTime.Today.Year })
                           .OrderByDescending(p => p)
                           .ToList();
        }

        public ResumenAcademico ObtenerResumen()
        {
            Repositorios datos = Repositorios.Consulta();
            List<Grupo> activos = datos.Grupos.Listar().Where(g => g.Estado).ToList();

            return new ResumenAcademico
            {
                Aulas = datos.Aulas.Listar().Count(a => a.Estado),
                Grupos = activos.Count,
                EstudiantesAsignados = activos.Sum(g => g.TotalEstudiantes),
                DocentesAsignados = activos.Where(g => g.IdDocente > 0).Select(g => g.IdDocente).Distinct().Count()
            };
        }

        // ================================================================
        // Guardar
        // ================================================================

        /// <summary>Crea (IdGrupo = 0) o actualiza un grupo, en una sola transacción.</summary>
        public ResultadoOperacion Guardar(Grupo grupo, int idResponsable)
        {
            return Operacion.Ejecutar(() =>
            {
                using (var tx = new Transaccion())
                {
                    string mensaje = GuardarEnTransaccion(new Repositorios(tx), grupo, idResponsable);
                    tx.Confirmar();
                    return mensaje;
                }
            });
        }

        /// <summary>Asigna el docente responsable de un grupo (idDocente = id de DOCENTE; 0 lo quita).</summary>
        public ResultadoOperacion AsignarDocente(int idGrupo, int idDocente, int idResponsable)
        {
            ResultadoOperacion resultado = Operacion.Ejecutar(() =>
            {
                using (var tx = new Transaccion())
                {
                    var datos = new Repositorios(tx);

                    Grupo grupo = datos.Grupos.Obtener(idGrupo);
                    if (grupo == null)
                        throw new ReglaNegocioException("El grupo no existe.");

                    grupo.IdDocente = idDocente;
                    GuardarEnTransaccion(datos, grupo, idResponsable);

                    tx.Confirmar();
                    return idDocente > 0 ? "Docente asignado correctamente." : "Se quitó el docente del grupo.";
                }
            });

            return resultado;
        }

        /// <summary>
        /// Lógica de guardar un grupo usando una transacción que ya está abierta.
        /// También la usa la importación desde Excel.
        /// </summary>
        internal string GuardarEnTransaccion(Repositorios datos, Grupo g, int idResponsable)
        {
            // ---------- Validaciones ----------
            g.Nombre = (g.Nombre ?? "").Trim();
            g.Nivel = (g.Nivel ?? "").Trim();

            if (g.Nombre == "")
                throw new ReglaNegocioException("El nombre del grupo es obligatorio.");

            if (g.Nombre.Length > 100)
                throw new ReglaNegocioException("El nombre del grupo admite máximo 100 caracteres.");

            if (g.Nivel.Length > 50)
                throw new ReglaNegocioException("El nivel admite máximo 50 caracteres.");

            if (g.Periodo < 2000 || g.Periodo > 2100)
                throw new ReglaNegocioException("El período (año) no es válido.");

            List<int> dias = g.ListaDias;
            TimeSpan? horaInicio = LeerHora(g.HoraInicio);
            TimeSpan? horaFin = LeerHora(g.HoraFin);

            if (dias.Count == 0)
            {
                horaInicio = null;
                horaFin = null;
            }
            else if (!horaInicio.HasValue || !horaFin.HasValue)
            {
                throw new ReglaNegocioException("Debe indicar la hora de inicio y la hora final del horario.");
            }
            else if (horaFin <= horaInicio)
            {
                throw new ReglaNegocioException("La hora final debe ser mayor que la hora de inicio.");
            }

            bool nombreRepetido = datos.Grupos.Listar(g.Periodo)
                .Any(x => x.IdGrupo != g.IdGrupo && string.Equals(x.Nombre, g.Nombre, StringComparison.OrdinalIgnoreCase));

            if (nombreRepetido)
                throw new ReglaNegocioException("Ya existe un grupo " + g.Nombre + " en el período " + g.Periodo + ".");

            Aula aula = null;
            if (g.IdAula > 0)
            {
                aula = datos.Aulas.Obtener(g.IdAula);
                if (aula == null || !aula.Estado)
                    throw new ReglaNegocioException("El aula seleccionada no existe o está inactiva.");
            }

            UsuarioSistema docente = null;
            if (g.IdDocente > 0)
            {
                docente = datos.Usuarios.ObtenerDocente(g.IdDocente);
                if (docente == null || !docente.EsDocente || !docente.Estado)
                    throw new ReglaNegocioException("El docente seleccionado no existe o no está activo en la tabla de docentes.");
            }

            // Cupo del grupo: si no se indica, se usa la capacidad del aula
            if (g.Cupo <= 0)
                g.Cupo = aula != null ? aula.Capacidad : 0;

            if (g.Cupo <= 0)
                throw new ReglaNegocioException("Indique el cupo del grupo o asígnele un aula.");

            if (aula != null && g.Cupo > aula.Capacidad)
                throw new ReglaNegocioException("El cupo del grupo (" + g.Cupo + ") supera la capacidad del aula " +
                                                aula.Nombre + " (" + aula.Capacidad + ").");

            g.HorarioDias = dias.Select(d => new HorarioDia
            {
                DiaSemana = d,
                HoraInicio = FormatoHora(horaInicio),
                HoraFin = FormatoHora(horaFin)
            }).ToList();

            string horarioNuevo = g.Horario;
            string nombreAula = aula != null ? aula.Nombre : null;
            string nombreDocente = docente != null ? docente.NombreCompleto : null;

            // ---------- Grupo nuevo ----------
            if (g.IdGrupo == 0)
            {
                g.Estado = true;
                int id = datos.Grupos.Insertar(g);
                datos.Grupos.ReemplazarHorario(id, dias, horaInicio, horaFin);

                Registrar(datos, TiposCambio.CreacionGrupo, id, idResponsable,
                    "Grupo " + g.Nombre + " (" + g.Periodo + ") · Aula: " + Valor(nombreAula, "sin aula") +
                    " · Docente: " + Valor(nombreDocente, "sin docente") + " · Horario: " + horarioNuevo + ".");

                if (docente != null)
                    NotificarNuevaAsignacion(datos, docente.IdUsuario, g, nombreAula, horarioNuevo);

                return "Grupo registrado correctamente.";
            }

            // ---------- Edición ----------
            Grupo anterior = datos.Grupos.Obtener(g.IdGrupo);
            if (anterior == null)
                throw new ReglaNegocioException("El grupo no existe.");

            if (aula != null && anterior.TotalEstudiantes > aula.Capacidad)
                throw new ReglaNegocioException("El aula " + aula.Nombre + " no tiene capacidad suficiente: el grupo tiene " +
                                                anterior.TotalEstudiantes + " estudiantes y el aula admite " + aula.Capacidad + ".");

            if (anterior.TotalEstudiantes > g.Cupo)
                throw new ReglaNegocioException("El cupo no puede ser menor que los " + anterior.TotalEstudiantes +
                                                " estudiantes que ya tiene el grupo.");

            g.Estado = anterior.Estado;
            datos.Grupos.Actualizar(g);

            bool cambioHorario = anterior.Dias != string.Join(",", dias)
                                 || anterior.HoraInicio != FormatoHora(horaInicio)
                                 || anterior.HoraFin != FormatoHora(horaFin);

            if (cambioHorario)
                datos.Grupos.ReemplazarHorario(g.IdGrupo, dias, horaInicio, horaFin);

            bool mismoDocente = anterior.IdDocente == g.IdDocente;

            // Cambio de aula
            if (anterior.IdAula != g.IdAula)
            {
                Registrar(datos, TiposCambio.CambioAula, g.IdGrupo, idResponsable,
                    "Grupo " + g.Nombre + ": " + Valor(anterior.Aula, "sin aula") + " → " + Valor(nombreAula, "sin aula") + ".");

                if (docente != null && mismoDocente)
                    datos.Notificaciones.Crear(docente.IdUsuario, TiposNotificacion.CambioAula, "Cambio de aula",
                        "El grupo " + g.Nombre + " cambió de aula: " + Valor(anterior.Aula, "sin aula") +
                        " → " + Valor(nombreAula, "sin aula") + ".");
            }

            // Cambio de docente
            if (!mismoDocente)
            {
                Registrar(datos, TiposCambio.AsignacionDocente, g.IdGrupo, idResponsable,
                    "Grupo " + g.Nombre + ": docente " + Valor(anterior.Docente, "sin docente") + " → " +
                    Valor(nombreDocente, "sin docente") + ".");

                if (docente != null)
                    NotificarNuevaAsignacion(datos, docente.IdUsuario, g, nombreAula, horarioNuevo);

                if (anterior.IdUsuarioDocente > 0)
                    datos.Notificaciones.Crear(anterior.IdUsuarioDocente, TiposNotificacion.RetiroGrupo, "Grupo reasignado",
                        "Ya no es el docente responsable del grupo " + g.Nombre + ".");
            }

            // Cambio de horario
            if (cambioHorario)
            {
                Registrar(datos, TiposCambio.CambioHorario, g.IdGrupo, idResponsable,
                    "Grupo " + g.Nombre + ": " + anterior.Horario + " → " + horarioNuevo + ".");

                if (docente != null && mismoDocente)
                    datos.Notificaciones.Crear(docente.IdUsuario, TiposNotificacion.CambioHorario, "Horario actualizado",
                        "El horario del grupo " + g.Nombre + " cambió a: " + horarioNuevo + ".");
            }

            return "Grupo actualizado correctamente.";
        }

        private static void Registrar(Repositorios datos, string tipo, int idGrupo, int idResponsable, string detalle)
        {
            datos.Auditoria.Registrar(Modulos.GestionAcademica, tipo, "GRUPO", idGrupo, detalle, idResponsable);
        }

        private static void NotificarNuevaAsignacion(Repositorios datos, int idDocente, Grupo g, string aula, string horario)
        {
            datos.Notificaciones.Crear(idDocente, TiposNotificacion.NuevaAsignacion, "Nuevo grupo asignado",
                "Se le asignó el grupo " + g.Nombre + " (período " + g.Periodo + ")" +
                (aula == null ? "." : " en el aula " + aula + ".") +
                " Horario: " + horario + ".");
        }

        // ================================================================
        // Utilidades
        // ================================================================

        private static TimeSpan? LeerHora(string texto)
        {
            TimeSpan hora;
            return TimeSpan.TryParseExact((texto ?? "").Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" },
                                          CultureInfo.InvariantCulture, out hora)
                ? hora
                : (TimeSpan?)null;
        }

        private static string FormatoHora(TimeSpan? hora)
        {
            return hora.HasValue ? hora.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";
        }

        private static string Valor(string texto, string siVacio)
        {
            return string.IsNullOrEmpty(texto) ? siVacio : texto;
        }
    }
}