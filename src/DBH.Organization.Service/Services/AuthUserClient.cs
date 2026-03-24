using System.Net.Http.Headers;
using System.Text.Json;

namespace DBH.Organization.Service.Services;

public class AuthUserClient : IAuthUserClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthUserClient> _logger;

    public AuthUserClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthUserClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DoctorUserInfoDto?> GetDoctorByUserIdInMyOrganizationAsync(string bearerToken, Guid orgId, Guid userId)
    {
        var baseUrl = _configuration["ServiceUrls:AuthService"] ?? "http://auth_service:5101";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/v1/doctors/organization/me/{userId}?orgId={orgId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to fetch doctor {UserId} from auth service. Status: {StatusCode}. Body: {Body}",
                    userId,
                    response.StatusCode,
                    body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DoctorUserInfoDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch doctor user info from auth service for user {UserId}", userId);
            return null;
        }
    }
}
