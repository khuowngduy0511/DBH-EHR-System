using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class FullOrgSetup_CreateToMembership_ShouldSucceed : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

    [SkippableFact]
    public async Task FullOrgSetup_CreateToMembership_ShouldSucceed_Test()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);

        var orgRequest = new
        {
            orgName = $"E2E Hospital {Guid.NewGuid():N}".Substring(0, 30),
            orgCode = $"E2E{Random.Shared.Next(1000, 9999)}",
            orgType = "HOSPITAL",
            licenseNumber = $"E2E-LIC-{Random.Shared.Next(100, 999)}",
            taxId = $"030{Random.Shared.Next(1000000, 9999999)}",
            address = "{\"line\":[\"123 E2E Street\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 1\",\"country\":\"VN\"}",
            contactInfo = "{\"phone\":\"028-1234-5678\",\"email\":\"e2e@hospital.com\"}",
            timezone = "Asia/Ho_Chi_Minh"
        };

        var orgResponse = await PostAsJsonWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.Create, orgRequest);
        var orgJson = await ReadJsonResponseAsync(orgResponse);
        Assert.False(string.IsNullOrEmpty(orgJson.GetProperty("message").GetString()));

        if (orgResponse.StatusCode == HttpStatusCode.Created || orgResponse.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(orgJson.GetProperty("success").GetBoolean());
            var orgId = Guid.Parse(orgJson.GetProperty("data").GetProperty("orgId").GetString()!);

            var getOrgResponse = await GetWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.GetById(orgId));
            Assert.Equal(HttpStatusCode.OK, getOrgResponse.StatusCode);

            var verifyResponse = await PostWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.Verify(orgId, Shared.TestSeedData.AdminUserId), null);
            var verifyJson = await ReadJsonResponseAsync(verifyResponse);
            Assert.False(string.IsNullOrEmpty(verifyJson.GetProperty("message").GetString()));
        }
    }
}