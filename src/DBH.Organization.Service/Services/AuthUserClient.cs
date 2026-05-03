using System.Net.Http.Headers;
using System.Text.Json;
using DBH.Organization.Service.DTOs;

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

    public async Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(string bearerToken, Guid userId)
    {
        var baseUrl = _configuration["ServiceUrls:AuthService"] ?? "http://auth_service:5101";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/v1/auth/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to fetch detailed user profile {UserId} from auth service. Status: {StatusCode}. Body: {Body}",
                    userId,
                    response.StatusCode,
                    body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthUserProfileDetailDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch detailed user profile from auth service for user {UserId}", userId);
            return null;
        }
    }

    public Task<Guid?> GetUserIdByPatientIdAsync(string bearerToken, Guid patientId)
    {
        return GetUserIdAsync(bearerToken, $"api/v1/auth/user-id?patientId={patientId}", "patient", patientId);
    }

    public Task<Guid?> GetUserIdByDoctorIdAsync(string bearerToken, Guid doctorId)
    {
        return GetUserIdAsync(bearerToken, $"api/v1/auth/user-id?doctorId={doctorId}", "doctor", doctorId);
    }

    private async Task<Guid?> GetUserIdAsync(string bearerToken, string relativePath, string profileType, Guid profileId)
    {
        var baseUrl = _configuration["ServiceUrls:AuthService"] ?? "http://auth_service:5101";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/{relativePath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to resolve userId for {ProfileType} profile {ProfileId}. Status: {StatusCode}. Body: {Body}",
                    profileType,
                    profileId,
                    response.StatusCode,
                    body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("userId", out var userIdElement))
            {
                return null;
            }

            return userIdElement.GetGuid();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve userId for {ProfileType} profile {ProfileId}", profileType, profileId);
            return null;
        }
    }

    public async Task<List<Guid>> SearchUserIdsAsync(string bearerToken, string keyword)
    {
        var baseUrl = _configuration["ServiceUrls:AuthService"] ?? "http://auth_service:5101";
        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/v1/auth/users/search-ids?keyword={Uri.EscapeDataString(keyword)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to search user ids from auth service. Keyword: {Keyword}. Status: {StatusCode}. Body: {Body}",
                    keyword,
                    response.StatusCode,
                    body);
                return new List<Guid>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Guid>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search user ids from auth service. Keyword: {Keyword}", keyword);
            return new List<Guid>();
        }
    }
}
