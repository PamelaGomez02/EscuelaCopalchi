using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>
    /// Ejecuta procedimientos almacenados. Hay dos implementaciones:
    ///  - AccesoDatos: cada llamada abre y cierra su propia conexión (para consultas).
    ///  - Transaccion: todas las llamadas usan la misma conexión y transacción
    ///    (para operaciones que deben guardarse completas o no guardarse).
    /// Los repositorios reciben cualquiera de las dos, así no les importa cuál es.
    /// </summary>
    public interface IAccesoDatos
    {
        /// <summary>Ejecuta un procedimiento que devuelve filas.</summary>
        DataTable Consultar(string procedimiento, params SqlParameter[] parametros);

        /// <summary>Ejecuta un procedimiento que no devuelve filas.</summary>
        void Ejecutar(string procedimiento, params SqlParameter[] parametros);

        /// <summary>Ejecuta un procedimiento y devuelve la primera columna de la primera fila (ej. el id nuevo).</summary>
        object Escalar(string procedimiento, params SqlParameter[] parametros);
    }

    /// <summary>Acceso sin transacción: una conexión por llamada.</summary>
    public class AccesoDatos : IAccesoDatos
    {
        /// <summary>Cadena "AulaVirtualDB" de Web.config (la misma que usa el módulo de Roles).</summary>
        public static string CadenaConexion
        {
            get
            {
                ConnectionStringSettings cadena = ConfigurationManager.ConnectionStrings["AulaVirtualDB"];

                if (cadena == null || string.IsNullOrWhiteSpace(cadena.ConnectionString))
                    throw new InvalidOperationException("Falta la cadena de conexión \"AulaVirtualDB\" en Web.config.");

                return cadena.ConnectionString;
            }
        }

        public DataTable Consultar(string procedimiento, params SqlParameter[] parametros)
        {
            using (var conexion = new SqlConnection(CadenaConexion))
            {
                conexion.Open();
                return Comando.Consultar(conexion, null, procedimiento, parametros);
            }
        }

        public void Ejecutar(string procedimiento, params SqlParameter[] parametros)
        {
            using (var conexion = new SqlConnection(CadenaConexion))
            {
                conexion.Open();
                Comando.Crear(conexion, null, procedimiento, parametros).ExecuteNonQuery();
            }
        }

        public object Escalar(string procedimiento, params SqlParameter[] parametros)
        {
            using (var conexion = new SqlConnection(CadenaConexion))
            {
                conexion.Open();
                return Comando.Crear(conexion, null, procedimiento, parametros).ExecuteScalar();
            }
        }
    }

    /// <summary>
    /// Transacción: si no se llama a Confirmar() antes de cerrar el bloque using,
    /// todos los cambios se deshacen.
    ///
    ///   using (var tx = new Transaccion())
    ///   {
    ///       var datos = new Repositorios(tx);
    ///       ... varias operaciones ...
    ///       tx.Confirmar();
    ///   }
    /// </summary>
    public sealed class Transaccion : IAccesoDatos, IDisposable
    {
        private readonly SqlConnection conexion;
        private readonly SqlTransaction transaccion;
        private bool confirmada;

        public Transaccion()
        {
            conexion = new SqlConnection(AccesoDatos.CadenaConexion);
            conexion.Open();
            transaccion = conexion.BeginTransaction();
        }

        public DataTable Consultar(string procedimiento, params SqlParameter[] parametros)
        {
            return Comando.Consultar(conexion, transaccion, procedimiento, parametros);
        }

        public void Ejecutar(string procedimiento, params SqlParameter[] parametros)
        {
            Comando.Crear(conexion, transaccion, procedimiento, parametros).ExecuteNonQuery();
        }

        public object Escalar(string procedimiento, params SqlParameter[] parametros)
        {
            return Comando.Crear(conexion, transaccion, procedimiento, parametros).ExecuteScalar();
        }

        public void Confirmar()
        {
            transaccion.Commit();
            confirmada = true;
        }

        public void Dispose()
        {
            if (!confirmada)
            {
                try { transaccion.Rollback(); }
                catch (Exception) { /* la conexión ya pudo haberse cerrado */ }
            }

            transaccion.Dispose();
            conexion.Dispose();
        }
    }

    /// <summary>Utilidades para armar comandos y parámetros.</summary>
    public static class Comando
    {
        public static SqlCommand Crear(SqlConnection conexion, SqlTransaction transaccion,
                                       string procedimiento, SqlParameter[] parametros)
        {
            var cmd = new SqlCommand(procedimiento, conexion, transaccion)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            if (parametros != null)
                cmd.Parameters.AddRange(parametros);

            return cmd;
        }

        public static DataTable Consultar(SqlConnection conexion, SqlTransaction transaccion,
                                          string procedimiento, SqlParameter[] parametros)
        {
            using (var cmd = Crear(conexion, transaccion, procedimiento, parametros))
            using (var adaptador = new SqlDataAdapter(cmd))
            {
                var tabla = new DataTable();
                adaptador.Fill(tabla);
                return tabla;
            }
        }

        /// <summary>Crea un parámetro. null o texto vacío se envían como NULL.</summary>
        public static SqlParameter P(string nombre, object valor)
        {
            if (valor is string && string.IsNullOrWhiteSpace((string)valor))
                valor = null;

            return new SqlParameter(nombre, valor ?? DBNull.Value);
        }

        /// <summary>Para ids opcionales: 0 se envía como NULL.</summary>
        public static SqlParameter Id(string nombre, int valor)
        {
            return new SqlParameter(nombre, valor > 0 ? (object)valor : DBNull.Value);
        }
    }

    /// <summary>Lectura segura de columnas de un DataRow.</summary>
    public static class Fila
    {
        public static string Texto(DataRow r, string columna)
        {
            return r.Table.Columns.Contains(columna) && r[columna] != DBNull.Value ? Convert.ToString(r[columna]) : "";
        }

        public static int Entero(DataRow r, string columna)
        {
            return r.Table.Columns.Contains(columna) && r[columna] != DBNull.Value ? Convert.ToInt32(r[columna]) : 0;
        }

        public static bool Bool(DataRow r, string columna)
        {
            return r.Table.Columns.Contains(columna) && r[columna] != DBNull.Value && Convert.ToBoolean(r[columna]);
        }

        public static DateTime? Fecha(DataRow r, string columna)
        {
            return r.Table.Columns.Contains(columna) && r[columna] != DBNull.Value ? Convert.ToDateTime(r[columna]) : (DateTime?)null;
        }

        /// <summary>Columnas TIME de SQL llegan como TimeSpan: devuelve "07:00".</summary>
        public static string Hora(DataRow r, string columna)
        {
            if (!r.Table.Columns.Contains(columna) || r[columna] == DBNull.Value) return "";
            object valor = r[columna];
            return valor is TimeSpan ? ((TimeSpan)valor).ToString(@"hh\:mm") : Convert.ToString(valor);
        }
    }
}