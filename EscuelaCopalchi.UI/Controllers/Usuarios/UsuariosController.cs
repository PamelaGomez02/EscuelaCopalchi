
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

        private const string MensajeOk =
            "Se ha guardado correctamente";

        public UsuariosController()
        {
            repository = new UsuarioRepository();
            rolRepository = new RolDropdownRepository();
        }

        // -------------------------------------------
        // CARGAR ROLES
        // -------------------------------------------
        private void CargarRoles()
        {
            ViewBag.Roles = rolRepository.ObtenerTodos();
        }

        // -------------------------------------------
        // REGISTRAR - GET
        // -------------------------------------------
        [HttpGet]
        public ActionResult Registrar()
        {
            CargarRoles();

            return View("~/Views/Usuarios/Crear.cshtml");
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
                            .Select(e => e.ErrorMessage));

                    CargarRoles();

                    return View(
                        "~/Views/Usuarios/Crear.cshtml",
                        usuario);
                }

                string resultado = repository.Guardar(usuario);

                if (resultado == MensajeOk)
                {
                    TempData["Success"] =
                        "Usuario registrado correctamente.";

                    return RedirectToAction("Index");
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
                        .IndexOf(texto,
                            StringComparison.OrdinalIgnoreCase) >= 0

                    || (x.Identificacion ?? "")
                        .IndexOf(texto,
                            StringComparison.OrdinalIgnoreCase) >= 0

                    || (x.Correo ?? "")
                        .IndexOf(texto,
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
                TempData["Error"] =
                    "Usuario no encontrado.";

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

                bool cambiaPass =
                    !string.IsNullOrEmpty(usuario.Contrasena)
                    || !string.IsNullOrEmpty(
                        usuario.ConfirmarContrasena);

                if (cambiaPass)
                {
                    if (string.IsNullOrEmpty(usuario.Contrasena)
                        || usuario.Contrasena.Length < 8)
                    {
                        ViewBag.Error =
                            "La contraseña debe tener al menos 8 caracteres.";

                        CargarRoles();

                        return View(
                            "~/Views/Usuarios/Editar.cshtml",
                            usuario);
                    }

                    if (usuario.Contrasena !=
                        usuario.ConfirmarContrasena)
                    {
                        ViewBag.Error =
                            "Las contraseñas no coinciden.";

                        CargarRoles();

                        return View(
                            "~/Views/Usuarios/Editar.cshtml",
                            usuario);
                    }
                }

                // La contraseña es opcional al editar.
                // No exigirla cuando no se está cambiando.
                ModelState.Remove("Contrasena");
                ModelState.Remove("ConfirmarContrasena");

                if (!ModelState.IsValid)
                {
                    ViewBag.Error =
                        "Revise los datos ingresados.";

                    CargarRoles();

                    return View(
                        "~/Views/Usuarios/Editar.cshtml",
                        usuario);
                }

                // Actualizar información y rol.
                string resultado =
                    repository.Actualizar(usuario);

                // Cambiar contraseña solo si se indicó.
                if (resultado == MensajeOk && cambiaPass)
                {
                    resultado =
                        repository.CambiarContrasena(
                            usuario.IdUsuario,
                            usuario.Contrasena);
                }

                if (resultado == MensajeOk)
                {
                    TempData["Success"] =
                        "Usuario actualizado correctamente.";

                    return RedirectToAction("Index");
                }

                ViewBag.Error = resultado;
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            CargarRoles();

            return View(
                "~/Views/Usuarios/Editar.cshtml",
                usuario);
        }

        // -------------------------------------------
        // DETALLE
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
        // CAMBIAR ESTADO
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

            var usuario = repository.ObtenerPorId(id.Value);

            if (usuario == null)
            {
                TempData["Error"] =
                    "Usuario no encontrado.";

                return RedirectToAction("Index");
            }

            string resultado = repository.CambiarEstado(
                id.Value,
                !usuario.Estado);

            if (resultado == MensajeOk)
            {
                TempData["Success"] =
                    "Estado actualizado correctamente.";
            }
            else
            {
                TempData["Error"] = resultado;
            }

            return RedirectToAction("Index");
        }
    }
}
