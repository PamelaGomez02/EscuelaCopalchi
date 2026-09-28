using System;
using System.Data;
using System.Data.SqlClient;

namespace EscuelaCopalchi.UI.Models
{
    public class ConexionBD
    {
        private string connString;
        private string connString_Adrian = @"Server=localhost\SQLEXPRESSADRIAN;Database=AulaVirtualCopalchi;Trusted_Connection=True;";
        private string connString_Pame;
        private string connString_Ema;
        private string connString_Fran = @"Server=localhost;Database=AulaVirtualCopalchi;Integrated Security=True;TrustServerCertificate=True;";
        private DataSet DataSet;

        public ConexionBD()
        {
            this.connString = connString_Fran;

            this.DataSet = new DataSet();
        }
        public string ObtenerConexion()
        {
            return connString;
        }

        public bool validarConexion()
        {
            try   
            {
                using (SqlConnection conn =
                       new SqlConnection(connString))
                {
                    conn.Open();

                    return conn.State ==
                           ConnectionState.Open;
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

            using (SqlConnection conn =
                   new SqlConnection(connString))
            {
                conn.Open();

                try
                {
                    SqlCommand sqlComm =
                        new SqlCommand(procedureName, conn);

                    sqlComm.CommandType =
                        CommandType.StoredProcedure;

                    if (paramNames != null &&
                        valueNames != null)
                    {
                        for (int i = 0; i < paramNames.Length; i++)
                        {
                            sqlComm.Parameters.AddWithValue(
                                paramNames[i],
                                valueNames[i]);
                        }
                    }

                    SqlDataAdapter da =
                        new SqlDataAdapter(sqlComm);

                    da.Fill(DataSet);

                    return DataSet.Tables[0];
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Error al ejecutar el procedimiento: "
                        + ex.Message);
                }
            }
        }


        public string ejecutarProcedimiento_Save(
            string procedureName,
            string[] paramNames,
            string[] valueNames)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                try
                {
                    SqlCommand sqlComm =
                        new SqlCommand(procedureName, conn);

                    sqlComm.CommandType =
                        CommandType.StoredProcedure;

                    for (int i = 0; i < paramNames.Length; i++)
                    {
                        sqlComm.Parameters.AddWithValue(
                            paramNames[i],
                            valueNames[i]);
                    }

                    sqlComm.ExecuteNonQuery();

                    return "Se ha guardado correctamente";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }


    }
}