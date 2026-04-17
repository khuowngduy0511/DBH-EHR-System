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

        var result = await EncryptToIpfsForCurrentUserAsync(request.Data);
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
        var payload = request.Data;
        if (string.IsNullOrWhiteSpace(payload) && request.File != null)
        {
            using var reader = new StreamReader(request.File.OpenReadStream());
            payload = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest(new { Message = "Provide either form field 'data' or file upload in 'file'." });
        }

        var result = await EncryptToIpfsForCurrentUserAsync(payload);
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
        if (string.IsNullOrWhiteSpace(decrypted))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        return Ok(new DecryptIpfsPayloadResponseDto { Data = decrypted });
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
        if (string.IsNullOrWhiteSpace(decrypted))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        return Ok(new DecryptIpfsPayloadResponseDto { Data = decrypted });
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

    private async Task<EncryptIpfsPayloadResponseDto?> EncryptToIpfsForCurrentUserAsync(string payload)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue || string.IsNullOrWhiteSpace(payload))
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

        var encryptedDataStr = SymmetricEncryptionService.EncryptString(payload, blueKeyBytes);
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
            DataHash = ComputeHash(payload)
        };
    }

    private async Task<string?> DecryptIpfsForCurrentUserAsync(string ipfsCid, string wrappedAesKey)
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

        var keys = await GetUserKeysAsync(currentUserId.Value);
        if (keys == null || string.IsNullOrWhiteSpace(keys.EncryptedPrivateKey))
        {
            return null;
        }

        try
        {
            var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);
            var blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(wrappedAesKey, privateKey);
            return SymmetricEncryptionService.DecryptString(encryptedText, blueKeyBytes);
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

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class AuthUserKeysDto
    {
        public Guid UserId { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string EncryptedPrivateKey { get; set; } = string.Empty;
    }
}