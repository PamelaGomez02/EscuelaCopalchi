
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EscuelaCopalchi.UI.Models
{
    public class ConexionBD
    {
        private readonly string connString;
        private readonly DataSet DataSet;

        public ConexionBD()
        {
            var configuracion =
                ConfigurationManager.ConnectionStrings["AulaVirtualDB"];

            if (configuracion == null ||
                string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la cadena de conexión AulaVirtualDB.");
            }

            connString = configuracion.ConnectionString;
            DataSet = new DataSet();
        }

        public string ObtenerConexion()
        {
            return connString;
        }

        public bool validarConexion()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    return conn.State == ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }

        public DataTable ejecutarProcedimiento(
            string procedureName,
            string[] paramNames,
            string[] valueNames)
        {
            DataSet.Tables.Clear();

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand sqlComm = new SqlCommand(procedureName, conn))
            {
                sqlComm.CommandType = CommandType.StoredProcedure;

                if (paramNames != null && valueNames != null)
                {
                    if (paramNames.Length != valueNames.Length)
                    {
                        throw new ArgumentException(
                            "La cantidad de parámetros y valores no coincide.");
                    }

                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        sqlComm.Parameters.AddWithValue(
                            paramNames[i],
                            (object)valueNames[i] ?? DBNull.Value);
                    }
                }

                using (SqlDataAdapter da = new SqlDataAdapter(sqlComm))
                {
                    da.Fill(DataSet);
                }

                if (DataSet.Tables.Count == 0)
                {
                    return new DataTable();
                }

                return DataSet.Tables[0];
            }
        }

        public string ejecutarProcedimiento_Save(
            string procedureName,
            string[] paramNames,
            string[] valueNames)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                using (SqlCommand sqlComm = new SqlCommand(procedureName, conn))
                {
                    sqlComm.CommandType = CommandType.StoredProcedure;

                    if (paramNames != null && valueNames != null)
                    {
                        if (paramNames.Length != valueNames.Length)
                        {
                            throw new ArgumentException(
                                "La cantidad de parámetros y valores no coincide.");
                        }

                        for (int i = 0; i < paramNames.Length; i++)
                        {
                            sqlComm.Parameters.AddWithValue(
                                paramNames[i],
                                (object)valueNames[i] ?? DBNull.Value);
                        }
                    }

                    conn.Open();
                    sqlComm.ExecuteNonQuery();

                    return "Se ha guardado correctamente";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public DataSet EjecutarConsulta(string query)
        {
            DataSet ds = new DataSet();

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
            {
                da.Fill(ds);
            }

            return ds;
        }
    }
}
