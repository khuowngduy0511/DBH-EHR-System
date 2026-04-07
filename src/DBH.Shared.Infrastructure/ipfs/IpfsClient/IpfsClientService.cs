using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DBH.Shared.Infrastructure.Ipfs;

public class IpfsConfig
{
    public string ApiUrl { get; set; } = "http://localhost:5001/api/v0";
    public string GatewayUrl { get; set; } = "http://localhost:8080/ipfs";
    public string DownloadPath { get; set; } = string.Empty;
}

public class IpfsUploadResponse
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("Size")]
    public string Size { get; set; } = string.Empty;
}

public class IpfsFileResponse
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string? ContentType { get; set; }
    public long? ContentLength { get; set; }
}

/// <summary>
/// Low-level IPFS HTTP client. Handles raw upload/retrieve.
/// </summary>
public class IpfsService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IpfsConfig _config;

    public IpfsService(IpfsConfig config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task<IpfsUploadResponse?> UploadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Target file not found", filePath);

        using var content = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync($"{_config.ApiUrl}/add?pin=true", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IpfsUploadResponse>(jsonResponse);
    }

    public async Task<IpfsFileResponse> RetrieveFileAsync(string cid)
    {
        var response = await _httpClient.GetAsync($"{_config.GatewayUrl}/{cid}");
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var contentLength = response.Content.Headers.ContentLength;
        var data = await response.Content.ReadAsByteArrayAsync();

        return new IpfsFileResponse
        {
            Data = data,
            ContentType = contentType,
            ContentLength = contentLength
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public static string GetExtensionForContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return ".bin";

        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "image/png", ".png" },
            { "image/jpeg", ".jpg" },
            { "application/pdf", ".pdf" },
            { "text/plain", ".txt" },
            { "application/json", ".json" },
            { "text/html", ".html" },
            { "image/gif", ".gif" },
            { "video/mp4", ".mp4" },
            { "application/octet-stream", ".aes" },
            { "application/x-aes", ".aes" },
            { "application/x-aes-encrypted", ".aes" }
        };

        var mediaType = contentType.Split(';')[0].Trim();

        return mapping.TryGetValue(mediaType, out var ext) ? ext : ".bin";
    }
}

/// <summary>
/// Service-style facade mirroring FileEncryptionService: config in, async upload/retrieve out.
/// </summary>
public static class IpfsClientService
{
    public static async Task<IpfsUploadResponse?> UploadAsync(string filePath, IpfsConfig? config = null)
    {
        config ??= LoadConfig();
        using var ipfs = new IpfsService(config);
        return await ipfs.UploadFileAsync(filePath);
    }

    public static async Task<string> RetrieveAsync(string cid, string? outPath = null, IpfsConfig? config = null)
    {
        config ??= LoadConfig();
        using var ipfs = new IpfsService(config);
        var response = await ipfs.RetrieveFileAsync(cid);

        var resolvedPath = ResolveOutputPath(cid, response.ContentType, outPath, config.DownloadPath);
        EnsureParentDirectoryExists(resolvedPath);
        await File.WriteAllBytesAsync(resolvedPath, response.Data);

        return resolvedPath;
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

        // Allow container/runtime overrides (e.g., docker-compose IpfsConfig__ApiUrl)
        // to take precedence over appsettings and localhost defaults.
        var apiUrlEnv = Environment.GetEnvironmentVariable("IpfsConfig__ApiUrl");
        if (!string.IsNullOrWhiteSpace(apiUrlEnv))
        {
            config.ApiUrl = apiUrlEnv;
        }

        var gatewayUrlEnv = Environment.GetEnvironmentVariable("IpfsConfig__GatewayUrl");
        if (!string.IsNullOrWhiteSpace(gatewayUrlEnv))
        {
            config.GatewayUrl = gatewayUrlEnv;
        }

        var downloadPathEnv = Environment.GetEnvironmentVariable("IpfsConfig__DownloadPath");
        if (!string.IsNullOrWhiteSpace(downloadPathEnv))
        {
            config.DownloadPath = downloadPathEnv;
        }

        return config;
    }

    private static string ResolveOutputPath(string cid, string? contentType, string? outPath, string? downloadPath)
    {
        if (!string.IsNullOrWhiteSpace(outPath))
        {
            return outPath;
        }

        var ext = IpfsService.GetExtensionForContentType(contentType);
        var fileName = $"{cid}{ext}";

        if (!string.IsNullOrEmpty(downloadPath))
        {
            return Path.Combine(downloadPath, fileName);
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
}
