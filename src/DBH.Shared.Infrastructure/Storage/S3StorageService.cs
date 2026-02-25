using System.Security.Cryptography;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DBH.Shared.Infrastructure.Storage;

/// <summary>
/// S3 Storage Service - Upload/Download EHR files với Server-Side Encryption (KMS)
/// </summary>
public class S3StorageService : IS3StorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly TransferUtility _transferUtility;

    public S3StorageService(
        IOptions<S3StorageOptions> options,
        ILogger<S3StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region),
            ForcePathStyle = _options.ForcePathStyle
        };

        if (!string.IsNullOrEmpty(_options.ServiceUrl))
        {
            config.ServiceURL = _options.ServiceUrl;
        }

        if (!string.IsNullOrEmpty(_options.AccessKey) && !string.IsNullOrEmpty(_options.SecretKey))
        {
            _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }
        else
        {
            // Use IAM role or environment credentials
            _s3Client = new AmazonS3Client(config);
        }

        _transferUtility = new TransferUtility(_s3Client);
    }

    public async Task<S3UploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string folder,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file size
            if (fileStream.Length > _options.MaxFileSizeMB * 1024 * 1024)
            {
                return new S3UploadResult
                {
                    Success = false,
                    ErrorMessage = $"File size exceeds maximum allowed size of {_options.MaxFileSizeMB}MB"
                };
            }

            // Generate unique key
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = string.IsNullOrEmpty(folder) 
                ? uniqueFileName 
                : $"{folder.TrimEnd('/')}/{uniqueFileName}";

            // Calculate file hash before upload
            fileStream.Position = 0;
            var fileHash = await ComputeSha256HashAsync(fileStream, cancellationToken);
            fileStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            // Add custom metadata
            if (metadata != null)
            {
                foreach (var (k, v) in metadata)
                {
                    request.Metadata.Add(k, v);
                }
            }
            request.Metadata.Add("x-amz-meta-original-filename", fileName);
            request.Metadata.Add("x-amz-meta-file-hash", fileHash);
            request.Metadata.Add("x-amz-meta-upload-timestamp", DateTime.UtcNow.ToString("O"));

            // Enable Server-Side Encryption with KMS
            if (_options.EnableServerSideEncryption)
            {
                if (!string.IsNullOrEmpty(_options.KmsKeyId))
                {
                    request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
                    request.ServerSideEncryptionKeyManagementServiceKeyId = _options.KmsKeyId;
                }
                else
                {
                    request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
                }
            }

            var response = await _s3Client.PutObjectAsync(request, cancellationToken);

            _logger.LogInformation(
                "Uploaded file {FileName} to S3 bucket {Bucket} with key {Key}",
                fileName, _options.BucketName, key);

            return new S3UploadResult
            {
                Success = true,
                Key = key,
                ETag = response.ETag,
                FileHash = fileHash,
                FileSizeBytes = fileStream.Length,
                VersionId = response.VersionId
            };
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading file {FileName}", fileName);
            return new S3UploadResult
            {
                Success = false,
                ErrorMessage = $"S3 Error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            return new S3UploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<S3DownloadResult> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);

            var metadata = new Dictionary<string, string>();
            foreach (var metaKey in response.Metadata.Keys)
            {
                metadata[metaKey] = response.Metadata[metaKey];
            }

            return new S3DownloadResult
            {
                Success = true,
                Content = response.ResponseStream,
                ContentType = response.Headers.ContentType,
                ContentLength = response.Headers.ContentLength,
                ETag = response.ETag,
                Metadata = metadata
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found in S3: {Key}", key);
            return new S3DownloadResult
            {
                Success = false,
                ErrorMessage = "File not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {Key}", key);
            return new S3DownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public string GetPreSignedUrl(string key, int? expirationMinutes = null)
    {
        var expiration = expirationMinutes ?? _options.PreSignedUrlExpirationMinutes;
        
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expiration),
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
            _logger.LogInformation("Deleted file from S3: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<S3FileMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            var metadata = new Dictionary<string, string>();
            foreach (var metaKey in response.Metadata.Keys)
            {
                metadata[metaKey] = response.Metadata[metaKey];
            }

            return new S3FileMetadata
            {
                Key = key,
                ContentLength = response.Headers.ContentLength,
                ContentType = response.Headers.ContentType,
                ETag = response.ETag,
                LastModified = response.LastModified,
                Metadata = metadata,
                IsEncrypted = response.ServerSideEncryptionMethod != ServerSideEncryptionMethod.None,
                EncryptionAlgorithm = response.ServerSideEncryptionMethod.ToString()
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey
        };

        if (_options.EnableServerSideEncryption)
        {
            if (!string.IsNullOrEmpty(_options.KmsKeyId))
            {
                request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
                request.ServerSideEncryptionKeyManagementServiceKeyId = _options.KmsKeyId;
            }
            else
            {
                request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
            }
        }

        await _s3Client.CopyObjectAsync(request, cancellationToken);
        _logger.LogInformation("Copied file from {Source} to {Destination}", sourceKey, destinationKey);
    }

    private static async Task<string> ComputeSha256HashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public void Dispose()
    {
        _transferUtility?.Dispose();
        _s3Client?.Dispose();
    }
}
