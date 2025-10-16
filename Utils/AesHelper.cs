using System.Security.Cryptography;
using System.Text;

namespace MyEcologicCrowsourcingApp.Utils
{
    public static class AesHelper
    {
        private static readonly string SecretKey = "CLE_SECRETE_FRONTEND"; 

        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                var key = new Rfc2898DeriveBytes(SecretKey, Encoding.UTF8.GetBytes("s@lt1234"));
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);
                return reader.ReadToEnd();
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
