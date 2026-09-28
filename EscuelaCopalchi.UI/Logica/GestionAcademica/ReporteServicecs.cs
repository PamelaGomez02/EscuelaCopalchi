using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>Reportes de distribución de estudiantes por aula, grupo o docente.</summary>
    public class ReporteService
    {
        private readonly GrupoService grupos = new GrupoService();

        public static string TipoValido(string tipo)
        {
            tipo = (tipo ?? "").ToUpperInvariant();
            return tipo == "GRUPO" || tipo == "DOCENTE" ? tipo : "AULA";
        }

        /// <param name="tipo">AULA, GRUPO o DOCENTE.</param>
        /// <param name="periodo">0 = todos.</param>
        public ReporteResultado Generar(string tipo, int periodo)
        {
            tipo = TipoValido(tipo);
            List<Grupo> activos = grupos.Listar(periodo).Where(g => g.Estado).ToList();

            var reporte = new ReporteResultado { Tipo = tipo, Periodo = periodo };

            if (tipo == "GRUPO")
            {
                reporte.Titulo = "Distribución de estudiantes por grupo";
                reporte.Columnas.AddRange(new[] { "Grupo", "Nivel", "Período", "Aula", "Docente", "Estudiantes", "Capacidad", "Espacios disponibles" });

                foreach (Grupo g in activos.OrderByDescending(g => g.Periodo).ThenBy(g => g.Nombre))
                    reporte.Filas.Add(new[]
                    {
                        g.Nombre, g.Nivel, Num(g.Periodo),
                        string.IsNullOrEmpty(g.Aula) ? "Sin aula" : g.Aula,
                        string.IsNullOrEmpty(g.Docente) ? "Sin docente" : g.Docente,
                        Num(g.TotalEstudiantes), Num(g.Capacidad), Num(g.EspaciosDisponibles)
                    });
            }
            else if (tipo == "DOCENTE")
            {
                reporte.Titulo = "Distribución de estudiantes por docente";
                reporte.Columnas.AddRange(new[] { "Docente", "Grupos", "Detalle de grupos", "Aulas", "Estudiantes" });

                foreach (var d in activos.Where(g => g.IdDocente > 0).GroupBy(g => new { g.IdDocente, g.Docente }).OrderBy(d => d.Key.Docente))
                    reporte.Filas.Add(new[]
                    {
                        d.Key.Docente,
                        Num(d.Count()),
                        string.Join(", ", d.Select(g => g.Nombre).OrderBy(n => n)),
                        string.Join(", ", d.Where(g => g.IdAula > 0).Select(g => g.Aula).Distinct().OrderBy(n => n)),
                        Num(d.Sum(g => g.TotalEstudiantes))
                    });
            }
            else
            {
                reporte.Titulo = "Distribución de estudiantes por aula";
                reporte.Columnas.AddRange(new[] { "Aula", "Capacidad", "Grupos", "Detalle de grupos", "Estudiantes", "Ocupación" });

                foreach (var a in activos.Where(g => g.IdAula > 0).GroupBy(g => new { g.IdAula, g.Aula, g.Capacidad }).OrderBy(a => a.Key.Aula))
                {
                    int estudiantes = a.Sum(g => g.TotalEstudiantes);
                    int capacidadTotal = a.Key.Capacidad * a.Count();

                    reporte.Filas.Add(new[]
                    {
                        a.Key.Aula,
                        Num(a.Key.Capacidad),
                        Num(a.Count()),
                        string.Join(", ", a.Select(g => g.Nombre).OrderBy(n => n)),
                        Num(estudiantes),
                        (capacidadTotal == 0 ? 0 : (int)Math.Round(100.0 * estudiantes / capacidadTotal)) + "%"
                    });
                }
            }

            return reporte;
        }

        public byte[] ExportarExcel(ReporteResultado reporte)
        {
            return ExcelHelper.Generar("Reporte", reporte.Titulo, reporte.Subtitulo, reporte.Columnas, reporte.Filas);
        }

        public byte[] ExportarPdf(ReporteResultado reporte)
        {
            return PdfHelper.GenerarTabla(reporte.Titulo, reporte.Subtitulo, reporte.Columnas, reporte.Filas);
        }

        private static string Num(int n)
        {
            return n.ToString(CultureInfo.InvariantCulture);
        }
    }
}