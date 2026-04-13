using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace DBH.UnitTest.Shared;

/// <summary>
/// Base class for all API integration tests.
/// Provides HttpClient instances pre-configured with service base URLs and authentication helpers.
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    protected readonly IConfiguration Configuration;
    protected readonly HttpClient GatewayClient;
    protected readonly HttpClient AuthClient;
    protected readonly HttpClient OrganizationClient;
    protected readonly HttpClient EhrClient;
    protected readonly HttpClient ConsentClient;
    protected readonly HttpClient AuditClient;
    protected readonly HttpClient NotificationClient;
    protected readonly HttpClient AppointmentClient;
    protected readonly HttpClient PaymentClient;

    private readonly List<HttpClient> _clients = new();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _tokenCache = new();
    private static readonly System.Threading.SemaphoreSlim _tokenLock = new(1, 1);

    protected ApiTestBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        GatewayClient = CreateClient("Gateway");
        AuthClient = CreateClient("AuthService");
        OrganizationClient = CreateClient("OrganizationService");
        EhrClient = CreateClient("EhrService");
        ConsentClient = CreateClient("ConsentService");
        AuditClient = CreateClient("AuditService");
        NotificationClient = CreateClient("NotificationService");
        AppointmentClient = CreateClient("AppointmentService");
        PaymentClient = CreateClient("PaymentService");
    }

    private HttpClient CreateClient(string serviceName)
    {
        var baseUrl = Configuration[$"ServiceUrls:{serviceName}"]
            ?? throw new InvalidOperationException($"ServiceUrls:{serviceName} not configured.");

        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _clients.Add(client);
        return client;
    }

    /// <summary>
    /// Authenticate with the Auth service and set the Bearer token on the given client.
    /// Returns the parsed JSON response for further assertions.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsync(HttpClient client, string email, string password)
    {
        if (_tokenCache.TryGetValue(email, out var cachedToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken);
            return JsonDocument.Parse("{}").RootElement;
        }

        await _tokenLock.WaitAsync();
        try
        {
            if (_tokenCache.TryGetValue(email, out cachedToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken);
                return JsonDocument.Parse("{}").RootElement;
            }

            var loginPayload = new { email, password };
            var response = await AuthClient.PostAsJsonAsync(ApiEndpoints.Auth.Login, loginPayload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = json.GetProperty("token").GetString()
                ?? throw new InvalidOperationException("Failed to retrieve access token.");

            _tokenCache[email] = token;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return json;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Authenticate using Admin seed credentials and set the token on the given client.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsAdminAsync(HttpClient client)
    {
        return await AuthenticateAsync(client, TestSeedData.AdminEmail, TestSeedData.AdminPassword);
    }

    /// <summary>
    /// Authenticate using Doctor seed credentials.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsDoctorAsync(HttpClient client)
    {
        return await AuthenticateAsync(client, TestSeedData.DoctorEmail, TestSeedData.DoctorPassword);
    }

    /// <summary>
    /// Authenticate using Patient seed credentials.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsPatientAsync(HttpClient client)
    {
        return await AuthenticateAsync(client, TestSeedData.PatientEmail, TestSeedData.PatientPassword);
    }

    /// <summary>
    /// Helper to deserialize response body into JsonElement, asserting success status.
    /// </summary>
    protected static async Task<JsonElement> ReadJsonResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(content);
        }
        catch (JsonException)
        {
            throw new Exception($"Failed to parse JSON. Status: {response.StatusCode}. Content: {content}");
        }
    }

    /// <summary>
    /// Helper to create StringContent from an object as JSON.
    /// </summary>
    protected static StringContent JsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
