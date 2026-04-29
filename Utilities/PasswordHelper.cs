using System;
using System.Security.Cryptography;
using System.Text;

namespace PayrollSystem.Utilities
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            // If the stored hash is plain text from old versions, we should ideally handle it,
            // but the migration script will hash all passwords, so storedHash should always be hashed.
            var hashOfInput = HashPassword(inputPassword);
            return hashOfInput == storedHash;
        }
    }
}
