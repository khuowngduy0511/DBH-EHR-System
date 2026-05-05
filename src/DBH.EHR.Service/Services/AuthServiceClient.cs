using System.Net.Http.Headers;
using System.Text.Json;
using DBH.EHR.Service.Models.DTOs;

namespace DBH.EHR.Service.Services;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AuthServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId, string bearerToken)
    {
        return GetUserIdAsync($"api/v1/auth/user-id?patientId={patientId}", bearerToken, "patient", patientId);
    }

    public Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId, string bearerToken)
    {
        return GetUserIdAsync($"api/v1/auth/user-id?doctorId={doctorId}", bearerToken, "doctor", doctorId);
    }

    public async Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId, string bearerToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await client.GetAsync($"api/v1/auth/users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth Service returned {StatusCode} for user profile {UserId}", response.StatusCode, userId);
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
            _logger.LogWarning(ex, "Failed to fetch detailed user profile from Auth Service for {UserId}", userId);
            return null;
        }
    }

    private async Task<Guid?> GetUserIdAsync(string requestUri, string bearerToken, string profileType, Guid profileId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth Service returned {StatusCode} for {ProfileType} profile {ProfileId}", response.StatusCode, profileType, profileId);
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
    public async Task<List<Guid>> SearchUserIdsAsync(string keyword, string bearerToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var encodedKeyword = Uri.EscapeDataString(keyword);
            var response = await client.GetAsync($"api/v1/auth/users/search-ids?keyword={encodedKeyword}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth Service returned {StatusCode} for search-ids", response.StatusCode);
                return new List<Guid>();
            }

            var json = await response.Content.ReadAsStringAsync();
            
            var ids = JsonSerializer.Deserialize<List<Guid>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return ids ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search user ids by keyword {Keyword}", keyword);
            return new List<Guid>();
        }
    }
}
