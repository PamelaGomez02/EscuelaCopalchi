
using System;
using System.Security.Cryptography;

namespace EscuelaCopalchi.UI.Models.Seguridad
{
    public static class PasswordHelper
    {
        private const int Iteraciones = 120000;
        private const int TamanoSal = 16;
        private const int TamanoHash = 32;

        public static string CrearHash(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena))
                throw new ArgumentException(
                    "La contraseña es obligatoria.");

            byte[] sal = new byte[TamanoSal];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(sal);
            }

            byte[] hash;

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                contrasena,
                sal,
                Iteraciones,
                HashAlgorithmName.SHA256))
            {
                hash = pbkdf2.GetBytes(TamanoHash);
            }

            return "PBKDF2-SHA256$" +
                Iteraciones + "$" +
                Convert.ToBase64String(sal) + "$" +
                Convert.ToBase64String(hash);
        }

        public static bool Verificar(
            string contrasena,
            string hashGuardado)
        {
            if (string.IsNullOrEmpty(contrasena) ||
                string.IsNullOrEmpty(hashGuardado))
                return false;

            string[] partes = hashGuardado.Split('$');

            if (partes.Length != 4 ||
                partes[0] != "PBKDF2-SHA256")
                return false;

            int iteraciones;

            if (!int.TryParse(partes[1], out iteraciones) ||
                iteraciones < 120000 ||
                iteraciones > 1000000)
                return false;

            try
            {
                byte[] sal =
                    Convert.FromBase64String(partes[2]);

                byte[] hashOriginal =
                    Convert.FromBase64String(partes[3]);

                if (sal.Length != TamanoSal ||
                    hashOriginal.Length != TamanoHash)
                    return false;

                byte[] hashCalculado;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    contrasena,
                    sal,
                    iteraciones,
                    HashAlgorithmName.SHA256))
                {
                    hashCalculado =
                        pbkdf2.GetBytes(hashOriginal.Length);
                }

                int diferencia = 0;

                for (int i = 0; i < hashOriginal.Length; i++)
                {
                    diferencia |=
                        hashOriginal[i] ^ hashCalculado[i];
                }

                return diferencia == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
