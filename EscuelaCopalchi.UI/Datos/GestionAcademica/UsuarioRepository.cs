using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EscuelaCopalchi.UI.Models.GestionAcademica;
using static EscuelaCopalchi.UI.Datos.GestionAcademica.Comando;

namespace EscuelaCopalchi.UI.Datos.GestionAcademica
{
    /// <summary>Usuarios con su rol, sus permisos y su id de docente (tablas USUARIO, ROL, ROL_PERMISO y DOCENTE).</summary>
    public interface IUsuarioRepository
    {
        List<UsuarioSistema> Listar();
        UsuarioSistema Obtener(int idUsuario);

        /// <summary>Busca por el id de la tabla DOCENTE.</summary>
        UsuarioSistema ObtenerDocente(int idDocente);
    }

    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly IAccesoDatos db;

        public UsuarioRepository(IAccesoDatos db)
        {
            this.db = db;
        }

        public List<UsuarioSistema> Listar()
        {
            return Mapear(db.Consultar("SP_GA_Usuario_Listar"));
        }

        public UsuarioSistema Obtener(int idUsuario)
        {
            return Mapear(db.Consultar("SP_GA_Usuario_Listar", P("@id_usuario", idUsuario))).FirstOrDefault();
        }

        public UsuarioSistema ObtenerDocente(int idDocente)
        {
            return Mapear(db.Consultar("SP_GA_Usuario_Listar", P("@id_docente", idDocente))).FirstOrDefault();
        }

        private static List<UsuarioSistema> Mapear(DataTable dt)
        {
            return dt.AsEnumerable().Select(r => new UsuarioSistema
            {
                IdUsuario = Fila.Entero(r, "id_usuario"),
                IdDocente = Fila.Entero(r, "id_docente"),
                Identificacion = Fila.Texto(r, "identificacion"),
                NombreCompleto = Fila.Texto(r, "nombre_completo"),
                Correo = Fila.Texto(r, "correo"),
                Telefono = Fila.Texto(r, "telefono"),
                Rol = Fila.Texto(r, "rol"),
                Estado = Fila.Bool(r, "estado"),
                Permisos = Fila.Texto(r, "permisos").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            }).ToList();
        }
    }
}