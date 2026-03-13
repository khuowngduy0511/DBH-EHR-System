using System.Security.Cryptography;
using System.Text;

namespace DBH.Shared.Infrastructure.cryptography;

/// <summary>
/// Service for symmetric encryption using AES.
/// Ideal for encrypting/decrypting strings or byte payloads.
/// </summary>
public static class SymmetricEncryptionService
{
    private const int KeySize = 256;
    private const int BlockSize = 128;

    /// <summary>
    /// Encrypts a string using AES-256 with a given byte array key.
    /// Returns the concatenated IV + Ciphertext as a Base64 string.
    /// </summary>
    public static string EncryptString(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (key == null || key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (256 bits) long.", nameof(key));

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;
        aes.Key = key;
        aes.GenerateIV(); // Generate a random IV for each encryption

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts a Base64 encoded string (IV + Ciphertext) using AES-256 with a given byte array key.
    /// </summary>
    public static string DecryptString(string cipherTextBase64, byte[] key)
    {
        if (string.IsNullOrEmpty(cipherTextBase64))
            return cipherTextBase64;

        if (key == null || key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes (256 bits) long.", nameof(key));

        var combinedBytes = Convert.FromBase64String(cipherTextBase64);

        using var aes = Aes.Create();
        aes.KeySize = KeySize;
        aes.BlockSize = BlockSize;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;
        aes.Key = key;

        // Extract IV (first 16 bytes for AES block size of 128)
        var iv = new byte[16];
        var cipherBytes = new byte[combinedBytes.Length - 16];

        Buffer.BlockCopy(combinedBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(combinedBytes, 16, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
