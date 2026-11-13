using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using EscuelaCopalchi.UI.Models.Auditoria;

namespace EscuelaCopalchi.UI.Models.Auditoria
{
    public class AuditoriaRepository
    {
        private readonly string conexion;
        public AuditoriaRepository()
        {
            var cs = ConfigurationManager.ConnectionStrings["AulaVirtualDB"];
            if (cs == null) throw new ConfigurationErrorsException("Falta AulaVirtualDB en Web.config.");
            conexion = cs.ConnectionString;
        }

        public AuditoriaListadoVM Consultar(AuditoriaFiltro f)
        {
            var vm = new AuditoriaListadoVM { Filtro = f };
            using (var cn = new SqlConnection(conexion))
            using (var cmd = new SqlCommand("dbo.SP_AS_Auditoria_Consultar", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = (object)f.IdUsuario ?? DBNull.Value;
                cmd.Parameters.Add("@modulo", SqlDbType.NVarChar, 100).Value = (object)f.Modulo ?? DBNull.Value;
                cmd.Parameters.Add("@accion", SqlDbType.NVarChar, 100).Value = (object)f.Accion ?? DBNull.Value;
                cmd.Parameters.Add("@desde", SqlDbType.Date).Value = (object)f.Desde ?? DBNull.Value;
                cmd.Parameters.Add("@hasta", SqlDbType.Date).Value = (object)f.Hasta ?? DBNull.Value;
                cmd.Parameters.Add("@pagina", SqlDbType.Int).Value = f.Pagina;
                cmd.Parameters.Add("@tamano_pagina", SqlDbType.Int).Value = f.TamanoPagina;
                cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) vm.Total = Convert.ToInt64(r["total"]);
                    if (r.NextResult())
                        while (r.Read()) vm.Registros.Add(Mapear(r));
                }
            }
            return vm;
        }

        public AuditoriaRegistro Obtener(long id)
        {
            using (var cn = new SqlConnection(conexion))
            using (var cmd = new SqlCommand("dbo.SP_AS_Auditoria_Detalle", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_auditoria", SqlDbType.BigInt).Value = id;
                cn.Open();
                using (var r = cmd.ExecuteReader()) return r.Read() ? Mapear(r) : null;
            }
        }

        private static string S(IDataRecord r, string col)
        {
            var v = r[col];
            return v == DBNull.Value ? null : Convert.ToString(v);
        }
        private static AuditoriaRegistro Mapear(IDataRecord r)
        {
            return new AuditoriaRegistro
            {
                IdAuditoria = Convert.ToInt64(r["id_auditoria"]),
                IdUsuario = r["id_usuario"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["id_usuario"]),
                Responsable = S(r, "responsable"), Rol = S(r, "rol"),
                Accion = S(r, "accion"), Modulo = S(r, "modulo"),
                TablaAfectada = S(r, "tabla_afectada"), RegistroAfectado = S(r, "registro_afectado"),
                Descripcion = S(r, "descripcion"), DireccionIp = S(r, "direccion_ip"),
                Fecha = Convert.ToDateTime(r["fecha"]), Resultado = S(r, "resultado"),
                RegistroIncompleto = Convert.ToBoolean(r["registro_incompleto"])
            };
        }
    }
}
