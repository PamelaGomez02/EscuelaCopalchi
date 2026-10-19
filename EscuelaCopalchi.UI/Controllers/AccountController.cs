
using EscuelaCopalchi.UI.Models;
using EscuelaCopalchi.UI.Models.Seguridad;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace EscuelaCopalchi.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly string conexion =
            ConfigurationManager.ConnectionStrings[
                "AulaVirtualCopalchi"].ConnectionString;

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(
            LoginViewModel modelo,
            string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            using (var conn = new SqlConnection(conexion))
            {
                conn.Open();

                string sql = @"
                    SELECT
                        u.id_usuario,
                        u.nombre,
                        u.apellido1,
                        u.correo,
                        u.contrasena_hash,
                        u.estado,
                        r.nombre AS nombre_rol
                    FROM dbo.USUARIO u
                    INNER JOIN dbo.ROL r
                        ON r.id_rol = u.id_rol
                    WHERE u.correo = @correo
                      AND u.estado = 1
                      AND r.estado = 1";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(
                        "@correo", SqlDbType.NVarChar, 150)
                        .Value = modelo.Correo.Trim();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ModelState.AddModelError(
                                "",
                                "Correo o contraseña incorrectos.");

                            return View(modelo);
                        }

                        int idUsuario =
                            Convert.ToInt32(reader["id_usuario"]);

                        string hash =
                            Convert.ToString(
                                reader["contrasena_hash"]);

                        if (!PasswordHelper.Verificar(
                            modelo.Contrasena, hash))
                        {
                            ModelState.AddModelError(
                                "",
                                "Correo o contraseña incorrectos.");

                            return View(modelo);
                        }

                        Session["IdUsuario"] = idUsuario;

                        Session["NombreUsuario"] =
                            Convert.ToString(reader["nombre"]) +
                            " " +
                            Convert.ToString(reader["apellido1"]);

                        Session["RolUsuario"] =
                            Convert.ToString(reader["nombre_rol"]);

                        Session["CorreoUsuario"] =
                            Convert.ToString(reader["correo"]);

                        // Crear cookie de autenticación.
                        FormsAuthentication.SetAuthCookie(
                            Convert.ToString(reader["correo"]),
                            false);

                        // Cerrar el lector antes del UPDATE.
                        reader.Close();

                        using (var actualizar = new SqlCommand(
                            @"UPDATE dbo.USUARIO
                              SET ultimo_acceso = SYSDATETIME()
                              WHERE id_usuario = @id", conn))
                        {
                            actualizar.Parameters.Add(
                                "@id", SqlDbType.Int)
                                .Value = idUsuario;

                            actualizar.ExecuteNonQuery();
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }
    }
}
