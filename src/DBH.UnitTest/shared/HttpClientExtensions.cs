namespace DBH.UnitTest.Shared;

public static class HttpClientExtensions
{
    public static HttpRequestMessage CreateRequest(this HttpClient client, HttpMethod method, string requestUri)
    {
        if (Uri.TryCreate(requestUri, UriKind.Absolute, out var absoluteUri))
        {
            return new HttpRequestMessage(method, absoluteUri);
        }

        return new HttpRequestMessage(method, requestUri);
    }
}