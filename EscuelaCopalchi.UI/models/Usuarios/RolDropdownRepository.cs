using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

namespace EscuelaCopalchi.UI.Models.Usuarios
{
    public class RolDropdownRepository
    {
        private readonly ConexionBD db;

        public RolDropdownRepository()
        {
            db = new ConexionBD();
        }

        public List<RolDropdownItem> ObtenerTodos()
        {
            List<RolDropdownItem> lista = new List<RolDropdownItem>();

            DataTable dt = db.ejecutarProcedimiento(
                "SP_ListarRoles_Dropdown", null, null);

            if (dt == null)
            {
                throw new Exception("DataTable es NULL");
            }

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new RolDropdownItem
                {
                    IdRol = Convert.ToInt32(row["id_rol"]),
                    Nombre = row["nombre"].ToString()
                });
            }

            return lista;
        }
    }
}
