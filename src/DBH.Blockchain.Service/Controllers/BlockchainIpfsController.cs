using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.Blockchain.Service.DTOs;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Ipfs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Blockchain.Service.Controllers;

[ApiController]
[Route("api/v1/blockchain/ipfs")]
[Produces("application/json")]
[Authorize]
public class BlockchainIpfsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlockchainIpfsController> _logger;

    public BlockchainIpfsController(
        IHttpClientFactory httpClientFactory,
        ILogger<BlockchainIpfsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{cid}/download")]
    [ProducesResponseType(typeof(IpfsRawDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IpfsRawDownloadResponseDto>> DownloadIpfsRaw(string cid)
    {
        var encryptedData = await DownloadIpfsRawAsync(cid);
        if (string.IsNullOrWhiteSpace(encryptedData))
        {
            return NotFound(new { Message = "IPFS payload not found" });
        }

        return Ok(new IpfsRawDownloadResponseDto
        {
            IpfsCid = cid,
            EncryptedData = encryptedData
        });
    }

    [HttpGet("records/{ehrId:guid}/download")]
    [ProducesResponseType(typeof(IpfsRawDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IpfsRawDownloadResponseDto>> DownloadLatestIpfsRawByEhrId(Guid ehrId)
    {
        var ehrClient = _httpClientFactory.CreateClient("EhrService");
        var bearerToken = GetBearerTokenFromContext();
        var response = await SendAuthorizedGetAsync(ehrClient, $"/api/v1/ehr/records/{ehrId}/ipfs/download", bearerToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new { Message = "EHR/IPFS payload not found for this ehrId" });
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to proxy latest EHR IPFS payload for {EhrId}. Status={StatusCode}, Body={Body}", ehrId, response.StatusCode, errorBody);
            return StatusCode((int)response.StatusCode, new { Message = "Failed to fetch EHR IPFS payload" });
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<IpfsRawDownloadResponseDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result == null
            ? StatusCode(StatusCodes.Status502BadGateway, new { Message = "Invalid response from EHR service" })
            : Ok(result);
    }

    [HttpPost("encrypt")]
    [ProducesResponseType(typeof(EncryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncryptIpfsPayloadResponseDto>> EncryptToIpfs([FromBody] EncryptIpfsPayloadRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await EncryptToIpfsForCurrentUserAsync(
            Encoding.UTF8.GetBytes(request.Data),
            isBinary: false,
            fileName: null,
            contentType: "text/plain");
        if (result == null)
        {
            return BadRequest(new { Message = "Failed to encrypt payload for current user" });
        }

        return Ok(result);
    }

    [HttpPost("encrypt/multipart")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EncryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncryptIpfsPayloadResponseDto>> EncryptToIpfsMultipart([FromForm] EncryptIpfsMultipartRequestDto request)
    {
        if (request.File != null)
        {
            await using var stream = request.File.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var fileBytes = memoryStream.ToArray();
            if (fileBytes.Length == 0)
            {
                return BadRequest(new { Message = "Uploaded file is empty." });
            }

            var resultFromFile = await EncryptToIpfsForCurrentUserAsync(
                fileBytes,
                isBinary: true,
                fileName: request.File.FileName,
                contentType: request.File.ContentType);

            if (resultFromFile == null)
            {
                return BadRequest(new { Message = "Failed to encrypt uploaded file for current user" });
            }

            return Ok(resultFromFile);
        }

        if (string.IsNullOrWhiteSpace(request.Data))
        {
            return BadRequest(new { Message = "Provide either form field 'data' or file upload in 'file'." });
        }

        var result = await EncryptToIpfsForCurrentUserAsync(
            Encoding.UTF8.GetBytes(request.Data),
            isBinary: false,
            fileName: null,
            contentType: "text/plain");
        if (result == null)
        {
            return BadRequest(new { Message = "Failed to encrypt payload for current user" });
        }

        return Ok(result);
    }

    [HttpPost("decrypt")]
    [ProducesResponseType(typeof(DecryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DecryptIpfsPayloadResponseDto>> DecryptFromIpfs([FromBody] DecryptIpfsPayloadRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var decrypted = await DecryptIpfsForCurrentUserAsync(request.IpfsCid, request.WrappedAesKey);
        if (decrypted == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        return Ok(decrypted);
    }

    [HttpPost("decrypt/multipart")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DecryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DecryptIpfsPayloadResponseDto>> DecryptFromIpfsMultipart([FromForm] DecryptIpfsMultipartRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var decrypted = await DecryptIpfsForCurrentUserAsync(request.IpfsCid, request.WrappedAesKey);
        if (decrypted == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        return Ok(decrypted);
    }

    [HttpPost("decrypt/raw-file")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DecryptFromRawFile([FromForm] DecryptIpfsRawFileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.File.Length == 0)
        {
            return BadRequest(new { Message = "Uploaded raw IPFS file is empty." });
        }

        string encryptedText;
        await using (var stream = request.File.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            encryptedText = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return BadRequest(new { Message = "Uploaded raw IPFS file does not contain encrypted content." });
        }

        var decrypted = await DecryptEncryptedPayloadAsync(encryptedText, request.WrappedAesKey);
        if (decrypted == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        if (decrypted.IsBinary)
        {
            if (string.IsNullOrWhiteSpace(decrypted.DataBase64))
            {
                return BadRequest(new { Message = "Binary payload is missing its data." });
            }

            var fileBytes = Convert.FromBase64String(decrypted.DataBase64);
            var downloadFileName = BuildDownloadFileName(decrypted.FileName, decrypted.ContentType, request.File.FileName);
            return File(fileBytes, decrypted.ContentType ?? "application/octet-stream", downloadFileName);
        }

        var textBytes = Encoding.UTF8.GetBytes(decrypted.Data);
        var textFileName = BuildDownloadFileName(decrypted.FileName, decrypted.ContentType, request.File.FileName, ".txt");
        return File(textBytes, decrypted.ContentType ?? "text/plain", textFileName);
    }

    [HttpGet("keys/current")]
    [ProducesResponseType(typeof(UserEncryptionKeysResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserEncryptionKeysResponseDto>> GetCurrentUserEncryptionKeys()
    {
        var userId = GetCurrentUserIdFromContext();
        if (!userId.HasValue)
        {
            return Unauthorized(new { Message = "Unable to resolve current user id from token" });
        }

        var keys = await GetUserKeysAsync(userId.Value);
        if (keys == null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { Message = "Failed to retrieve user encryption keys" });
        }

        return Ok(new UserEncryptionKeysResponseDto
        {
            UserId = keys.UserId,
            PublicKey = keys.PublicKey,
            EncryptedPrivateKey = keys.EncryptedPrivateKey
        });
    }

    [HttpGet("keys/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserEncryptionKeysResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserEncryptionKeysResponseDto>> GetEncryptionKeysByUserId(Guid userId)
    {
        var keys = await GetUserKeysAsync(userId);
        if (keys == null)
        {
            return NotFound(new { Message = "User keys not found" });
        }

        return Ok(new UserEncryptionKeysResponseDto
        {
            UserId = keys.UserId,
            PublicKey = keys.PublicKey,
            EncryptedPrivateKey = keys.EncryptedPrivateKey
        });
    }

    private async Task<EncryptIpfsPayloadResponseDto?> EncryptToIpfsForCurrentUserAsync(
        byte[] payloadBytes,
        bool isBinary,
        string? fileName,
        string? contentType)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue || payloadBytes.Length == 0)
        {
            return null;
        }

        var keys = await GetUserKeysAsync(currentUserId.Value);
        if (keys == null || string.IsNullOrWhiteSpace(keys.PublicKey))
        {
            return null;
        }

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        var envelope = new IpfsEncryptedPayloadEnvelope
        {
            IsBinary = isBinary,
            Data = isBinary
                ? Convert.ToBase64String(payloadBytes)
                : Encoding.UTF8.GetString(payloadBytes),
            FileName = fileName,
            ContentType = contentType
        };

        var envelopeJson = JsonSerializer.Serialize(envelope);
        var encryptedDataStr = SymmetricEncryptionService.EncryptString(envelopeJson, blueKeyBytes);
        var wrappedAesKey = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);

        string? ipfsCid = null;
        var tempFile = Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null && !string.IsNullOrWhiteSpace(uploadRes.Hash))
            {
                ipfsCid = uploadRes.Hash;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted payload to IPFS for current user {UserId}", currentUserId.Value);
            return null;
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);
            }
        }

        if (string.IsNullOrWhiteSpace(ipfsCid))
        {
            return null;
        }

        return new EncryptIpfsPayloadResponseDto
        {
            IpfsCid = ipfsCid,
            WrappedAesKey = wrappedAesKey,
            DataHash = ComputeHash(payloadBytes),
            IsBinary = isBinary,
            FileName = fileName,
            ContentType = contentType
        };
    }

    private async Task<DecryptIpfsPayloadResponseDto?> DecryptIpfsForCurrentUserAsync(string ipfsCid, string wrappedAesKey)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue
            || string.IsNullOrWhiteSpace(ipfsCid)
            || string.IsNullOrWhiteSpace(wrappedAesKey))
        {
            return null;
        }

        var encryptedText = await DownloadIpfsRawAsync(ipfsCid);
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return null;
        }

        return await DecryptEncryptedPayloadAsync(encryptedText, wrappedAesKey);
    }

    private async Task<DecryptIpfsPayloadResponseDto?> DecryptEncryptedPayloadAsync(string encryptedText, string wrappedAesKey)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue
            || string.IsNullOrWhiteSpace(encryptedText)
            || string.IsNullOrWhiteSpace(wrappedAesKey))
        {
            return null;
        }

        var keys = await GetUserKeysAsync(currentUserId.Value);
        if (keys == null || string.IsNullOrWhiteSpace(keys.EncryptedPrivateKey))
        {
            return null;
        }

        try
        {
            var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);
            var blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(wrappedAesKey, privateKey);
            var decryptedJson = SymmetricEncryptionService.DecryptString(encryptedText, blueKeyBytes);
            return ParseDecryptedPayload(decryptedJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt IPFS payload for current user {UserId}", currentUserId.Value);
            return null;
        }
    }

    private async Task<string?> DownloadIpfsRawAsync(string cid)
    {
        try
        {
            var downloadedPath = await IpfsClientService.RetrieveAsync(cid);
            if (!System.IO.File.Exists(downloadedPath))
            {
                return null;
            }

            var encryptedText = await System.IO.File.ReadAllTextAsync(downloadedPath);
            System.IO.File.Delete(downloadedPath);
            return encryptedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download IPFS payload by CID {Cid}", cid);
            return null;
        }
    }

    private async Task<AuthUserKeysDto?> GetUserKeysAsync(Guid userId)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var response = await SendAuthorizedGetAsync(authClient, $"/api/v1/auth/{userId}/keys", bearerToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthUserKeysDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private async Task<HttpResponseMessage> SendAuthorizedGetAsync(HttpClient client, string requestUri, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await client.SendAsync(request);
    }

    private string? GetBearerTokenFromContext()
    {
        var authHeader = HttpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader["Bearer ".Length..].Trim();
    }

    private Guid? GetCurrentUserIdFromContext()
    {
        var candidates = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("userId"),
            User.FindFirstValue("uid"),
            User.FindFirstValue("sub"),
            User.FindFirstValue("id")
        };

        foreach (var value in candidates)
        {
            if (Guid.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static DecryptIpfsPayloadResponseDto ParseDecryptedPayload(string decryptedValue)
    {
        if (!string.IsNullOrWhiteSpace(decryptedValue))
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<IpfsEncryptedPayloadEnvelope>(decryptedValue, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope != null && envelope.Data != null)
                {
                    return new DecryptIpfsPayloadResponseDto
                    {
                        Data = envelope.IsBinary ? string.Empty : envelope.Data,
                        IsBinary = envelope.IsBinary,
                        DataBase64 = envelope.IsBinary ? envelope.Data : null,
                        FileName = envelope.FileName,
                        ContentType = envelope.ContentType
                    };
                }
            }
            catch (JsonException)
            {
                // Backward compatibility for records encrypted before envelope support.
            }
        }

        return new DecryptIpfsPayloadResponseDto
        {
            Data = decryptedValue,
            IsBinary = false,
            DataBase64 = null,
            FileName = null,
            ContentType = "text/plain"
        };
    }

    private static string ComputeHash(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(content);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildDownloadFileName(string? envelopeFileName, string? contentType, string fallbackSourceFileName, string? fallbackExtension = null)
    {
        if (!string.IsNullOrWhiteSpace(envelopeFileName))
        {
            return envelopeFileName;
        }

        var extension = fallbackExtension ?? GetExtensionForContentType(contentType);
        var sourceName = Path.GetFileNameWithoutExtension(fallbackSourceFileName);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            sourceName = "decrypted";
        }

        return string.Concat(sourceName, extension);
    }

    private static string GetExtensionForContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return ".bin";
        }

        return contentType.Split(';')[0].Trim().ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "application/json" => ".json",
            "text/html" => ".html",
            "video/mp4" => ".mp4",
            "application/octet-stream" => ".bin",
            _ => ".bin"
        };
    }

    private sealed class IpfsEncryptedPayloadEnvelope
    {
        public bool IsBinary { get; set; }
        public string Data { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
    }

    private sealed class AuthUserKeysDto
    {
        public Guid UserId { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string EncryptedPrivateKey { get; set; } = string.Empty;
    }
}