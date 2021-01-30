using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TableService.Core.Utility
{
    public static class PasswordUtility
    {
        private static string GenerateSalt()
        {
            var bytes = new byte[128 / 8];
            var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);

            return Convert.ToBase64String(bytes);
        }


        /// <summary>
        /// Hashes a password for storage in the database as format -digest-.-salt-
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string HashPassword(string password, string salt = null)
        {
            // Generate the Salt
            var genSalt = (salt == null) ? GenerateSalt() : salt;

            // Hash the password
            var byteResult = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(genSalt), 10000);
            var hash = Convert.ToBase64String(byteResult.GetBytes(24));

            return hash + "." + genSalt;
        }

        /// <summary>
        /// Verify a password
        /// </summary>
        /// <param name="password"></param>
        /// <param name="digest"></param>
        /// <returns></returns>
        public static bool VerifyPassword(string password, string digest)
        {
            // Split the digest into two parts
            var parts = digest.Split(".");
            var storedHash = parts[0];
            var storedSalt = parts[1];

            var hash = HashPassword(password, storedSalt);

            return (hash == digest);
        }
    }
}
