using System.ComponentModel.DataAnnotations;

namespace DBH.Blockchain.Service.DTOs;

public class EncryptIpfsPayloadRequestDto
{
    [Required]
    public string Data { get; set; } = string.Empty;
}

public class EncryptIpfsMultipartRequestDto
{
    public string? Data { get; set; }
    public IFormFile? File { get; set; }
}

public class EncryptIpfsPayloadResponseDto
{
    public string IpfsCid { get; set; } = string.Empty;
    public string WrappedAesKey { get; set; } = string.Empty;
    public string DataHash { get; set; } = string.Empty;
    public bool IsBinary { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public class DecryptIpfsPayloadRequestDto
{
    [Required]
    public string IpfsCid { get; set; } = string.Empty;

    [Required]
    public string WrappedAesKey { get; set; } = string.Empty;
}

public class DecryptIpfsMultipartRequestDto
{
    [Required]
    public string IpfsCid { get; set; } = string.Empty;

    [Required]
    public string WrappedAesKey { get; set; } = string.Empty;
}

public class DecryptIpfsRawFileRequestDto
{
    [Required]
    public string WrappedAesKey { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = default!;
}

public class DecryptIpfsPayloadResponseDto
{
    public string Data { get; set; } = string.Empty;
    public bool IsBinary { get; set; }
    public string? DataBase64 { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public class IpfsRawDownloadResponseDto
{
    public string IpfsCid { get; set; } = string.Empty;
    public string EncryptedData { get; set; } = string.Empty;
}

public class UserEncryptionKeysResponseDto
{
    public Guid UserId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string EncryptedPrivateKey { get; set; } = string.Empty;
}