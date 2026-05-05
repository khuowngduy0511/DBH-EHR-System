using System.Net.Http.Headers;
using System.Text.Json;

namespace DBH.EHR.Service.Services;

public interface IOrganizationServiceClient
{
    Task<List<Guid>> SearchOrganizationIdsAsync(string keyword, string bearerToken);
}

public class OrganizationServiceClient : IOrganizationServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrganizationServiceClient> _logger;

    public OrganizationServiceClient(IHttpClientFactory httpClientFactory, ILogger<OrganizationServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<Guid>> SearchOrganizationIdsAsync(string keyword, string bearerToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrganizationService");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var encodedKeyword = Uri.EscapeDataString(keyword);
            var response = await client.GetAsync($"api/v1/organizations?page=1&pageSize=100&search={encodedKeyword}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Organization Service returned {StatusCode} for search", response.StatusCode);
                return new List<Guid>();
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var ids = new List<Guid>();
            if (doc.RootElement.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    if (item.TryGetProperty("orgId", out var idElement) && Guid.TryParse(idElement.GetString(), out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search organizations by keyword {Keyword}", keyword);
            return new List<Guid>();
        }
    }
}
