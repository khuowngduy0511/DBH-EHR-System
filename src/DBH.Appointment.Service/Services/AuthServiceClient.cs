using System.Net.Http.Headers;
using System.Text.Json;
using DBH.Appointment.Service.DTOs;

namespace DBH.Appointment.Service.Services;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId)
    {
        return GetUserIdAsync($"api/v1/auth/user-id?patientId={patientId}", "patient", patientId);
    }

    public Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId)
    {
        return GetUserIdAsync($"api/v1/auth/user-id?doctorId={doctorId}", "doctor", doctorId);
    }

    public async Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }

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

    private async Task<Guid?> GetUserIdAsync(string requestUri, string profileType, Guid profileId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }

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
}
