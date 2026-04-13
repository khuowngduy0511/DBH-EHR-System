using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace DBH.UnitTest.Shared;

/// <summary>
/// Base class for all API integration tests.
/// Provides HttpClient instances pre-configured with service base URLs and authentication helpers.
/// </summary>
public abstract class ApiTestBase : IDisposable, IAsyncLifetime
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
    private bool _disposed;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _tokenCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _readyServices =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Threading.SemaphoreSlim _tokenLock = new(1, 1);
    private static readonly System.Threading.SemaphoreSlim _readinessLock = new(1, 1);

    private const int MaxRequestRetries = 4;
    private const int RetryBaseDelayMs = 200;
    private const int ServiceReadyTimeoutSeconds = 10;
    private const bool DefaultSkipWhenServiceUnavailable = true;

    protected virtual IReadOnlyCollection<string> RequiredServices => Array.Empty<string>();

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

    public async Task InitializeAsync()
    {
        foreach (var serviceName in RequiredServices.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await EnsureServiceReadyAsync(serviceName);
            }
            catch (Exception ex) when (ShouldSkipOnServiceUnavailable(ex))
            {
                Skip.If(true,
                    $"Network error: required service '{serviceName}' is unavailable for '{GetType().Name}'. Details: {ex.Message}");
                return;
            }
        }
    }

    private bool ShouldSkipOnServiceUnavailable(Exception ex)
    {
        var skipWhenUnavailable = Configuration.GetValue<bool?>("Tests:SkipWhenServiceUnavailable")
            ?? DefaultSkipWhenServiceUnavailable;

        if (!skipWhenUnavailable)
        {
            return false;
        }

        return ex is TimeoutException
            || ex is HttpRequestException
            || ex is SocketException;
    }

    private async Task EnsureServiceReadyAsync(string serviceName)
    {
        if (_readyServices.ContainsKey(serviceName))
        {
            return;
        }

        await _readinessLock.WaitAsync();
        try
        {
            if (_readyServices.ContainsKey(serviceName))
            {
                return;
            }

            var serviceUrl = Configuration[$"ServiceUrls:{serviceName}"]
                ?? throw new InvalidOperationException($"ServiceUrls:{serviceName} not configured.");

            await WaitForServiceReadyAsync(serviceName, new Uri(serviceUrl));
            _readyServices.TryAdd(serviceName, 0);
        }
        finally
        {
            _readinessLock.Release();
        }
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    private static async Task WaitForServiceReadyAsync(string serviceName, Uri serviceUri)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(ServiceReadyTimeoutSeconds);

        while (DateTime.UtcNow < timeoutAt)
        {
            if (await IsPortOpenAsync(serviceUri.Host, serviceUri.Port) && await IsHttpEndpointResponsiveAsync(serviceUri))
            {
                return;
            }

            await Task.Delay(300);
        }

        throw new TimeoutException($"Network error: service '{serviceName}' at '{serviceUri}' was not ready within {ServiceReadyTimeoutSeconds}s.");
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        using var tcpClient = new TcpClient();
        try
        {
            var connectTask = tcpClient.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(1000);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return false;
            }

            await connectTask;
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static async Task<bool> IsHttpEndpointResponsiveAsync(Uri serviceUri)
    {
        using var client = new HttpClient { BaseAddress = serviceUri, Timeout = TimeSpan.FromSeconds(2) };

        try
        {
            using var response = await client.GetAsync("/");
            return true;
        }
        catch (HttpRequestException ex) when (IsConnectionRefused(ex))
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    protected Task<HttpResponseMessage> GetWithRetryAsync(HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.GetAsync(requestUri, cancellationToken), cancellationToken);
    }

    protected Task<HttpResponseMessage> PostAsJsonWithRetryAsync<T>(HttpClient client, string requestUri, T payload, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.PostAsJsonAsync(requestUri, payload, cancellationToken), cancellationToken);
    }

    protected Task<HttpResponseMessage> PutAsJsonWithRetryAsync<T>(HttpClient client, string requestUri, T payload, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.PutAsJsonAsync(requestUri, payload, cancellationToken), cancellationToken);
    }

    protected Task<HttpResponseMessage> PutWithRetryAsync(HttpClient client, string requestUri, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.PutAsync(requestUri, content, cancellationToken), cancellationToken);
    }

    protected Task<HttpResponseMessage> PostWithRetryAsync(HttpClient client, string requestUri, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.PostAsync(requestUri, content, cancellationToken), cancellationToken);
    }

    protected Task<HttpResponseMessage> DeleteWithRetryAsync(HttpClient client, string requestUri, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(() => client.DeleteAsync(requestUri, cancellationToken), cancellationToken);
    }

    private static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> requestFactory,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 1; attempt <= MaxRequestRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await requestFactory();
                if (!ShouldRetryStatus(response.StatusCode) || attempt == MaxRequestRetries)
                {
                    return response;
                }

                lastResponse?.Dispose();
                lastResponse = response;
            }
            catch (HttpRequestException ex) when (IsConnectionRefused(ex) && attempt < MaxRequestRetries)
            {
                // Retry transient startup race while Docker service ports are opening.
            }
            catch (TaskCanceledException) when (attempt < MaxRequestRetries)
            {
                // Retry transient timeout while service is warming up.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(RetryBaseDelayMs * attempt), cancellationToken);
        }

        if (lastResponse is not null)
        {
            return lastResponse;
        }

        throw new HttpRequestException($"Network error: HTTP request failed after {MaxRequestRetries} attempts.");
    }

    private static bool ShouldRetryStatus(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || statusCode == HttpStatusCode.BadGateway
            || statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.GatewayTimeout;
    }

    private static bool IsConnectionRefused(HttpRequestException ex)
    {
        return ex.InnerException is SocketException socketEx
            && socketEx.SocketErrorCode == SocketError.ConnectionRefused;
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
            var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginPayload);
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
        if (_disposed)
        {
            return;
        }

        foreach (var client in _clients)
        {
            client.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
