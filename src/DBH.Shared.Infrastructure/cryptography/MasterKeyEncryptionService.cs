using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DBH.Shared.Infrastructure.cryptography
{
    public static class MasterKeyEncryptionService
    {
        // For a production app, this key should be loaded securely from an environment variable or secret manager.
        // It MUST be exactly 32 bytes for AES-256.
        private static readonly byte[] MasterKey = GetMasterKey();

        private static byte[] GetMasterKey()
        {
            try
            {
                var envKey = Environment.GetEnvironmentVariable("MASTER_ENCRYPTION_KEY");
                if (!string.IsNullOrEmpty(envKey) && envKey.Length == 32)
                {
                    return Encoding.UTF8.GetBytes(envKey);
                }
                
                // Fallback fixed key for development/testing ONLY
                var fallbackStr = "DbhEhrSystemSecureMasterKey2026!";
                return Encoding.UTF8.GetBytes(fallbackStr);
            }
            catch
            {
                return Encoding.UTF8.GetBytes("DbhEhrSystemSecureMasterKey2026!");
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using (Aes aes = Aes.Create())
            {
                aes.Key = MasterKey;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(iv, 0, iv.Length);
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    var encryptedBytes = msEncrypt.ToArray();
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            var fullCipher = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = MasterKey;
                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
