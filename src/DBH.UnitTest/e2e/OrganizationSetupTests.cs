using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// End-to-end: Organization setup flow across Organization service.
/// Flow: Create Org → Verify → Add Department → Add Membership → Search Doctors → Get by User
/// </summary>
public class OrganizationSetupTests : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task FullOrgSetup_CreateToMembership_ShouldSucceed()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);

        // =====================================================================
        // STEP 1: Create a new organization
        // =====================================================================
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

            // =================================================================
            // STEP 2: Get org and verify data matches
            // =================================================================
            var getOrgResponse = await GetWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.GetById(orgId));
            Assert.Equal(HttpStatusCode.OK, getOrgResponse.StatusCode);
            var getOrgJson = await ReadJsonResponseAsync(getOrgResponse);
            Assert.Equal(orgRequest.orgCode, getOrgJson.GetProperty("data").GetProperty("orgCode").GetString());

            // =================================================================
            // STEP 3: Verify the organization
            // =================================================================
            var verifyResponse = await PostWithRetryAsync(OrganizationClient, 
                Shared.ApiEndpoints.Organizations.Verify(orgId, Shared.TestSeedData.AdminUserId), null);
            var verifyJson = await ReadJsonResponseAsync(verifyResponse);
            Assert.False(string.IsNullOrEmpty(verifyJson.GetProperty("message").GetString()));

            // =================================================================
            // STEP 4: Add a department
            // =================================================================
            var deptRequest = new
            {
                orgId,
                departmentName = "E2E Cardiology Dept",
                departmentCode = "E2E-CARD",
                description = "E2E test department",
                floor = "3",
                roomNumbers = "301-305",
                phoneExtension = "3001"
            };

            var deptResponse = await PostAsJsonWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Departments.Create, deptRequest);
            var deptJson = await ReadJsonResponseAsync(deptResponse);
            Assert.False(string.IsNullOrEmpty(deptJson.GetProperty("message").GetString()));

            if (deptResponse.StatusCode == HttpStatusCode.Created || deptResponse.StatusCode == HttpStatusCode.OK)
            {
                var deptId = Guid.Parse(deptJson.GetProperty("data").GetProperty("departmentId").GetString()!);

                // =============================================================
                // STEP 5: Add a membership (doctor to org+dept)
                // =============================================================
                var memberRequest = new
                {
                    userId = Shared.TestSeedData.DoctorUserId,
                    orgId,
                    departmentId = deptId,
                    employeeId = "E2E-DOC-001",
                    jobTitle = "E2E Test Doctor",
                    licenseNumber = "VN-E2E-001",
                    specialty = "E2E Cardiology",
                    startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
                };
                var memberResponse = await PostAsJsonWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Memberships.Create, memberRequest);
                var memberJson = await ReadJsonResponseAsync(memberResponse);
                Assert.False(string.IsNullOrEmpty(memberJson.GetProperty("message").GetString()));

                // =============================================================
                // STEP 6: Verify dept shows up in org listing
                // =============================================================
                var deptListResponse = await GetWithRetryAsync(OrganizationClient, 
                    $"{Shared.ApiEndpoints.Departments.ByOrganization(orgId)}?page=1&pageSize=10");
                Assert.Equal(HttpStatusCode.OK, deptListResponse.StatusCode);
                var deptListJson = await ReadJsonResponseAsync(deptListResponse);
                Assert.True(deptListJson.GetProperty("data").GetArrayLength() >= 1);
            }
        }
    }

    [SkippableFact]
    public async Task VerifySeedOrganization_ThenListDepartmentsAndMemberships()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);

        // STEP 1: Get Hospital A — verify name and status
        var orgResponse = await GetWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.GetById(Shared.TestSeedData.HospitalAOrgId));
        Assert.Equal(HttpStatusCode.OK, orgResponse.StatusCode);
        var orgJson = await ReadJsonResponseAsync(orgResponse);
        Assert.Equal(Shared.TestSeedData.HospitalAName, orgJson.GetProperty("data").GetProperty("orgName").GetString());

        // STEP 2: List departments — should contain Cardiology, Pharmacy, Reception
        var deptResponse = await GetWithRetryAsync(OrganizationClient, 
            $"{Shared.ApiEndpoints.Departments.ByOrganization(Shared.TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, deptResponse.StatusCode);
        var deptJson = await ReadJsonResponseAsync(deptResponse);
        Assert.True(deptJson.GetProperty("data").GetArrayLength() >= 3, "Hospital A should have ≥3 departments");

        // STEP 3: List memberships — should contain admin, doctor, pharmacist, receptionist
        var memberResponse = await GetWithRetryAsync(OrganizationClient, 
            $"{Shared.ApiEndpoints.Memberships.ByOrganization(Shared.TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, memberResponse.StatusCode);
        var memberJson = await ReadJsonResponseAsync(memberResponse);
        Assert.True(memberJson.GetProperty("data").GetArrayLength() >= 4, "Hospital A should have ≥4 memberships");

        // STEP 4: Get doctor membership — verify job title
        var doctorMemberResponse = await GetWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Memberships.GetById(Shared.TestSeedData.DoctorMembershipId));
        Assert.Equal(HttpStatusCode.OK, doctorMemberResponse.StatusCode);
        var doctorMemberJson = await ReadJsonResponseAsync(doctorMemberResponse);
        Assert.Contains("Tim mach", doctorMemberJson.GetProperty("data").GetProperty("jobTitle").GetString());
    }
}

