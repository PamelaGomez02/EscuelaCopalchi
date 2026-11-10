using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace EscuelaCopalchi.UI.Models.Reportes
{
    public class CartaItem
    {
        public int IdCarta { get; set; }
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; }
        public string Tipo { get; set; }
        public string Destino { get; set; }
        public string Archivo { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class TrasladoViewModel
    {
        public string Buscar { get; set; }
        public List<EstudianteRendimiento> Estudiantes { get; set; }
        public List<CartaItem> Cartas { get; set; }
        public string Director { get; set; }

        public TrasladoViewModel()
        {
            Estudiantes = new List<EstudianteRendimiento>();
            Cartas = new List<CartaItem>();
        }
    }

    public class CartaRepository
    {
        private readonly ConexionBD db = new ConexionBD();

        public string Guardar(int idUsuario, int idEstudiante, string tipo, string destino, string motivo, string archivo, byte[] contenido)
        {
            return db.ejecutarProcedimiento_Save("SP_RP_Carta_Guardar",
                new[] { "@id_estudiante", "@tipo", "@escuela_destino", "@motivo", "@nombre_archivo", "@contenido_b64", "@id_usuario" },
                new[]
                {
                    idEstudiante.ToString(CultureInfo.InvariantCulture), tipo, destino ?? "", motivo ?? "", archivo,
                    Convert.ToBase64String(contenido), idUsuario.ToString(CultureInfo.InvariantCulture)
                });
        }

        public List<CartaItem> Listar(int idEstudiante)
        {
            var lista = new List<CartaItem>();
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Carta_Listar",
                new[] { "@id_estudiante" }, new[] { idEstudiante.ToString(CultureInfo.InvariantCulture) });
            if (dt == null) return lista;

            foreach (DataRow r in dt.Rows)
            {
                lista.Add(new CartaItem
                {
                    IdCarta = Convert.ToInt32(r["id_carta"]),
                    IdEstudiante = Convert.ToInt32(r["id_estudiante"]),
                    Estudiante = Convert.ToString(r["estudiante"]),
                    Tipo = Convert.ToString(r["tipo"]),
                    Destino = r["escuela_destino"] == DBNull.Value ? "" : Convert.ToString(r["escuela_destino"]),
                    Archivo = Convert.ToString(r["nombre_archivo"]),
                    Usuario = r["usuario"] == DBNull.Value ? "" : Convert.ToString(r["usuario"]),
                    Fecha = Convert.ToDateTime(r["fecha"])
                });
            }
            return lista;
        }

        // null si no existe
        public byte[] Obtener(int idCarta, out string nombre)
        {
            nombre = null;
            DataTable dt = db.ejecutarProcedimiento("SP_RP_Carta_Obtener",
                new[] { "@id_carta" }, new[] { idCarta.ToString(CultureInfo.InvariantCulture) });
            if (dt == null || dt.Rows.Count == 0) return null;

            nombre = Convert.ToString(dt.Rows[0]["nombre_archivo"]);
            return (byte[])dt.Rows[0]["contenido"];
        }
    }
}
