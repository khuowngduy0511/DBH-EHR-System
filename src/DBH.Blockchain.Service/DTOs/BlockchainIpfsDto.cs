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

public class DecryptIpfsPayloadResponseDto
{
    public string Data { get; set; } = string.Empty;
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