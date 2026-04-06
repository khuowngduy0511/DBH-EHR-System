using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FileEncryptor;

namespace DBH.Shared.Infrastructure.Ipfs;

/// <summary>
/// Thin, reusable facade for encrypting/decrypting files and moving them to/from IPFS.
/// Keep using the same IpfsConfig shape found in appsettings.json.
/// </summary>
public sealed class SecureFileTransferService : IDisposable
{
    private readonly IpfsService _ipfs;
    private readonly IpfsConfig _config;

    public SecureFileTransferService(IpfsConfig config)
    {
        _config = config;
        _ipfs = new IpfsService(config);
    }

    public Task<IpfsUploadResponse?> UploadAsync(string filePath) => _ipfs.UploadFileAsync(filePath);

    public async Task<IpfsUploadResponse?> EncryptAndUploadAsync(string filePath, string password)
    {
        var encryptedPath = GetEncryptedPath(filePath);
        if (!FileEncryptionService.EncryptFile(filePath, password))
        {
            return null;
        }

        if (!File.Exists(encryptedPath))
        {
            throw new FileNotFoundException("Encrypted file not found after encryption", encryptedPath);
        }

        return await _ipfs.UploadFileAsync(encryptedPath);
    }

    public bool EncryptFile(string filePath, string password)
    {
        return FileEncryptionService.EncryptFile(filePath, password);
    }

    public bool DecryptFile(string encryptedFilePath, string password)
    {
        return FileEncryptionService.DecryptFile(encryptedFilePath, password);
    }

    public async Task<IpfsFileResponse> RetrieveAsync(string cid)
    {
        return await _ipfs.RetrieveFileAsync(cid);
    }

    public async Task<string?> RetrieveAndDecryptAsync(string cid, string password, string? encryptedOutPath = null)
    {
        var file = await _ipfs.RetrieveFileAsync(cid);

        var targetEncryptedPath = ResolveEncryptedSavePath(cid, file.ContentType, encryptedOutPath);
        EnsureParentDirectoryExists(targetEncryptedPath);
        await File.WriteAllBytesAsync(targetEncryptedPath, file.Data);

        if (!FileEncryptionService.DecryptFile(targetEncryptedPath, password))
        {
            return null;
        }

        return targetEncryptedPath.EndsWith(".aes", StringComparison.OrdinalIgnoreCase)
            ? targetEncryptedPath[..^4]
            : targetEncryptedPath + ".decrypted";
    }

    public void Dispose()
    {
        _ipfs.Dispose();
    }

    public static IpfsConfig LoadConfig(string path = "appsettings.json")
    {
        var config = new IpfsConfig();
        if (!File.Exists(path)) return config;

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("IpfsConfig", out var configEl))
        {
            config.ApiUrl = configEl.GetProperty("ApiUrl").GetString() ?? config.ApiUrl;
            config.GatewayUrl = configEl.GetProperty("GatewayUrl").GetString() ?? config.GatewayUrl;
            if (configEl.TryGetProperty("DownloadPath", out var downloadPathEl))
            {
                config.DownloadPath = downloadPathEl.GetString() ?? string.Empty;
            }
            if (configEl.TryGetProperty("FilePath", out var pathEl))
            {
                config.DownloadPath = pathEl.GetString() ?? string.Empty;
            }
        }

        return config;
    }

    private string ResolveEncryptedSavePath(string cid, string? contentType, string? encryptedOutPath)
    {
        if (!string.IsNullOrWhiteSpace(encryptedOutPath))
        {
            return encryptedOutPath;
        }

        var ext = IpfsService.GetExtensionForContentType(contentType);
        var fileName = $"{cid}{ext}";

        if (!string.IsNullOrEmpty(_config.DownloadPath))
        {
            return Path.Combine(_config.DownloadPath, fileName);
        }

        return Path.Combine(Path.GetTempPath(), "dbh-ipfs-downloads", fileName);
    }

    private static void EnsureParentDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetEncryptedPath(string filePath)
    {
        return filePath.EndsWith(".aes", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : filePath + ".aes";
    }
}
