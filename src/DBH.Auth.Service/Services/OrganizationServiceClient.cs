using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DBH.Auth.Service.Services;

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationServiceClient> _logger;
    private readonly string _baseUrl;

    public OrganizationServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OrganizationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ServiceUrls:OrganizationService"] ?? "http://localhost:5002";
    }

    public async Task<OrganizationServiceResponse<CreateMembershipResponse>> CreateMembershipAsync(
        Guid userId,
        Guid organizationId,
        Guid? departmentId = null,
        string? jobTitle = null)
    {
        try
        {
            var request = new
            {
                userId,
                orgId = organizationId,
                departmentId,
                jobTitle,
                startDate = DateOnly.FromDateTime(DateTime.UtcNow),
                status = "ACTIVE"
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var url = $"{_baseUrl}/api/v1/memberships";
            _logger.LogInformation("Creating membership in Organization Service: {Url}", url);

            var response = await _httpClient.PostAsync(url, jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to create membership in Organization Service. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new OrganizationServiceResponse<CreateMembershipResponse>
                {
                    Success = false,
                    Message = $"Failed to create membership: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<OrganizationServiceResponse<CreateMembershipResponse>>(
                responseContent, options);

            if (result?.Success == true)
            {
                _logger.LogInformation(
                    "Successfully created membership for user {UserId} in organization {OrgId}",
                    userId, organizationId);
            }

            return result ?? new OrganizationServiceResponse<CreateMembershipResponse>
            {
                Success = false,
                Message = "Invalid response from Organization Service"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error while creating membership for user {UserId} in organization {OrgId}",
                userId, organizationId);

            return new OrganizationServiceResponse<CreateMembershipResponse>
            {
                Success = false,
                Message = $"Service communication error: {ex.Message}",
                ErrorCode = "SERVICE_UNAVAILABLE"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating membership for user {UserId} in organization {OrgId}",
                userId, organizationId);

            return new OrganizationServiceResponse<CreateMembershipResponse>
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }
}
