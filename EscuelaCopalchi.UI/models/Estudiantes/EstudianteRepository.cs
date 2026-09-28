
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EscuelaCopalchi.UI.Models.Estudiantes;


namespace EscuelaCopalchi.UI.Models
{
    public class EstudianteRepository
    {
        private readonly ConexionBD db;

        public EstudianteRepository()
        {
            db = new ConexionBD();
        }


        // Metodo para registar estudiantes
        public string Guardar(Estudiante estudiante)
        {
            string[] parametros =
            {
                "@identificacion",
                "@nombre",
                "@apellido1",
                "@apellido2",
                "@fecha_nacimiento",
                "@direccion",
                "@nombre_encargado",
                "@parentesco",
                "@telefono_encargado",
                "@correo_encargado",
                "@estado"
            };

            string[] valores =
            {
                estudiante.Identificacion,
                estudiante.Nombre,
                estudiante.Apellido1,
                estudiante.Apellido2,
                estudiante.FechaNacimiento.ToString("yyyy-MM-dd"),
                estudiante.Direccion,
                estudiante.NombreEncargado,
                estudiante.Parentesco,
                estudiante.TelefonoEncargado,
                estudiante.CorreoEncargado,
                estudiante.Estado ? "1" : "0"
            };

            return db.ejecutarProcedimiento_Save(
                "SP_InsertarEstudiante",
                parametros,
                valores);
        }


        // Metodo para listar estidiantes
        public List<Estudiante> ObtenerTodos()
        {
            List<Estudiante> lista =
                new List<Estudiante>();

            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_ListarEstudiantes",
                    null,
                    null);

            if (dt == null)
            {
                throw new Exception("DataTable es NULL");
            }

            if (dt.Rows.Count == 0)
            {
                throw new Exception("SP_ListarEstudiantes devolvió 0 registros");
            }

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new Estudiante
                {
                    IdEstudiante = Convert.ToInt32(row["id_estudiante"]),
                    Identificacion = row["identificacion"].ToString(),
                    Nombre = row["nombre"].ToString(),
                    Apellido1 = row["apellido1"].ToString(),
                    Apellido2 = row["apellido2"].ToString(),
                    NombreEncargado = row["nombre_encargado"].ToString(),
                    TelefonoEncargado = row["telefono_encargado"].ToString(),
                    CorreoEncargado = row["correo_encargado"].ToString(),
                    Parentesco = row["parentesco"].ToString(),
                    Estado = Convert.ToBoolean(row["estado"])
                });
            }

            return lista;
        }


    }
}