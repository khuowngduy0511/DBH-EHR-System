using System;
using System.Security.Cryptography;
using System.Text;

namespace DBH.Shared.Infrastructure.cryptography
{
    public static class AsymmetricEncryptionService
    {
        // Note: RSA padding mode OAEP uses SHA1 by default in older frameworks, but SHA256 is better.
        // We will stick to OAEP SHA256 for wrapping.
        private static readonly RSAEncryptionPadding _padding = RSAEncryptionPadding.OaepSHA256;

        /// <summary>
        /// Generates a new RSA 2048-bit key pair.
        /// </summary>
        /// <returns>A tuple containing the PublicKey and PrivateKey in base64 string format.</returns>
        public static (string PublicKey, string PrivateKey) GenerateKeyPair()
        {
            using (var rsa = RSA.Create(2048))
            {
                var privKeyBytes = rsa.ExportPkcs8PrivateKey();
                var pubKeyBytes = rsa.ExportSubjectPublicKeyInfo();

                return (
                    Convert.ToBase64String(pubKeyBytes),
                    Convert.ToBase64String(privKeyBytes)
                );
            }
        }

        /// <summary>
        /// Wraps (Encrypts) a symmetric AES key using the recipient's RSA Public Key.
        /// </summary>
        /// <param name="aesKey">The raw AES key (e.g. 32 bytes).</param>
        /// <param name="recipientPublicKeyBase64">The recipient's RSA public key.</param>
        /// <returns>The wrapped key in base64 format.</returns>
        public static string WrapKey(byte[] aesKey, string recipientPublicKeyBase64)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(recipientPublicKeyBase64), out _);
                byte[] wrappedKeyBytes = rsa.Encrypt(aesKey, _padding);
                return Convert.ToBase64String(wrappedKeyBytes);
            }
        }

        /// <summary>
        /// Wraps (Encrypts) a symmetric base64 AES key string using the recipient's RSA Public Key.
        /// </summary>
        public static string WrapKeyBase64(string aesKeyBase64, string recipientPublicKeyBase64)
        {
            var aesKey = Convert.FromBase64String(aesKeyBase64);
            return WrapKey(aesKey, recipientPublicKeyBase64);
        }

        /// <summary>
        /// Unwraps (Decrypts) an AES key using the owner's RSA Private Key.
        /// </summary>
        /// <param name="wrappedKeyBase64">The encrypted base64 wrapped key.</param>
        /// <param name="ownerPrivateKeyBase64">The owner's RSA private key.</param>
        /// <returns>The raw unwrapped AES key.</returns>
        public static byte[] UnwrapKey(string wrappedKeyBase64, string ownerPrivateKeyBase64)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(ownerPrivateKeyBase64), out _);
                byte[] wrappedKeyBytes = Convert.FromBase64String(wrappedKeyBase64);
                return rsa.Decrypt(wrappedKeyBytes, _padding);
            }
        }

        /// <summary>
        /// Unwraps (Decrypts) an AES key using the owner's RSA Private Key, returning it as Base64.
        /// </summary>
        public static string UnwrapKeyBase64(string wrappedKeyBase64, string ownerPrivateKeyBase64)
        {
            var rawKey = UnwrapKey(wrappedKeyBase64, ownerPrivateKeyBase64);
            return Convert.ToBase64String(rawKey);
        }
    }
}
