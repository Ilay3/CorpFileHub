using System.Security.Cryptography;

namespace CorpFileHub.Application.Utilities
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[48];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, 16);
            Buffer.BlockCopy(hash, 0, hashBytes, 16, 32);
            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(storedHash);
                var salt = new byte[16];
                Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);
                var storedSubHash = new byte[32];
                Buffer.BlockCopy(hashBytes, 16, storedSubHash, 0, 32);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                var computedHash = pbkdf2.GetBytes(32);
                return storedSubHash.SequenceEqual(computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
