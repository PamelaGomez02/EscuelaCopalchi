using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>Aulas (tabla AULA).</summary>
    public interface IAulaRepository
    {
        List<Aula> Listar();
        Aula Obtener(int idAula);
        int Insertar(Aula aula);
        void Actualizar(Aula aula);
    }

    public class AulaRepository : IAulaRepository
    {
        private readonly IAccesoDatos db;

        public AulaRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public List<Aula> Listar()
        {
            return Mapear(db.Consultar("SP_GA_Aula_Listar"));
        }

        public Aula Obtener(int idAula)
        {
            return Mapear(db.Consultar("SP_GA_Aula_Listar", P("@id_aula", idAula))).FirstOrDefault();
        }

        public int Insertar(Aula a)
        {
            return System.Convert.ToInt32(db.Escalar("SP_GA_Aula_Insertar",
                P("@nombre", a.Nombre), P("@capacidad", a.Capacidad), P("@ubicacion", a.Ubicacion), P("@estado", a.Estado)));
        }

        public void Actualizar(Aula a)
        {
            db.Ejecutar("SP_GA_Aula_Actualizar",
                P("@id_aula", a.IdAula), P("@nombre", a.Nombre), P("@capacidad", a.Capacidad),
                P("@ubicacion", a.Ubicacion), P("@estado", a.Estado));
        }

        private static List<Aula> Mapear(DataTable dt)
        {
            return dt.AsEnumerable().Select(r => new Aula
            {
                IdAula = Fila.Entero(r, "id_aula"),
                Nombre = Fila.Texto(r, "nombre"),
                Capacidad = Fila.Entero(r, "capacidad"),
                Ubicacion = Fila.Texto(r, "ubicacion"),
                Estado = Fila.Bool(r, "estado")
            }).ToList();
        }
    }
}