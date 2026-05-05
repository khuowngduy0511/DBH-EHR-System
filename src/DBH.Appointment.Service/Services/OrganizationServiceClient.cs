using System.Net.Http.Headers;
using System.Text.Json;

namespace DBH.Appointment.Service.Services;

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
            if (!string.IsNullOrEmpty(bearerToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var encodedKeyword = Uri.EscapeDataString(keyword);
            var response = await client.GetAsync($"api/v1/organizations?page=1&pageSize=100&search={encodedKeyword}");

            if (!response.IsSuccessStatusCode)
            {
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
        catch
        {
            return new List<Guid>();
        }
    }
}
