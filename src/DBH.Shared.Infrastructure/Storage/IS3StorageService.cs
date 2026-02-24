namespace DBH.Shared.Infrastructure.Storage;

/// <summary>
/// Interface cho S3 Storage Service - Upload/Download EHR files với encryption
/// </summary>
public interface IS3StorageService
{
    /// <summary>
    /// Upload file lên S3 với Server-Side Encryption
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="folder">Folder path (e.g., "ehr/{patientId}")</param>
    /// <param name="metadata">Custom metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with S3 key and hash</returns>
    Task<S3UploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string folder,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download file từ S3
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream và metadata</returns>
    Task<S3DownloadResult> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate pre-signed URL để download file trực tiếp
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="expirationMinutes">URL expiration in minutes</param>
    /// <returns>Pre-signed URL</returns>
    string GetPreSignedUrl(string key, int? expirationMinutes = null);

    /// <summary>
    /// Delete file từ S3
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file metadata without downloading
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    Task<S3FileMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy file to another location
    /// </summary>
    /// <param name="sourceKey">Source S3 key</param>
    /// <param name="destinationKey">Destination S3 key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result của upload operation
/// </summary>
public class S3UploadResult
{
    public bool Success { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? VersionId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result của download operation
/// </summary>
public class S3DownloadResult
{
    public bool Success { get; set; }
    public Stream? Content { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string? ETag { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// File metadata từ S3
/// </summary>
public class S3FileMetadata
{
    public string Key { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ETag { get; set; }
    public DateTime LastModified { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsEncrypted { get; set; }
    public string? EncryptionAlgorithm { get; set; }
}
