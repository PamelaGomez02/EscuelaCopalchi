using System.Collections.Generic;
using System.Linq;
using EscuelaCopalchi.UI.Datos.GestionAcademica;
using EscuelaCopalchi.UI.Models.GestionAcademica;

namespace EscuelaCopalchi.UI.Logica.GestionAcademica
{
    /// <summary>Consulta de usuarios del módulo y de sus permisos.</summary>
    public class UsuarioService
    {
        /// <summary>Usuarios activos con algún permiso de Gestión Académica (para el selector de usuario).</summary>
        public List<UsuarioSistema> ListarUsuariosDelModulo()
        {
            return ListarConGrupos().Where(u => u.UsaModulo).ToList();
        }

        /// <summary>Docentes activos (tabla DOCENTE) con su cantidad de grupos: son los que pueden recibir grupos.</summary>
        public List<UsuarioSistema> ListarDocentes()
        {
            return ListarConGrupos().Where(u => u.EsDocente).ToList();
        }

        public UsuarioSistema Obtener(int idUsuario)
        {
            return Repositorios.Consulta().Usuarios.Obtener(idUsuario);
        }

        private static List<UsuarioSistema> ListarConGrupos()
        {
            Repositorios datos = Repositorios.Consulta();

            var gruposPorDocente = datos.Grupos.Listar()
                                        .Where(g => g.Estado && g.IdDocente > 0)
                                        .GroupBy(g => g.IdDocente)
                                        .ToDictionary(g => g.Key, g => g.Count());

            List<UsuarioSistema> usuarios = datos.Usuarios.Listar().Where(u => u.Estado).ToList();

            foreach (UsuarioSistema u in usuarios)
            {
                int total;
                u.TotalGrupos = gruposPorDocente.TryGetValue(u.IdDocente, out total) ? total : 0;
            }

            return usuarios;
        }
    }
}