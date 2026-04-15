using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
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
    private static readonly object _httpLogLock = new();
    private static readonly string _httpLogPath = Path.Combine(AppContext.BaseDirectory, "dbh-unittest-http.log");
    private static bool _httpLogInitialized;
    private static bool _httpLogPathAnnounced;

    private const int MaxRequestRetries = 4;
    private const int RetryBaseDelayMs = 200;
    private const int ServiceReadyTimeoutSeconds = 10;
    private const bool DefaultSkipWhenServiceUnavailable = true;
    private const int HttpLogBodyMaxLength = 4000;

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

    protected Task<HttpResponseMessage> GetWithRetryAsync(HttpClient client, string requestUri, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        return ExecuteWithRetryAsync(
            client,
            HttpMethod.Get,
            requestUri,
            () => client.GetAsync(requestUri, cancellationToken),
            cancellationToken,
            testName,
            requestBody: "<none>");
    }

    protected Task<HttpResponseMessage> PostAsJsonWithRetryAsync<T>(HttpClient client, string requestUri, T payload, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        return ExecuteWithRetryAsync(
            client,
            HttpMethod.Post,
            requestUri,
            () => client.PostAsJsonAsync(requestUri, payload, cancellationToken),
            cancellationToken,
            testName,
            requestBody: SerializeForLog(payload));
    }

    protected Task<HttpResponseMessage> PutAsJsonWithRetryAsync<T>(HttpClient client, string requestUri, T payload, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        return ExecuteWithRetryAsync(
            client,
            HttpMethod.Put,
            requestUri,
            () => client.PutAsJsonAsync(requestUri, payload, cancellationToken),
            cancellationToken,
            testName,
            requestBody: SerializeForLog(payload));
    }

    protected async Task<HttpResponseMessage> PutWithRetryAsync(HttpClient client, string requestUri, HttpContent? content = null, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        var requestBody = await SnapshotHttpContentAsync(content);
        return await ExecuteWithRetryAsync(
            client,
            HttpMethod.Put,
            requestUri,
            () => client.PutAsync(requestUri, content, cancellationToken),
            cancellationToken,
            testName,
            requestBody);
    }

    protected async Task<HttpResponseMessage> PostWithRetryAsync(HttpClient client, string requestUri, HttpContent? content = null, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        var requestBody = await SnapshotHttpContentAsync(content);
        return await ExecuteWithRetryAsync(
            client,
            HttpMethod.Post,
            requestUri,
            () => client.PostAsync(requestUri, content, cancellationToken),
            cancellationToken,
            testName,
            requestBody);
    }

    protected Task<HttpResponseMessage> DeleteWithRetryAsync(HttpClient client, string requestUri, CancellationToken cancellationToken = default, [CallerMemberName] string testName = "")
    {
        return ExecuteWithRetryAsync(
            client,
            HttpMethod.Delete,
            requestUri,
            () => client.DeleteAsync(requestUri, cancellationToken),
            cancellationToken,
            testName,
            requestBody: "<none>");
    }

    private static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        Func<Task<HttpResponseMessage>> requestFactory,
        CancellationToken cancellationToken,
        string testName,
        string requestBody)
    {
        HttpResponseMessage? lastResponse = null;
        var url = ResolveRequestUrl(client, requestUri);

        for (int attempt = 1; attempt <= MaxRequestRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await requestFactory();
                stopwatch.Stop();
                var responseBody = await SnapshotHttpContentAsync(response.Content);
                LogHttpCall(method, url, response.StatusCode, stopwatch.Elapsed, attempt, testName, requestBody, responseBody);

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
                stopwatch.Stop();
                LogHttpFailure(method, url, ex, stopwatch.Elapsed, attempt, testName, requestBody);
            }
            catch (TaskCanceledException) when (attempt < MaxRequestRetries)
            {
                // Retry transient timeout while service is warming up.
                stopwatch.Stop();
                LogHttpFailure(method, url, new TimeoutException("Request timed out while retrying."), stopwatch.Elapsed, attempt, testName, requestBody);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(RetryBaseDelayMs * attempt), cancellationToken);
        }

        if (lastResponse is not null)
        {
            return lastResponse;
        }

        throw new HttpRequestException($"Network error: HTTP request failed after {MaxRequestRetries} attempts.");
    }

    private static string ResolveRequestUrl(HttpClient client, string requestUri)
    {
        if (Uri.TryCreate(requestUri, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        if (client.BaseAddress is null)
        {
            return requestUri;
        }

        return new Uri(client.BaseAddress, requestUri).ToString();
    }

    private static void LogHttpCall(
        HttpMethod method,
        string url,
        HttpStatusCode statusCode,
        TimeSpan duration,
        int attempt,
        string testName,
        string requestBody,
        string responseBody)
    {
        WriteHttpLog(
            $"[{DateTime.UtcNow:O}] test={FormatTestName(testName)} attempt={attempt} {method} {url} => {(int)statusCode} {statusCode} ({duration.TotalMilliseconds:F0} ms){Environment.NewLine}" +
            $"  request: {FormatBodyForLog(requestBody)}{Environment.NewLine}" +
            $"  response: {FormatBodyForLog(responseBody)}");
    }

    private static void LogHttpFailure(HttpMethod method, string url, Exception exception, TimeSpan duration, int attempt, string testName, string requestBody)
    {
        WriteHttpLog(
            $"[{DateTime.UtcNow:O}] test={FormatTestName(testName)} attempt={attempt} {method} {url} => FAILED ({duration.TotalMilliseconds:F0} ms) {exception.GetType().Name}: {exception.Message}{Environment.NewLine}" +
            $"  request: {FormatBodyForLog(requestBody)}");
    }

    private static void WriteHttpLog(string message)
    {
        lock (_httpLogLock)
        {
            if (!_httpLogInitialized)
            {
                File.WriteAllText(_httpLogPath, string.Empty);
                _httpLogInitialized = true;
            }

            if (!_httpLogPathAnnounced)
            {
                Console.WriteLine($"HTTP test log: {_httpLogPath}");
                _httpLogPathAnnounced = true;
            }

            File.AppendAllText(_httpLogPath, message + Environment.NewLine);
        }

        Console.WriteLine(message);
    }

    private static string FormatTestName(string testName)
    {
        return string.IsNullOrWhiteSpace(testName) ? "UnknownTest" : testName;
    }

    private static string SerializeForLog<T>(T payload)
    {
        if (payload is null)
        {
            return "<null>";
        }

        try
        {
            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        catch
        {
            return payload.ToString() ?? "<unserializable-payload>";
        }
    }

    private static async Task<string> SnapshotHttpContentAsync(HttpContent? content)
    {
        if (content is null)
        {
            return "<none>";
        }

        try
        {
            var body = await content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body) ? "<empty>" : body;
        }
        catch (Exception ex)
        {
            return $"<unavailable: {ex.GetType().Name}: {ex.Message}>";
        }
    }

    private static string FormatBodyForLog(string body)
    {
        var normalized = NormalizeWhitespace(body);
        if (normalized.Length <= HttpLogBodyMaxLength)
        {
            return normalized;
        }

        return normalized[..HttpLogBodyMaxLength] + "... <truncated>";
    }

    private static string NormalizeWhitespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        return value
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
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
    protected async Task<JsonElement> AuthenticateAsync(HttpClient client, string email, string password, [CallerMemberName] string testName = "")
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
            var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginPayload, default, testName);
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
    protected async Task<JsonElement> AuthenticateAsAdminAsync(HttpClient client, [CallerMemberName] string testName = "")
    {
        return await AuthenticateAsync(client, TestSeedData.AdminEmail, TestSeedData.AdminPassword, testName);
    }

    /// <summary>
    /// Authenticate using Doctor seed credentials.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsDoctorAsync(HttpClient client, [CallerMemberName] string testName = "")
    {
        return await AuthenticateAsync(client, TestSeedData.DoctorEmail, TestSeedData.DoctorPassword, testName);
    }

    /// <summary>
    /// Authenticate using Patient seed credentials.
    /// </summary>
    protected async Task<JsonElement> AuthenticateAsPatientAsync(HttpClient client, [CallerMemberName] string testName = "")
    {
        return await AuthenticateAsync(client, TestSeedData.PatientEmail, TestSeedData.PatientPassword, testName);
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
