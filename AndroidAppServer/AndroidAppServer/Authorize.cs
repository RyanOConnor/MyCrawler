using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using MongoDB.Driver;
using MongoDB.Bson;

namespace AndroidAppServer
{
    public class Authorize
    {
        public static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        public static RNGCryptoServiceProvider rng
        {
            get { lock (_rng) { return _rng; } }
        }
        private const int SaltSize = 24;
        private const int HashSize = 24;

        public static byte[] GenerateSalt()
        {
            byte[] buffer = new byte[SaltSize];
            rng.GetBytes(buffer);

            return buffer;
        }

        public static byte[] GenerateSaltedHash(byte[] password, byte[] salt)
        {
            SHA256Managed algorithm = new SHA256Managed();
            byte[] algorithmInput = new byte[password.Length + salt.Length];

            password.CopyTo(algorithmInput, 0);
            salt.CopyTo(algorithmInput, password.Length);

            return algorithm.ComputeHash(algorithmInput);
        }

        public static string GetSessionToken()
        {
            return ObjectId.GenerateNewId().ToString();
        }

        public static bool IsValidHash(byte[] inputPassword, byte[] storedPassword)
        {
            if (inputPassword.Length != storedPassword.Length)
            {
                return false;
            }

            for (int i = 0; i < inputPassword.Length; i++)
            {
                if (inputPassword[i] != storedPassword[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool PassesGuidelines(string password)
        {
            // Set up password requirements
            return true;
        }
    }
}
