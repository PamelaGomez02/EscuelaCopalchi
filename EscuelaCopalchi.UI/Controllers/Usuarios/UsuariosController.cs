
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Usuarios;
using System;
using System.Linq;
using System.Web.Mvc;

namespace EscuelaCopalchi.UI.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UsuarioRepository repository;
        private readonly RolDropdownRepository rolRepository;

        public UsuariosController()
        {
            repository = new UsuarioRepository();
            rolRepository = new RolDropdownRepository();
        }

        // -------------------------------------------
        // MÉTODOS AUXILIARES
        // -------------------------------------------

        private void CargarRoles()
        {
            ViewBag.Roles = rolRepository.ObtenerTodos();
        }

        private static bool EsExito(string resultado)
        {
            if (string.IsNullOrWhiteSpace(resultado))
                return false;

            string texto = resultado.Trim();

            return texto.Equals(
                    "Se ha guardado correctamente",
                    StringComparison.OrdinalIgnoreCase)
                || texto.Equals(
                    "Usuario registrado correctamente.",
                    StringComparison.OrdinalIgnoreCase)
                || texto.Equals(
                    "Usuario actualizado correctamente.",
                    StringComparison.OrdinalIgnoreCase)
                || texto.Equals(
                    "Usuario activado correctamente.",
                    StringComparison.OrdinalIgnoreCase)
                || texto.Equals(
                    "Usuario desactivado correctamente.",
                    StringComparison.OrdinalIgnoreCase);
        }

        private ActionResult MostrarEditarConError(
            Usuario usuario,
            string mensaje)
        {
            ViewBag.Error = mensaje;

            // Recuperar el estado real de SQL Server.
            if (usuario != null && usuario.IdUsuario > 0)
            {
                try
                {
                    var usuarioActual =
                        repository.ObtenerPorId(usuario.IdUsuario);

                    if (usuarioActual != null)
                        usuario.Estado = usuarioActual.Estado;
                }
                catch
                {
                    // Mantener el error original.
                }
            }

            CargarRoles();

            return View(
                "~/Views/Usuarios/Editar.cshtml",
                usuario);
        }

        // -------------------------------------------
        // REGISTRAR - GET
        // -------------------------------------------

        [HttpGet]
        public ActionResult Registrar()
        {
            CargarRoles();

            return View(
                "~/Views/Usuarios/Crear.cshtml",
                new Usuario { Estado = true });
        }

        // -------------------------------------------
        // REGISTRAR - POST
        // -------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(Usuario usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Error = string.Join(
                        " ",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .Where(x => !string.IsNullOrWhiteSpace(x)));

                    CargarRoles();

                    return View(
                        "~/Views/Usuarios/Crear.cshtml",
                        usuario);
                }

                // Registrar usuario con su rol.
                string resultado = repository.Guardar(usuario);

                if (EsExito(resultado))
                {
                    TempData["Success"] =
                        "Usuario registrado correctamente.";

                    return RedirectToAction("Index", "Usuarios");
                }

                ViewBag.Error = resultado;
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            CargarRoles();

            return View(
                "~/Views/Usuarios/Crear.cshtml",
                usuario);
        }

        // -------------------------------------------
        // LISTADO DE USUARIOS
        // -------------------------------------------

        [HttpGet]
        public ActionResult Index(
            string filtroEstado = "Todos",
            string busqueda = "")
        {
            var usuarios = repository.ObtenerTodos();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string texto = busqueda.Trim();

                usuarios = usuarios.Where(x =>
                    ((x.Nombre ?? "") + " " +
                     (x.Apellido1 ?? "") + " " +
                     (x.Apellido2 ?? ""))
                        .IndexOf(
                            texto,
                            StringComparison.OrdinalIgnoreCase) >= 0

                    || (x.Identificacion ?? "")
                        .IndexOf(
                            texto,
                            StringComparison.OrdinalIgnoreCase) >= 0

                    || (x.Correo ?? "")
                        .IndexOf(
                            texto,
                            StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            if (filtroEstado == "Activos")
            {
                usuarios = usuarios
                    .Where(x => x.Estado)
                    .ToList();
            }
            else if (filtroEstado == "Inactivos")
            {
                usuarios = usuarios
                    .Where(x => !x.Estado)
                    .ToList();
            }

            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.Busqueda = busqueda;

            return View(
                "~/Views/Usuarios/Index.cshtml",
                usuarios);
        }

        // -------------------------------------------
        // EDITAR - GET
        // -------------------------------------------

        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un usuario para editar.";

                return RedirectToAction("Index");
            }

            var usuario = repository.ObtenerPorId(id.Value);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";

                return RedirectToAction("Index");
            }

            CargarRoles();

            return View(
                "~/Views/Usuarios/Editar.cshtml",
                usuario);
        }

        // -------------------------------------------
        // EDITAR - POST
        // -------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Usuario usuario)
        {
            if (usuario == null || usuario.IdUsuario <= 0)
            {
                TempData["Error"] =
                    "No se pudo identificar el usuario.";

                return RedirectToAction("Index");
            }

            try
            {
                var usuarioActual =
                    repository.ObtenerPorId(usuario.IdUsuario);

                if (usuarioActual == null)
                {
                    TempData["Error"] =
                        "Usuario no encontrado.";

                    return RedirectToAction("Index");
                }

                // El estado no se cambia desde Editar.
                // Se conserva el valor real de SQL Server.
                usuario.Estado = usuarioActual.Estado;

                bool cambiaPass =
                    !string.IsNullOrEmpty(usuario.Contrasena)
                    || !string.IsNullOrEmpty(
                        usuario.ConfirmarContrasena);

                // La contraseña es opcional al editar.
                ModelState.Remove("Contrasena");
                ModelState.Remove("ConfirmarContrasena");
                ModelState.Remove("Estado");

                if (cambiaPass)
                {
                    if (string.IsNullOrEmpty(usuario.Contrasena)
                        || usuario.Contrasena.Length < 8)
                    {
                        return MostrarEditarConError(
                            usuario,
                            "La contraseña debe tener al menos 8 caracteres.");
                    }

                    if (usuario.Contrasena !=
                        usuario.ConfirmarContrasena)
                    {
                        return MostrarEditarConError(
                            usuario,
                            "Las contraseñas no coinciden.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    return MostrarEditarConError(
                        usuario,
                        "Revise los datos ingresados.");
                }

                // Actualizar datos personales y rol.
                string resultado =
                    repository.Actualizar(usuario);

                if (!EsExito(resultado))
                {
                    return MostrarEditarConError(
                        usuario,
                        resultado);
                }

                // Cambiar contraseña únicamente si
                // el usuario indicó una nueva.
                if (cambiaPass)
                {
                    string resultadoPass =
                        repository.CambiarContrasena(
                            usuario.IdUsuario,
                            usuario.Contrasena);

                    if (!EsExito(resultadoPass))
                    {
                        return MostrarEditarConError(
                            usuario,
                            "Los datos se guardaron, pero no se confirmó " +
                            "el cambio de contraseña: " + resultadoPass);
                    }
                }

                // Confirmación de actualización.
                TempData["Success"] =
                    "Usuario actualizado correctamente.";

                // REGRESAR AUTOMÁTICAMENTE AL LISTADO.
                return RedirectToAction("Index", "Usuarios");
            }
            catch (Exception ex)
            {
                return MostrarEditarConError(
                    usuario,
                    ex.Message);
            }
        }

        // -------------------------------------------
        // DETALLE DE USUARIO
        // -------------------------------------------

        [HttpGet]
        public ActionResult Detalle(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "Debe seleccionar un usuario.";

                return RedirectToAction("Index");
            }

            var usuario = repository.ObtenerPorId(id.Value);

            if (usuario == null)
            {
                TempData["Error"] =
                    "Usuario no encontrado.";

                return RedirectToAction("Index");
            }

            return View(
                "~/Views/Usuarios/Detalle.cshtml",
                usuario);
        }

        // -------------------------------------------
        // ACTIVAR / DESACTIVAR USUARIO
        // -------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                TempData["Error"] =
                    "No se recibió el ID del usuario.";

                return RedirectToAction("Index");
            }

            try
            {
                var usuario =
                    repository.ObtenerPorId(id.Value);

                if (usuario == null)
                {
                    TempData["Error"] =
                        "Usuario no encontrado.";

                    return RedirectToAction("Index");
                }

                // Invertir el estado actual.
                bool nuevoEstado = !usuario.Estado;

                string resultado =
                    repository.CambiarEstado(
                        id.Value,
                        nuevoEstado);

                if (EsExito(resultado))
                {
                    TempData["Success"] = nuevoEstado
                        ? "Usuario activado correctamente."
                        : "Usuario desactivado correctamente.";
                }
                else
                {
                    TempData["Error"] = resultado;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index", "Usuarios");
        }
    }
}
