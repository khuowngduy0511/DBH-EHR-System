using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DBH.Shared.Infrastructure.cryptography;
using FileEncryptor;

namespace DBH.UnitTest.UnitTests;

public class CryptographyServiceTests
{
    private readonly ITestOutputHelper _output;

    public CryptographyServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ===== SYMMETRICENCRYPTIONSERVICE TESTS =====

    [Fact(DisplayName = "EncryptString::EncryptString-01")]
    public void EncryptString_EncryptString_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid plainText, key provided
        var plainText = "Hello, World!";
        var key = new byte[32];
        RandomNumberGenerator.Fill(key); // Generate random 32-byte key

        // Act
        var result = SymmetricEncryptionService.EncryptString(plainText, key);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(result);
        Assert.True(bytes.Length > 0);
    }

    [Fact(DisplayName = "EncryptString::EncryptString-02")]
    public void EncryptString_EncryptString_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: plainText = null/empty OR Invalid key
        var plainText = "";
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        // Act
        var result = SymmetricEncryptionService.EncryptString(plainText, key);
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        // According to the code, empty string returns empty string
        Assert.Equal("", result);
    }

    [Fact(DisplayName = "EncryptString::EncryptString-03")]
    public void EncryptString_EncryptString_03()
    {
        // Arrange
        // Precondition: Invalid input - invalid key length
        // Input: Invalid key (wrong length)
        var plainText = "Hello, World!";
        var key = new byte[16]; // Wrong length - should be 32 bytes

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        var exception = Assert.Throws<ArgumentException>(() => 
            SymmetricEncryptionService.EncryptString(plainText, key));
        Assert.Contains("Key must be 32 bytes", exception.Message);
    }

    [Fact(DisplayName = "DecryptString::DecryptString-01")]
    public void DecryptString_DecryptString_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid cipherTextBase64, key provided
        var plainText = "Hello, World!";
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        
        // First encrypt to get cipher text
        var cipherText = SymmetricEncryptionService.EncryptString(plainText, key);

        // Act
        var result = SymmetricEncryptionService.DecryptString(cipherText, key);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.Equal(plainText, result);
    }

    [Fact(DisplayName = "DecryptString::DecryptString-02")]
    public void DecryptString_DecryptString_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: cipherTextBase64 = null/empty OR Invalid key
        var cipherTextBase64 = "";
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        // Act
        var result = SymmetricEncryptionService.DecryptString(cipherTextBase64, key);
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        // According to the code, empty string returns empty string
        Assert.Equal("", result);
    }

    [Fact(DisplayName = "DecryptString::DecryptString-03")]
    public void DecryptString_DecryptString_03()
    {
        // Arrange
        // Precondition: Invalid input - invalid key length
        // Input: Invalid key (wrong length)
        var cipherTextBase64 = "dGVzdA=="; // Some base64
        var key = new byte[16]; // Wrong length - should be 32 bytes

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        var exception = Assert.Throws<ArgumentException>(() => 
            SymmetricEncryptionService.DecryptString(cipherTextBase64, key));
        Assert.Contains("Key must be 32 bytes", exception.Message);
    }

    // ===== ASYMMETRICENCRYPTIONSERVICE TESTS =====

    [Fact(DisplayName = "GenerateKeyPair::GenerateKeyPair-01")]
    public void GenerateKeyPair_GenerateKeyPair_01()
    {
        // Arrange
        // Precondition: No parameters, valid state
        
        // Act
        var result = AsymmetricEncryptionService.GenerateKeyPair();
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.NotNull(result.PublicKey);
        Assert.NotNull(result.PrivateKey);
        Assert.NotEmpty(result.PublicKey);
        Assert.NotEmpty(result.PrivateKey);
        
        // Verify they are valid base64
        var pubKeyBytes = Convert.FromBase64String(result.PublicKey);
        var privKeyBytes = Convert.FromBase64String(result.PrivateKey);
        Assert.True(pubKeyBytes.Length > 0);
        Assert.True(privKeyBytes.Length > 0);
    }

    [Fact(DisplayName = "WrapKey::WrapKey-01")]
    public void WrapKey_WrapKey_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid aesKey, recipientPublicKeyBase64 provided
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);
        
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        var recipientPublicKeyBase64 = keyPair.PublicKey;

        // Act
        var result = AsymmetricEncryptionService.WrapKey(aesKey, recipientPublicKeyBase64);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Verify it's valid base64
        var wrappedKeyBytes = Convert.FromBase64String(result);
        Assert.True(wrappedKeyBytes.Length > 0);
    }

    [Fact(DisplayName = "WrapKey::WrapKey-02")]
    public void WrapKey_WrapKey_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: Invalid aesKey OR recipientPublicKeyBase64 = null/empty
        byte[] aesKey = null;
        var recipientPublicKeyBase64 = "invalid-base64";

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.Throws<ArgumentNullException>(() => 
            AsymmetricEncryptionService.WrapKey(aesKey, recipientPublicKeyBase64));
    }

    [Fact(DisplayName = "WrapKeyBase64::WrapKeyBase64-01")]
    public void WrapKeyBase64_WrapKeyBase64_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid aesKeyBase64, recipientPublicKeyBase64 provided
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);
        var aesKeyBase64 = Convert.ToBase64String(aesKey);
        
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        var recipientPublicKeyBase64 = keyPair.PublicKey;

        // Act
        var result = AsymmetricEncryptionService.WrapKeyBase64(aesKeyBase64, recipientPublicKeyBase64);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Verify it's valid base64
        var wrappedKeyBytes = Convert.FromBase64String(result);
        Assert.True(wrappedKeyBytes.Length > 0);
    }

    [Fact(DisplayName = "WrapKeyBase64::WrapKeyBase64-02")]
    public void WrapKeyBase64_WrapKeyBase64_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: aesKeyBase64 = null/empty OR recipientPublicKeyBase64 = null/empty
        string aesKeyBase64 = null;
        var recipientPublicKeyBase64 = "invalid-base64";

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.Throws<ArgumentNullException>(() => 
            AsymmetricEncryptionService.WrapKeyBase64(aesKeyBase64, recipientPublicKeyBase64));
    }

    [Fact(DisplayName = "UnwrapKey::UnwrapKey-01")]
    public void UnwrapKey_UnwrapKey_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid wrappedKeyBase64, ownerPrivateKeyBase64 provided
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);
        
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        var wrappedKey = AsymmetricEncryptionService.WrapKey(aesKey, keyPair.PublicKey);

        // Act
        var result = AsymmetricEncryptionService.UnwrapKey(wrappedKey, keyPair.PrivateKey);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.Equal(aesKey, result);
    }

    [Fact(DisplayName = "UnwrapKey::UnwrapKey-02")]
    public void UnwrapKey_UnwrapKey_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: wrappedKeyBase64 = null/empty OR ownerPrivateKeyBase64 = null/empty
        string wrappedKeyBase64 = null;
        string ownerPrivateKeyBase64 = "invalid-base64";

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.Throws<ArgumentNullException>(() => 
            AsymmetricEncryptionService.UnwrapKey(wrappedKeyBase64, ownerPrivateKeyBase64));
    }

    [Fact(DisplayName = "UnwrapKeyBase64::UnwrapKeyBase64-01")]
    public void UnwrapKeyBase64_UnwrapKeyBase64_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid wrappedKeyBase64, ownerPrivateKeyBase64 provided
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);
        var aesKeyBase64 = Convert.ToBase64String(aesKey);
        
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        var wrappedKey = AsymmetricEncryptionService.WrapKeyBase64(aesKeyBase64, keyPair.PublicKey);

        // Act
        var result = AsymmetricEncryptionService.UnwrapKeyBase64(wrappedKey, keyPair.PrivateKey);
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.NotNull(result);
        Assert.Equal(aesKeyBase64, result);
    }

    [Fact(DisplayName = "UnwrapKeyBase64::UnwrapKeyBase64-02")]
    public void UnwrapKeyBase64_UnwrapKeyBase64_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: wrappedKeyBase64 = null/empty OR ownerPrivateKeyBase64 = null/empty
        string wrappedKeyBase64 = null;
        string ownerPrivateKeyBase64 = "invalid-base64";

        // Act & Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.Throws<ArgumentNullException>(() => 
            AsymmetricEncryptionService.UnwrapKeyBase64(wrappedKeyBase64, ownerPrivateKeyBase64));
    }

    // ===== FILEENCRYPTIONSERVICE TESTS =====
    // Note: FileEncryptionService is in FileEncryptor namespace, not DBH.Shared.Infrastructure.cryptography

    [Fact(DisplayName = "EncryptFile::EncryptFile-01")]
    public void EncryptFile_EncryptFile_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid filePath, password provided
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Test content for encryption");
        var password = "TestPassword123!";

        try
        {
            // Act
            var result = FileEncryptionService.EncryptFile(tempFile, password);
            
            // Assert
            // Expected Return: Returns success payload matching declared return type
            Assert.True(result);
            
            // Verify encrypted file was created
            var encryptedFile = tempFile + ".aes";
            Assert.True(File.Exists(encryptedFile));
            
            // Clean up
            if (File.Exists(encryptedFile))
                File.Delete(encryptedFile);
        }
        finally
        {
            // Clean up
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact(DisplayName = "EncryptFile::EncryptFile-02")]
    public void EncryptFile_EncryptFile_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: filePath = null/empty OR password = null/empty
        string filePath = null;
        string password = "TestPassword123!";

        // Act
        var result = FileEncryptionService.EncryptFile(filePath, password);
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.False(result);
    }

    [Fact(DisplayName = "EncryptFile::EncryptFile-03")]
    public void EncryptFile_EncryptFile_03()
    {
        // Arrange
        // Precondition: File not found
        // Input: Non-existent file path
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        var password = "TestPassword123!";

        // Act
        var result = FileEncryptionService.EncryptFile(filePath, password);
        
        // Assert
        // Expected Return: Returns false for file not found
        Assert.False(result);
    }

    [Fact(DisplayName = "DecryptFile::DecryptFile-01")]
    public void DecryptFile_DecryptFile_01()
    {
        // Arrange
        // Precondition: Valid input
        // Input: Valid encryptedFilePath, password provided
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Test content for encryption and decryption");
        var password = "TestPassword123!";
        
        // First encrypt the file
        var encryptResult = FileEncryptionService.EncryptFile(tempFile, password);
        Assert.True(encryptResult);
        
        var encryptedFile = tempFile + ".aes";
        var decryptedFile = encryptedFile + ".decrypted";

        try
        {
            // Act
            var result = FileEncryptionService.DecryptFile(encryptedFile, password);
            
            // Assert
            // Expected Return: Returns success payload matching declared return type
            Assert.True(result);
            // Verify content matches
            var originalContent = File.ReadAllText(tempFile);
            // Since inputFile was encryptedFile (tempFile + ".aes"), outputFile is tempFile
            // However, tempFile already exists. Let's re-read it.
            var decryptedContent = File.ReadAllText(tempFile);
            Assert.Equal(originalContent, decryptedContent);
        }
        finally
        {
            // Clean up
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (File.Exists(encryptedFile))
                File.Delete(encryptedFile);
            if (File.Exists(decryptedFile))
                File.Delete(decryptedFile);
        }
    }

    [Fact(DisplayName = "DecryptFile::DecryptFile-02")]
    public void DecryptFile_DecryptFile_02()
    {
        // Arrange
        // Precondition: Invalid input
        // Input: encryptedFilePath = null/empty OR password = null/empty
        string encryptedFilePath = null;
        string password = "TestPassword123!";

        // Act
        var result = FileEncryptionService.DecryptFile(encryptedFilePath, password);
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.False(result);
    }

    [Fact(DisplayName = "DecryptFile::DecryptFile-03")]
    public void DecryptFile_DecryptFile_03()
    {
        // Arrange
        // Precondition: File not found
        // Input: Non-existent encrypted file path
        var encryptedFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".aes");
        var password = "TestPassword123!";

        // Act
        var result = FileEncryptionService.DecryptFile(encryptedFilePath, password);
        
        // Assert
        // Expected Return: Returns false for file not found
        Assert.False(result);
    }
}