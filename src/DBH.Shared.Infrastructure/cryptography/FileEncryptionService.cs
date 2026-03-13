using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileEncryptor
{
    /// <summary>
    /// Provides services for encrypting files using AES-256.
    /// </summary>
    public class FileEncryptionService
    {
        /// <summary>
        /// Encrypts the specified file using a password-derived key.
        /// </summary>
        /// <param name="inputFile">The full path to the file to be encrypted.</param>
        /// <param name="password">The password used to derive the encryption key.</param>
        /// <returns>True if encryption was successful; otherwise, false.</returns>
        public static bool EncryptFile(string inputFile, string password)
        {
            try
            {
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Error: File not found at {inputFile}");
                    return false;
                }

                // Derive a 32-byte key from the password
                byte[] key = GetKeyFromPassword(password);

                string outputFile = inputFile + ".aes";
                
                // Create a new AES instance
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV(); // Generate a new random IV
                    byte[] iv = aes.IV;

                    using (FileStream outputFs = new FileStream(outputFile, FileMode.Create))
                    {
                        // Write the IV to the beginning of the file so it can be used for decryption
                        outputFs.Write(iv, 0, iv.Length);

                        using (CryptoStream cryptoStream = new CryptoStream(outputFs, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (FileStream inputFs = new FileStream(inputFile, FileMode.Open))
                        {
                            inputFs.CopyTo(cryptoStream);
                        }
                    }
                }

                Console.WriteLine($"Encryption completed successfully. Encrypted file: {outputFile}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decrypts the specified file using a password-derived key.
        /// </summary>
        /// <param name="inputFile">The full path to the encrypted file.</param>
        /// <param name="password">The password used to derive the decryption key.</param>
        /// <returns>True if decryption was successful; otherwise, false.</returns>
        public static bool DecryptFile(string inputFile, string password)
        {
            try
            {
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Error: File not found at {inputFile}");
                    return false;
                }

                // Derive a 32-byte key from the password
                byte[] key = GetKeyFromPassword(password);

                // Determine output filename: remove ".aes" if present, otherwise append ".decrypted"
                string outputFile = inputFile.EndsWith(".aes") 
                    ? inputFile.Substring(0, inputFile.Length - 4) 
                    : inputFile + ".decrypted";

                // If the output file already exists (e.g. overwriting the original), you might want to ask or handle it.
                // For this simple example, we will just overwrite or create it.

                using (FileStream inputFs = new FileStream(inputFile, FileMode.Open))
                {
                    byte[] iv = new byte[16];
                    int bytesRead = inputFs.Read(iv, 0, iv.Length);
                    if (bytesRead < 16)
                    {
                        Console.WriteLine("Error: Invalid file format (file too short).");
                        return false;
                    }

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;

                        using (FileStream outputFs = new FileStream(outputFile, FileMode.Create))
                        using (CryptoStream cryptoStream = new CryptoStream(inputFs, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(outputFs);
                        }
                    }
                }

                Console.WriteLine($"Decryption completed successfully. Decrypted file: {outputFile}");
                return true;
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Decryption failed: Incorrect password or corrupted file.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Derives a 256-bit (32-byte) key from the given string password using SHA-256 hashing.
        /// </summary>
        /// <param name="password">The input password.</param>
        /// <returns>A 32-byte byte array serving as the key.</returns>
        private static byte[] GetKeyFromPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
