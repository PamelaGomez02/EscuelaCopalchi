using System;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using EscuelaCopalchi.UI.Models.Estudiantes;
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


        public Estudiante ObtenerPorId(int id)
        {
            string[] parametros =
            {
        "@id_estudiante"
    };

            string[] valores =
            {
        id.ToString()
    };

            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_ObtenerEstudiantePorId",
                    parametros,
                    valores);

            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];

            return new Estudiante
            {
                IdEstudiante =
                    Convert.ToInt32(row["id_estudiante"]),

                Identificacion =
                    row["identificacion"].ToString(),

                Nombre =
                    row["nombre"].ToString(),

                Apellido1 =
                    row["apellido1"].ToString(),

                Apellido2 =
                    row["apellido2"].ToString(),

                FechaNacimiento =
                    Convert.ToDateTime(row["fecha_nacimiento"]),

                Direccion =
                    row["direccion"].ToString(),

                NombreEncargado =
                    row["nombre_encargado"].ToString(),

                TelefonoEncargado =
                    row["telefono_encargado"].ToString(),

                CorreoEncargado =
                    row["correo_encargado"].ToString(),

                Parentesco =
                    row["parentesco"].ToString(),

                Estado =
                    Convert.ToBoolean(row["estado"]),

                /*FechaRegistro =
                    Convert.ToDateTime(row["fecha_registro"])*/
            };
        }


        public string Actualizar(Estudiante estudiante)
        {
            string[] parametros =
            {
                "@id_estudiante",
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
                estudiante.IdEstudiante.ToString(),
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
                "SP_ActualizarEstudiante",
                parametros,
                valores);
        }


        public string DarBaja(int idEstudiante, string observacionBaja)
        {
            string[] parametros =
            {
                "@id_estudiante",
                "@observacion_baja"
            };

            string[] valores =
            {
                idEstudiante.ToString(),
                observacionBaja
            };

            return db.ejecutarProcedimiento_Save(
                "SP_DarBajaEstudiante",
                parametros,
                valores);
        }

        public string GuardarAdecuacion(
            AdecuacionAcademica adecuacion)
            {
                string[] parametros =
                {
                    "@id_estudiante",
                    "@id_usuario_registro",
                    "@tipo_adecuacion",
                    "@descripcion",
                    "@medidas",
                    "@fecha_inicio",
                    "@fecha_fin",
                    "@estado"
                };

                string[] valores =
                {
                    adecuacion.IdEstudiante.ToString(),
                    adecuacion.IdUsuarioRegistro.ToString(),
                    adecuacion.TipoAdecuacion.ToString(),
                    adecuacion.Descripcion,
                    adecuacion.Medidas ?? "",
                    adecuacion.FechaInicio.ToString("yyyy-MM-dd"),
                    adecuacion.FechaFin.HasValue
                        ? adecuacion.FechaFin.Value.ToString("yyyy-MM-dd")
                        : "",
                    adecuacion.Estado
                };

                return db.ejecutarProcedimiento_Save(
                    "SP_GUARDAR_ADECUACION_ACADEMICA",
                    parametros,
                    valores);
            }


        public List<TipoAdecuacion> ObtenerTiposAdecuacion()
        {
            List<TipoAdecuacion> lista =
                new List<TipoAdecuacion>();

            DataSet ds =
                db.EjecutarConsulta(
                    @"SELECT
                id_tipo,
                nombre_adecuacion,
                descripcion,
                objetivo
              FROM TIPO_ADECUACION
              ORDER BY nombre_adecuacion");

            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    lista.Add(new TipoAdecuacion
                    {
                        IdTipo =
                            Convert.ToInt32(row["id_tipo"]),

                        NombreAdecuacion =
                            row["nombre_adecuacion"].ToString(),

                        Descripcion =
                            row["descripcion"].ToString(),

                        Objetivo =
                            row["objetivo"].ToString()
                    });
                }
            }

            return lista;
        }

    public List<AdecuacionAcademica> ObtenerAdecuaciones(
        int idEstudiante)
        {
        string[] parametros =
        {
            "@id_estudiante"
        };

        string[] valores =
        {
            idEstudiante.ToString()
        };

            String NombreTipoAdecuacion = "drec";

        DataTable dt =
        db.ejecutarProcedimiento(
            "SP_OBTENER_ADECUACIONES_ESTUDIANTE",
            parametros,
            valores);

            List<AdecuacionAcademica> lista =
                new List<AdecuacionAcademica>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new AdecuacionAcademica
                {
                    IdAdecuacion =
                        Convert.ToInt32(row["id_adecuacion"]),

                    IdEstudiante =
                        Convert.ToInt32(row["id_estudiante"]),

                    TipoAdecuacion =
                        Convert.ToInt32(row["tipo_adecuacion"]),

                    NombreTipoAdecuacion =
                    row["nombre_adecuacion"].ToString(),

                    Descripcion =
                        row["descripcion"].ToString(),

                    Medidas =
                        row["medidas"].ToString(),

                    FechaInicio =
                        Convert.ToDateTime(row["fecha_inicio"]),

                    FechaFin =
                        row["fecha_fin"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(row["fecha_fin"]),

                    Estado =
                        row["estado"].ToString(),

                    FechaRegistro =
                        Convert.ToDateTime(row["fecha_registro"])
                });
            }

            return lista;
        }



        public string ActualizarAdecuacion(
        AdecuacionAcademica adecuacion)
        {
            string[] parametros =
            {
                "@id_adecuacion",
                "@tipo_adecuacion",
                "@descripcion",
                "@medidas",
                "@fecha_inicio",
                "@fecha_fin",
                "@estado"
            };

             string[] valores =
             {
                adecuacion.IdAdecuacion.ToString(),
                adecuacion.TipoAdecuacion.ToString(),
                adecuacion.Descripcion,
                adecuacion.Medidas ?? "",
                adecuacion.FechaInicio.ToString("yyyy-MM-dd"),
                adecuacion.FechaFin.HasValue
                    ? adecuacion.FechaFin.Value.ToString("yyyy-MM-dd")
                    : "",
                adecuacion.Estado
            };

                return db.ejecutarProcedimiento_Save(
                    "SP_ACTUALIZAR_ADECUACION_ACADEMICA",
                    parametros,
                    valores);
        }


        public List<Alergia> ObtenerAlergias(
        int idEstudiante)
        {
            string[] parametros =
            {
                "@id_estudiante"
            };

            string[] valores =
            {
                idEstudiante.ToString()
            };

            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_OBTENER_ALERGIAS",
                    parametros,
                    valores);

            List<Alergia> lista =
                new List<Alergia>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new Alergia
                {
                    IdAlergia =
                        Convert.ToInt32(row["id_alergia"]),

                    IdEstudiante =
                        Convert.ToInt32(row["id_estudiante"]),

                    NombreAlergia =
                        row["alergia"].ToString(),

                    Descripcion =
                        row["descripcion"].ToString(),

                    FechaRegistro =
                        Convert.ToDateTime(row["fecha_registro"])
                });
            }

            return lista;
        }





        public string GuardarAlergia(
        Alergia alergia)
            {
                string[] parametros =
                {
                    "@id_estudiante",
                    "@alergia",
                    "@descripcion"
                };

                string[] valores =
                {
                    alergia.IdEstudiante.ToString(),
                    alergia.NombreAlergia,
                    alergia.Descripcion
                };

            return db.ejecutarProcedimiento_Save(
                "SP_GUARDAR_ALERGIA",
                parametros,
                valores);
        }


        public string ActualizarAlergia(
        Alergia alergia)
        {
            string[] parametros =
            {
                "@id_alergia",
                "@alergia",
                "@descripcion"
            };

            string[] valores =
            {
                alergia.IdAlergia.ToString(),
                alergia.NombreAlergia,
                alergia.Descripcion
            };

            return db.ejecutarProcedimiento_Save(
                "SP_ACTUALIZAR_ALERGIA",
                parametros,
                valores);
        }

        public string EliminarAlergia(
        int idAlergia)
        {
            string[] parametros =
            {
                "@id_alergia"
            };

            string[] valores =
            {
                idAlergia.ToString()
            };

            return db.ejecutarProcedimiento_Save(
                "SP_ELIMINAR_ALERGIA",
                parametros,
                valores);
        }

        public string GuardarObservacion(
        Observaciones observacion)
        {
            string[] parametros =
            {
                "@id_estudiante",
                "@titulo",
                "@observacion"
            };

            string[] valores =
            {
                observacion.IdEstudiante.ToString(),
                observacion.Titulo,
                observacion.Observacion
            };

            return db.ejecutarProcedimiento_Save(
                "SP_GUARDAR_OBSERVACION",
                parametros,
                valores);
        }


        public List<Observaciones> ObtenerObservaciones(int idEstudiante)
        {
            string[] parametros =
            {
                "@id_estudiante"
            };

            string[] valores =
            {
                idEstudiante.ToString()
            };

            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_OBTENER_OBSERVACIONES",
                    parametros,
                    valores);

            List<Observaciones> lista =
                new List<Observaciones>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(
                    new Observaciones
                    {
                        IdObservacion =
                            Convert.ToInt32(
                                row["id_observacion"]),

                        IdEstudiante =
                            Convert.ToInt32(
                                row["id_estudiante"]),

                        Titulo =
                            row["titulo"].ToString(),

                        Observacion =
                            row["observacion"].ToString(),

                        FechaRegistro =
                            Convert.ToDateTime(
                                row["fecha_registro"])
                    });
            }

            return lista;
        }


        public string ActualizarObservacion(Observaciones observacion)
        {
            string[] parametros =
            {
                "@id_observacion",
                "@titulo",
                "@observacion"
            };

            string[] valores =
            {
                observacion.IdObservacion.ToString(),
                observacion.Titulo,
                observacion.Observacion
            };

            return db.ejecutarProcedimiento_Save(
                "SP_ACTUALIZAR_OBSERVACION",
                parametros,
                valores);
        }

        public string EliminarObservacion(
        int idObservacion)
        {
            string[] parametros =
                    {
                "@id_observacion"
            };

            string[] valores =
            {
                idObservacion.ToString()
            };

            return db.ejecutarProcedimiento_Save(
                "SP_ELIMINAR_OBSERVACION",
                parametros,
                valores);
        }


        public List<Estudiante>ObtenerHistorialBajas()
        {
            DataTable dt =
                db.ejecutarProcedimiento(
                    "SP_OBTENER_HISTORIAL_BAJAS",
                    new string[] { },
                    new string[] { });

            List<Estudiante> lista =
                new List<Estudiante>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new Estudiante
                {
                    IdEstudiante =
                        Convert.ToInt32(
                            row["id_estudiante"]),

                    Identificacion =
                        row["identificacion"].ToString(),

                    Nombre =
                        row["nombre"].ToString(),

                    Apellido1 =
                        row["apellido1"].ToString(),

                    Apellido2 =
                        row["apellido2"].ToString(),

                    NombreEncargado =
                        row["nombre_encargado"].ToString(),

                    FechaBaja =
                        row["fecha_baja"] == DBNull.Value
                            ? (DateTime?)null
                            : Convert.ToDateTime(
                                row["fecha_baja"]),

                    ObservacionBaja =
                        row["observacion_baja"].ToString(),

                    Estado =
                        Convert.ToBoolean(
                            row["estado"])
                });
            }

            return lista;
        }

        public bool ExisteIdentificacion(
        string identificacion)
        {
            return ObtenerTodos()
                .Any(x =>
                    x.Identificacion ==
                    identificacion);
        }

    }
}