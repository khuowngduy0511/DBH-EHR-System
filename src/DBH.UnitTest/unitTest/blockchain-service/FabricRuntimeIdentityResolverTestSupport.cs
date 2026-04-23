using System.Net;
using System.Security.Claims;
using DBH.Shared.Infrastructure.Blockchain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DBH.UnitTest.UnitTests;

internal static class FabricRuntimeIdentityResolverTestSupport
{
    internal static FabricRuntimeIdentityResolver CreateResolver(
        FabricOptions fabricOptions,
        FabricCaOptions caOptions,
        HttpContext? httpContext = null,
        Func<HttpRequestMessage, HttpResponseMessage>? organizationServiceResponseFactory = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceUrls:OrganizationService"] = "http://organization_service:5002"
            })
            .Build();

        var responseFactory = organizationServiceResponseFactory ?? (_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var clientFactory = new StubHttpClientFactory(client);

        return new FabricRuntimeIdentityResolver(
            Options.Create(fabricOptions),
            Options.Create(caOptions),
            accessor,
            clientFactory,
            configuration,
            NullLogger<FabricRuntimeIdentityResolver>.Instance);
    }

    internal static HttpContext CreateHttpContext(Guid orgId, string? bearerToken = null)
    {
        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new(ClaimTypes.GroupSid, orgId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test-auth"));

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            context.Request.Headers.Authorization = new StringValues($"Bearer {bearerToken}");
        }

        return context;
    }

    internal sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}