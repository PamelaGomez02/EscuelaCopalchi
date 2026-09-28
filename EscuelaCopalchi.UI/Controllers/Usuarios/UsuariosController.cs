using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Usuarios;

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

        // ---------------------------------------------------
        // REGISTRAR
        // ---------------------------------------------------
        [HttpGet]
        public ActionResult Registrar()
        {
            ViewBag.Roles = rolRepository.ObtenerTodos();
            return View("~/Views/Usuarios/Crear.cshtml");
        }

        [HttpPost]
        public ActionResult Registrar(Usuario usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errores = string.Join(
                        "<br>",
                        ModelState.Values
                                  .SelectMany(v => v.Errors)
                                  .Select(e => e.ErrorMessage));

                    ViewBag.Error = errores;
                    ViewBag.Roles = rolRepository.ObtenerTodos();
                    return View("~/Views/Usuarios/Crear.cshtml", usuario);
                }

                string resultado = repository.Guardar(usuario);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] = "Usuario registrado correctamente.";
                    return RedirectToAction("Registrar");
                }

                ViewBag.Error = resultado;
                ViewBag.Roles = rolRepository.ObtenerTodos();
                return View("~/Views/Usuarios/Crear.cshtml", usuario);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Roles = rolRepository.ObtenerTodos();
                return View("~/Views/Usuarios/Crear.cshtml", usuario);
            }
        }

        // ---------------------------------------------------
        // INDEX
        // ---------------------------------------------------
        public ActionResult Index(
            string filtroEstado = "Todos",
            string busqueda = "")
        {
            var usuarios = repository.ObtenerTodos();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                usuarios = usuarios
                    .Where(x =>
                        (x.Nombre + " " + x.Apellido1 + " " + x.Apellido2)
                        .ToLower().Contains(busqueda.ToLower())
                        || x.Identificacion.ToLower().Contains(busqueda.ToLower())
                        || x.Correo.ToLower().Contains(busqueda.ToLower()))
                    .ToList();
            }

            if (filtroEstado == "Activos")
            {
                usuarios = usuarios.Where(x => x.Estado).ToList();
            }
            else if (filtroEstado == "Inactivos")
            {
                usuarios = usuarios.Where(x => !x.Estado).ToList();
            }

            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.Busqueda = busqueda;

            return View("~/Views/Usuarios/Index.cshtml", usuarios);
        }

        // ---------------------------------------------------
        // EDITAR
        // ---------------------------------------------------
        [HttpGet]
        public ActionResult Editar(int id)
        {
            var usuario = repository.ObtenerPorId(id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = rolRepository.ObtenerTodos();
            return View("~/Views/Usuarios/Editar.cshtml", usuario);
        }

        [HttpPost]
        public ActionResult Editar(Usuario usuario)
        {
            try
            {
                string resultado = repository.Actualizar(usuario);

                if (resultado == "Se ha guardado correctamente")
                {
                    TempData["Success"] = "Usuario actualizado correctamente.";
                    return RedirectToAction("Index");
                }

                ViewBag.Error = resultado;
                ViewBag.Roles = rolRepository.ObtenerTodos();
                return View("~/Views/Usuarios/Editar.cshtml", usuario);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Roles = rolRepository.ObtenerTodos();
                return View("~/Views/Usuarios/Editar.cshtml", usuario);
            }
        }

        // ---------------------------------------------------
        // DETALLE
        // ---------------------------------------------------
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            var usuario = repository.ObtenerPorId(id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            return View("~/Views/Usuarios/Detalle.cshtml", usuario);
        }

        // ---------------------------------------------------
        // CAMBIAR ESTADO
        // ---------------------------------------------------
        [HttpGet]
        public ActionResult CambiarEstado(int id)
        {
            var usuario = repository.ObtenerPorId(id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            string resultado = repository.CambiarEstado(id, !usuario.Estado);

            TempData["Success"] = resultado == "Se ha guardado correctamente"
                ? "Estado actualizado correctamente."
                : resultado;

            return RedirectToAction("Index");
        }
    }
}
