using System;
using System.Collections.Generic;
using System.Linq;

namespace EscuelaCopalchi.UI.Models.Monitoreo
{
    // Todos los cálculos del módulo. No toca la base de datos: recibe las filas de
    // SP_MON_Notas_Finales y devuelve promedios, estados y resúmenes.
    public static class MonitoreoCalculo
    {
        public static decimal? Promedio(IEnumerable<decimal?> valores)
        {
            var l = (valores ?? new decimal?[0]).Where(v => v.HasValue).Select(v => v.Value).ToList();
            return l.Count == 0
                ? (decimal?)null
                : Math.Round(l.Average(), 1, MidpointRounding.AwayFromZero);
        }

        // Un registro por estudiante y grupo.
        //  trimestre = 0 -> todos los trimestres;  asignatura vacía -> todas las asignaturas.
        // Los estudiantes sin notas (o sin notas para ese filtro) NO se descartan: salen con
        // Promedio = null y estado "Sin calificaciones".
        public static List<RendimientoEstudiante> ArmarRendimiento(
            IEnumerable<NotaFinalFila> filas, UmbralesRendimiento umbrales,
            int trimestre = 0, string asignatura = null)
        {
            var lista = new List<RendimientoEstudiante>();
            if (filas == null) return lista;
            if (umbrales == null) umbrales = UmbralesRendimiento.Actual();

            bool filtraAsignatura = !string.IsNullOrWhiteSpace(asignatura);

            foreach (var g in filas.GroupBy(f => new { f.IdEstudiante, f.IdGrupo }))
            {
                NotaFinalFila p = g.First();

                var detalle = g.Where(f => f.TieneAsignatura
                                        && (trimestre == 0 || f.Trimestre == trimestre)
                                        && (!filtraAsignatura
                                            || string.Equals(f.Asignatura, asignatura, StringComparison.OrdinalIgnoreCase)))
                               .ToList();

                var conNota = detalle.Where(f => f.NotaFinal.HasValue).ToList();
                decimal? promedio = Promedio(conNota.Select(f => f.NotaFinal));

                lista.Add(new RendimientoEstudiante
                {
                    IdEstudiante = p.IdEstudiante,
                    Identificacion = p.Identificacion,
                    Estudiante = p.Estudiante,
                    IdGrupo = p.IdGrupo,
                    Grupo = p.Grupo,
                    Nivel = p.Nivel,
                    Seccion = p.Seccion,
                    Anio = p.Anio,
                    Promedio = promedio,
                    Estado = umbrales.Clasificar(promedio),
                    NotasUsadas = conNota.Count,
                    Detalle = detalle
                });
            }

            return lista.OrderBy(r => r.Grupo, StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(r => r.Estudiante, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
        }

        // Notas de un estudiante por asignatura (columnas T1, T2, T3 y promedio).
        public static List<NotaPorAsignatura> PorAsignatura(IEnumerable<NotaFinalFila> detalle)
        {
            var res = new List<NotaPorAsignatura>();
            if (detalle == null) return res;

            foreach (var g in detalle.Where(f => f.TieneAsignatura)
                                     .GroupBy(f => f.Asignatura, StringComparer.OrdinalIgnoreCase)
                                     .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                Func<int, decimal?> trim = t =>
                    Promedio(g.Where(f => f.Trimestre == t).Select(f => f.NotaFinal));

                res.Add(new NotaPorAsignatura
                {
                    Asignatura = g.Key,
                    T1 = trim(1),
                    T2 = trim(2),
                    T3 = trim(3),
                    Promedio = Promedio(g.Select(f => f.NotaFinal))
                });
            }
            return res;
        }

        public static ResumenRendimiento Resumir(string etiqueta, IEnumerable<RendimientoEstudiante> estudiantes, int id = 0)
        {
            var l = (estudiantes ?? new RendimientoEstudiante[0]).ToList();

            return new ResumenRendimiento
            {
                Id = id,
                Etiqueta = etiqueta,
                Total = l.Count,
                ConNotas = l.Count(e => e.TieneNotas),
                SinNotas = l.Count(e => !e.TieneNotas),
                Aprobados = l.Count(e => e.Estado == EstadosRendimiento.Aprobado),
                EnRiesgo = l.Count(e => e.Estado == EstadosRendimiento.EnRiesgo),
                Reprobados = l.Count(e => e.Estado == EstadosRendimiento.Reprobado),
                Promedio = Promedio(l.Select(e => e.Promedio))
            };
        }

        // "3" antes que "10"; si no son números, orden alfabético.
        private static int ClaveOrden(string texto, out string resto)
        {
            int n;
            resto = texto ?? "";
            return int.TryParse(resto.Trim(), out n) ? n : int.MaxValue;
        }

        private static IEnumerable<IGrouping<string, RendimientoEstudiante>> Ordenar(
            IEnumerable<IGrouping<string, RendimientoEstudiante>> grupos)
        {
            return grupos.OrderBy(g => { string r; return ClaveOrden(g.Key, out r); })
                         .ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase);
        }

        public static List<ResumenRendimiento> PorGrupo(IEnumerable<RendimientoEstudiante> estudiantes)
        {
            return (estudiantes ?? new RendimientoEstudiante[0])
                .GroupBy(e => e.IdGrupo)
                .Select(g => Resumir(g.First().Grupo, g, g.Key))
                .OrderBy(r => r.Etiqueta, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static List<ResumenRendimiento> PorSeccion(IEnumerable<RendimientoEstudiante> estudiantes)
        {
            return Ordenar((estudiantes ?? new RendimientoEstudiante[0])
                    .GroupBy(e => e.Seccion ?? ""))
                .Select(g => Resumir(g.Key, g))
                .ToList();
        }

        public static List<ResumenRendimiento> PorNivel(IEnumerable<RendimientoEstudiante> estudiantes)
        {
            return Ordenar((estudiantes ?? new RendimientoEstudiante[0])
                    .GroupBy(e => e.Nivel ?? ""))
                .Select(g => Resumir(g.Key, g))
                .ToList();
        }

        // Una fila por materia. Cuenta solo a los estudiantes de grupos que tienen esa materia.
        public static List<ResumenRendimiento> PorMateria(
            IEnumerable<NotaFinalFila> filas, UmbralesRendimiento umbrales, int trimestre = 0)
        {
            var res = new List<ResumenRendimiento>();
            if (filas == null) return res;

            var todas = filas.ToList();

            var materias = todas.Where(f => f.TieneAsignatura)
                                .Select(f => f.Asignatura)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(a => a, StringComparer.CurrentCultureIgnoreCase)
                                .ToList();

            foreach (string materia in materias)
            {
                var gruposConMateria = new HashSet<int>(
                    todas.Where(f => f.TieneAsignatura
                                  && string.Equals(f.Asignatura, materia, StringComparison.OrdinalIgnoreCase))
                         .Select(f => f.IdGrupo));

                var est = ArmarRendimiento(todas.Where(f => gruposConMateria.Contains(f.IdGrupo)),
                                           umbrales, trimestre, materia);

                res.Add(Resumir(materia, est));
            }
            return res;
        }
    }
}