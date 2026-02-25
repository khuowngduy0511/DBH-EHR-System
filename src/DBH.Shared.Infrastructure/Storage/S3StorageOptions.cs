namespace DBH.Shared.Infrastructure.Storage;

/// <summary>
/// Cấu hình S3 Storage với Server-Side Encryption (KMS)
/// </summary>
public class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    /// <summary>
    /// AWS Region (e.g., ap-southeast-1)
    /// </summary>
    public string Region { get; set; } = "ap-southeast-1";

    /// <summary>
    /// S3 Bucket name cho EHR files
    /// </summary>
    public string BucketName { get; set; } = "dbh-ehr-files";

    /// <summary>
    /// AWS KMS Key ID for Server-Side Encryption
    /// </summary>
    public string? KmsKeyId { get; set; }

    /// <summary>
    /// Enable Server-Side Encryption với KMS
    /// </summary>
    public bool EnableServerSideEncryption { get; set; } = true;

    /// <summary>
    /// Max file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Pre-signed URL expiration in minutes
    /// </summary>
    public int PreSignedUrlExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Custom S3 endpoint (for MinIO or LocalStack in dev)
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// AWS Access Key (optional - use IAM roles in production)
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS Secret Key (optional - use IAM roles in production)
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Force path style (required for MinIO)
    /// </summary>
    public bool ForcePathStyle { get; set; } = false;
}
