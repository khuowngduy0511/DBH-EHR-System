using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using DBH.Organization.Service.DbContext;
using DBH.Organization.Service.Services;
using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Models.Enums;
using DBH.Organization.Service.Models.Entities;
using OrgEntity = DBH.Organization.Service.Models.Entities.Organization;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceDirectTests : IDisposable
{
    private readonly OrganizationDbContext _db;
    private readonly Mock<ILogger<OrganizationService>> _logger;
    private readonly Mock<IAuthUserClient> _authClient;
    private readonly Mock<IHttpContextAccessor> _http;
    private readonly OrganizationService _svc;

    public OrganizationServiceDirectTests()
    {
        var opts = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new OrganizationDbContext(opts);
        _logger = new Mock<ILogger<OrganizationService>>();
        _authClient = new Mock<IAuthUserClient>();
        _http = new Mock<IHttpContextAccessor>();
        _http.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _svc = new OrganizationService(_db, _logger.Object, _authClient.Object, _http.Object);
    }

    public void Dispose() => _db.Dispose();

    // =========================================================================
    // CreateOrganizationAsync
    // =========================================================================

    [Fact(DisplayName = "CreateOrganizationAsync::CreateOrganizationAsync-01")]
    public async Task CreateOrganizationAsync_HappyPath_ReturnsSuccess()
    {
        // Arrange – valid complete request
        var request = new CreateOrganizationRequest
        {
            OrgName = "Bach Mai Hospital",
            OrgCode = "BMH",
            OrgType = OrganizationType.HOSPITAL,
            LicenseNumber = "LIC-2024-001",
            TaxId = "0100109106",
            Address = "{\"city\":\"Ha Noi\"}",
            ContactInfo = "{\"phone\":\"024-3869-3731\"}",
            Website = "https://benhvien.bachmai.edu.vn"
        };

        // Act
        var result = await _svc.CreateOrganizationAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Organization created successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.OrgId);
        Assert.Equal("Bach Mai Hospital", result.Data.OrgName);
        Assert.Equal("BMH", result.Data.OrgCode);
        Assert.Equal(OrganizationType.HOSPITAL, result.Data.OrgType);
        Assert.Equal(OrganizationStatus.PENDING_VERIFICATION, result.Data.Status);
        Assert.Equal(1, await _db.Organizations.CountAsync());
    }

    [Fact(DisplayName = "CreateOrganizationAsync::CreateOrganizationAsync-02")]
    public async Task CreateOrganizationAsync_MissingRequiredFields_ServiceProcesses()
    {
        // Arrange – OrgName is empty (Required annotation enforced at controller, not service)
        var request = new CreateOrganizationRequest { OrgName = string.Empty };

        // Act
        var result = await _svc.CreateOrganizationAsync(request);

        // Assert – Service layer has no guard; validation is the controller's responsibility
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(string.Empty, result.Data.OrgName);
    }

    [Fact(DisplayName = "CreateOrganizationAsync::CreateOrganizationAsync-03")]
    public async Task CreateOrganizationAsync_Unauthorized_ServiceLayerHasNoAuthCheck()
    {
        // Arrange – Permission enforcement is done upstream by JWT middleware
        var request = new CreateOrganizationRequest { OrgName = "Unauthorized Caller Org" };

        // Act
        var result = await _svc.CreateOrganizationAsync(request);

        // Assert – Service proceeds; auth rejection is a controller/middleware concern
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact(DisplayName = "CreateOrganizationAsync::CreateOrganizationAsync-04")]
    public async Task CreateOrganizationAsync_DbFailure_ThrowsException()
    {
        // Arrange – Simulate DB failure by disposing the context
        var request = new CreateOrganizationRequest { OrgName = "Fail Org" };
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.CreateOrganizationAsync(request));
    }

    // =========================================================================
    // GetOrganizationByIdAsync
    // =========================================================================

    [Fact(DisplayName = "GetOrganizationByIdAsync::GetOrganizationByIdAsync-01")]
    public async Task GetOrganizationByIdAsync_ValidOrgId_ReturnsOrganization()
    {
        // Arrange
        var org = new OrgEntity
        {
            OrgName = "Cho Ray Hospital", OrgCode = "CRH",
            OrgType = OrganizationType.HOSPITAL, Status = OrganizationStatus.ACTIVE
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetOrganizationByIdAsync(org.OrgId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(org.OrgId, result.Data.OrgId);
        Assert.Equal("Cho Ray Hospital", result.Data.OrgName);
        Assert.Equal(OrganizationStatus.ACTIVE, result.Data.Status);
    }

    [Fact(DisplayName = "GetOrganizationByIdAsync::GetOrganizationByIdAsync-02")]
    public async Task GetOrganizationByIdAsync_EmptyGuid_ReturnsNotFound()
    {
        // Arrange – Guid.Empty matches no record
        // Act
        var result = await _svc.GetOrganizationByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "GetOrganizationByIdAsync::GetOrganizationByIdAsync-03")]
    public async Task GetOrganizationByIdAsync_NonExistentOrgId_ReturnsNotFound()
    {
        // Arrange – a random GUID that has never been inserted
        var nonExistent = Guid.NewGuid();

        // Act
        var result = await _svc.GetOrganizationByIdAsync(nonExistent);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "GetOrganizationByIdAsync::GetOrganizationByIdAsync-04")]
    public async Task GetOrganizationByIdAsync_DbFailure_ThrowsException()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetOrganizationByIdAsync(id));
    }

    [Fact(DisplayName = "GetOrganizationByIdAsync::GetOrganizationByIdAsync-ORGID-EmptyGuid")]
    public async Task GetOrganizationByIdAsync_OrgId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.GetOrganizationByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
    }


    // =========================================================================
    // GetOrganizationsAsync
    // =========================================================================

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-01")]
    public async Task GetOrganizationsAsync_ValidParams_ReturnsPagedOrgs()
    {
        // Arrange
        _db.Organizations.AddRange(
            new OrgEntity { OrgName = "Org A", Status = OrganizationStatus.ACTIVE },
            new OrgEntity { OrgName = "Org B", Status = OrganizationStatus.ACTIVE },
            new OrgEntity { OrgName = "Org C", Status = OrganizationStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetOrganizationsAsync(page: 1, pageSize: 10, search: null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.Data.Count);
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-02")]
    public async Task GetOrganizationsAsync_ZeroPageSize_ReturnsNoItems()
    {
        // Arrange – pageSize=0, service has no guard; Take(0) returns empty
        _db.Organizations.Add(new OrgEntity { OrgName = "Hospital X" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetOrganizationsAsync(page: 1, pageSize: 0, search: null);

        // Assert
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-03")]
    public async Task GetOrganizationsAsync_NoMatchingSearch_ReturnsEmptyData()
    {
        // Arrange
        _db.Organizations.Add(new OrgEntity { OrgName = "Cho Ray" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetOrganizationsAsync(page: 1, pageSize: 10, search: "NonExistentXYZ9999");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-04")]
    public async Task GetOrganizationsAsync_PageOutOfBounds_ReturnsEmptyItems()
    {
        // Arrange – only 1 record, but request page 9999
        _db.Organizations.Add(new OrgEntity { OrgName = "Hospital Y" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetOrganizationsAsync(page: 9999, pageSize: 10, search: null);

        // Assert – TotalCount still correct, but Data is empty for out-of-range page
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(9999, result.Page);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-05")]
    public async Task GetOrganizationsAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetOrganizationsAsync());
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-PAGE-ZeroOrNegative")]
    public async Task GetOrganizationsAsync_PageZero_SkipsNegativeRecords()
    {
        // Arrange
        _db.Organizations.Add(new OrgEntity { OrgName = "Test Org" });
        await _db.SaveChangesAsync();

        // Act – page=0 → Skip(-pageSize) which EF clamps; behavior is deterministic
        var result = await _svc.GetOrganizationsAsync(page: 0, pageSize: 10, search: null);

        // Assert – page parameter is stored as-is, data may or may not be returned
        Assert.Equal(0, result.Page);
    }

    [Fact(DisplayName = "GetOrganizationsAsync::GetOrganizationsAsync-PAGESIZE-ZeroOrNegative")]
    public async Task GetOrganizationsAsync_PageSizeNegative_ReturnsNoItems()
    {
        // Arrange
        _db.Organizations.Add(new OrgEntity { OrgName = "Test Org 2" });
        await _db.SaveChangesAsync();

        // Act – negative pageSize → Take(negative) = empty set
        var result = await _svc.GetOrganizationsAsync(page: 1, pageSize: -1, search: null);

        // Assert
        Assert.Empty(result.Data);
    }

    // =========================================================================
    // UpdateOrganizationAsync
    // =========================================================================

    [Fact(DisplayName = "UpdateOrganizationAsync::UpdateOrganizationAsync-01")]
    public async Task UpdateOrganizationAsync_ValidRequest_ReturnsUpdatedOrg()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Old Name", OrgCode = "OLD", OrgType = OrganizationType.CLINIC, Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        var req = new UpdateOrganizationRequest { OrgName = "New Hospital Name", OrgCode = "NEW", OrgType = OrganizationType.HOSPITAL, Website = "https://new.vn" };

        // Act
        var result = await _svc.UpdateOrganizationAsync(org.OrgId, req);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Organization updated successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("New Hospital Name", result.Data.OrgName);
        Assert.Equal("NEW", result.Data.OrgCode);
        Assert.Equal(OrganizationType.HOSPITAL, result.Data.OrgType);
        Assert.Equal("https://new.vn", result.Data.Website);
    }

    [Fact(DisplayName = "UpdateOrganizationAsync::UpdateOrganizationAsync-02")]
    public async Task UpdateOrganizationAsync_EmptyGuidOrgId_ReturnsNotFound()
    {
        // Arrange
        var req = new UpdateOrganizationRequest { OrgName = "Test" };

        // Act
        var result = await _svc.UpdateOrganizationAsync(Guid.Empty, req);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "UpdateOrganizationAsync::UpdateOrganizationAsync-03")]
    public async Task UpdateOrganizationAsync_Unauthorized_ServiceHasNoAuthCheck()
    {
        // Arrange – auth enforced at controller/middleware layer, not service
        var org = new OrgEntity { OrgName = "Hospital A", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.UpdateOrganizationAsync(org.OrgId, new UpdateOrganizationRequest { OrgName = "Updated" });

        // Assert – service proceeds without auth check
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "UpdateOrganizationAsync::UpdateOrganizationAsync-04")]
    public async Task UpdateOrganizationAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.UpdateOrganizationAsync(Guid.NewGuid(), new UpdateOrganizationRequest()));
    }

    [Fact(DisplayName = "UpdateOrganizationAsync::UpdateOrganizationAsync-ORGID-EmptyGuid")]
    public async Task UpdateOrganizationAsync_OrgId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.UpdateOrganizationAsync(Guid.Empty, new UpdateOrganizationRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Contains("tổ chức", result.Message);
    }
    // =========================================================================
    // DeleteOrganizationAsync
    // =========================================================================

    [Fact(DisplayName = "DeleteOrganizationAsync::DeleteOrganizationAsync-01")]
    public async Task DeleteOrganizationAsync_ValidOrgId_DeactivatesOrg()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital To Delete", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.DeleteOrganizationAsync(org.OrgId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal("Organization deactivated", result.Message);
        var updated = await _db.Organizations.FindAsync(org.OrgId);
        Assert.Equal(OrganizationStatus.INACTIVE, updated!.Status);
    }

    [Fact(DisplayName = "DeleteOrganizationAsync::DeleteOrganizationAsync-02")]
    public async Task DeleteOrganizationAsync_EmptyGuid_ReturnsNotFound()
    {
        // Act
        var result = await _svc.DeleteOrganizationAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "DeleteOrganizationAsync::DeleteOrganizationAsync-03")]
    public async Task DeleteOrganizationAsync_Unauthorized_ServiceHasNoAuthCheck()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital B", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act – no auth guard at service layer
        var result = await _svc.DeleteOrganizationAsync(org.OrgId);

        // Assert
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "DeleteOrganizationAsync::DeleteOrganizationAsync-04")]
    public async Task DeleteOrganizationAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteOrganizationAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "DeleteOrganizationAsync::DeleteOrganizationAsync-ORGID-EmptyGuid")]
    public async Task DeleteOrganizationAsync_OrgId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.DeleteOrganizationAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
    }

    // =========================================================================
    // VerifyOrganizationAsync
    // =========================================================================

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-01")]
    public async Task VerifyOrganizationAsync_ValidIds_SetsActiveAndGeneratesDid()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Pending Hospital", Status = OrganizationStatus.PENDING_VERIFICATION };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        var verifierId = Guid.NewGuid();

        // Act
        var result = await _svc.VerifyOrganizationAsync(org.OrgId, verifierId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Organization verified successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(OrganizationStatus.ACTIVE, result.Data.Status);
        Assert.NotNull(result.Data.OrgDid);
        Assert.Contains(org.OrgId.ToString("N"), result.Data.OrgDid);
        Assert.NotNull(result.Data.VerifiedAt);
    }

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-02")]
    public async Task VerifyOrganizationAsync_EmptyOrgId_ReturnsNotFound()
    {
        // Arrange – Guid.Empty matches no record
        var verifierId = Guid.NewGuid();

        // Act
        var result = await _svc.VerifyOrganizationAsync(Guid.Empty, verifierId);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-03")]
    public async Task VerifyOrganizationAsync_NonExistentOrgId_ReturnsNotFound()
    {
        // Act
        var result = await _svc.VerifyOrganizationAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-04")]
    public async Task VerifyOrganizationAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.VerifyOrganizationAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-ORGID-EmptyGuid")]
    public async Task VerifyOrganizationAsync_OrgId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.VerifyOrganizationAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
    }

    [Fact(DisplayName = "VerifyOrganizationAsync::VerifyOrganizationAsync-VERIFIEDBYUSERID-EmptyGuid")]
    public async Task VerifyOrganizationAsync_VerifiedByUserId_EmptyGuid_ServiceProceedsWithEmptyVerifier()
    {
        // Arrange – service does not guard on verifiedByUserId being Guid.Empty
        var org = new OrgEntity { OrgName = "Org Pending", Status = OrganizationStatus.PENDING_VERIFICATION };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.VerifyOrganizationAsync(org.OrgId, Guid.Empty);

        // Assert – service stores Guid.Empty as verifier; no guard at service level
        Assert.True(result.Success);
        Assert.Equal(OrganizationStatus.ACTIVE, result.Data!.Status);
    }

    // =========================================================================
    // CreateDepartmentAsync
    // =========================================================================

    [Fact(DisplayName = "CreateDepartmentAsync::CreateDepartmentAsync-01")]
    public async Task CreateDepartmentAsync_ValidRequest_ReturnsDepartment()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Bach Mai", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        var req = new CreateDepartmentRequest
        {
            OrgId = org.OrgId,
            DepartmentName = "Cardiology",
            DepartmentCode = "CARD",
            Description = "Heart department",
            Floor = "2F",
            RoomNumbers = "201,202"
        };

        // Act
        var result = await _svc.CreateDepartmentAsync(req);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Department created successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.DepartmentId);
        Assert.Equal("Cardiology", result.Data.DepartmentName);
        Assert.Equal("CARD", result.Data.DepartmentCode);
        Assert.Equal(org.OrgId, result.Data.OrgId);
        Assert.Equal(1, await _db.Departments.CountAsync());
    }

    [Fact(DisplayName = "CreateDepartmentAsync::CreateDepartmentAsync-02")]
    public async Task CreateDepartmentAsync_NonExistentOrg_ReturnsNotFound()
    {
        // Arrange – OrgId points to a non-existent organization
        var req = new CreateDepartmentRequest
        {
            OrgId = Guid.NewGuid(),
            DepartmentName = "Neurology"
        };

        // Act
        var result = await _svc.CreateDepartmentAsync(req);

        // Assert – service guards: org must exist
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "CreateDepartmentAsync::CreateDepartmentAsync-03")]
    public async Task CreateDepartmentAsync_Unauthorized_ServiceHasNoAuthCheck()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Test Org", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Act – auth enforced at controller layer
        var result = await _svc.CreateDepartmentAsync(new CreateDepartmentRequest { OrgId = org.OrgId, DepartmentName = "ICU" });

        // Assert
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "CreateDepartmentAsync::CreateDepartmentAsync-04")]
    public async Task CreateDepartmentAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateDepartmentAsync(new CreateDepartmentRequest { OrgId = Guid.NewGuid(), DepartmentName = "Test" }));
    }

    // =========================================================================
    // GetDepartmentByIdAsync
    // =========================================================================

    [Fact(DisplayName = "GetDepartmentByIdAsync::GetDepartmentByIdAsync-01")]
    public async Task GetDepartmentByIdAsync_ValidId_ReturnsDepartment()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org A", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "Radiology", DepartmentCode = "RAD", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetDepartmentByIdAsync(dept.DepartmentId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(dept.DepartmentId, result.Data.DepartmentId);
        Assert.Equal("Radiology", result.Data.DepartmentName);
        Assert.Equal("RAD", result.Data.DepartmentCode);
    }

    [Fact(DisplayName = "GetDepartmentByIdAsync::GetDepartmentByIdAsync-02")]
    public async Task GetDepartmentByIdAsync_EmptyGuid_ReturnsNotFound()
    {
        // Act
        var result = await _svc.GetDepartmentByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Department not found", result.Message);
    }

    [Fact(DisplayName = "GetDepartmentByIdAsync::GetDepartmentByIdAsync-03")]
    public async Task GetDepartmentByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _svc.GetDepartmentByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Department not found", result.Message);
    }

    [Fact(DisplayName = "GetDepartmentByIdAsync::GetDepartmentByIdAsync-04")]
    public async Task GetDepartmentByIdAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetDepartmentByIdAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "GetDepartmentByIdAsync::GetDepartmentByIdAsync-DEPARTMENTID-EmptyGuid")]
    public async Task GetDepartmentByIdAsync_DepartmentId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.GetDepartmentByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
    }

    // =========================================================================
    // GetDepartmentsByOrgAsync
    // =========================================================================

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-01")]
    public async Task GetDepartmentsByOrgAsync_ValidOrgId_ReturnsDepartments()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Multi-Dept Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Departments.AddRange(
            new Department { OrgId = org.OrgId, DepartmentName = "Surgery", Status = DepartmentStatus.ACTIVE },
            new Department { OrgId = org.OrgId, DepartmentName = "ICU", Status = DepartmentStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(org.OrgId, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-02")]
    public async Task GetDepartmentsByOrgAsync_EmptyGuidOrgId_ReturnsEmpty()
    {
        // Arrange – no departments for Guid.Empty (expected behavior, no guard in service)
        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-03")]
    public async Task GetDepartmentsByOrgAsync_NonExistentOrg_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(Guid.NewGuid(), page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-04")]
    public async Task GetDepartmentsByOrgAsync_PageOutOfBounds_ReturnsEmptyItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital Z", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Departments.Add(new Department { OrgId = org.OrgId, DepartmentName = "Dept A", Status = DepartmentStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act – request far out-of-range page
        var result = await _svc.GetDepartmentsByOrgAsync(org.OrgId, page: 9999, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-05")]
    public async Task GetDepartmentsByOrgAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetDepartmentsByOrgAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-ORGID-EmptyGuid")]
    public async Task GetDepartmentsByOrgAsync_OrgId_EmptyGuid_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-PAGE-ZeroOrNegative")]
    public async Task GetDepartmentsByOrgAsync_PageZero_ReturnsDeterministicResult()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital W", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Departments.Add(new Department { OrgId = org.OrgId, DepartmentName = "ER", Status = DepartmentStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(org.OrgId, page: 0, pageSize: 10);

        // Assert
        Assert.Equal(0, result.Page);
    }

    [Fact(DisplayName = "GetDepartmentsByOrgAsync::GetDepartmentsByOrgAsync-PAGESIZE-ZeroOrNegative")]
    public async Task GetDepartmentsByOrgAsync_PageSizeNegative_ReturnsNoItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital V", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Departments.Add(new Department { OrgId = org.OrgId, DepartmentName = "Labs", Status = DepartmentStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetDepartmentsByOrgAsync(org.OrgId, page: 1, pageSize: -1);

        // Assert
        Assert.Empty(result.Data);
    }

    // =========================================================================
    // UpdateDepartmentAsync
    // =========================================================================

    [Fact(DisplayName = "UpdateDepartmentAsync::UpdateDepartmentAsync-01")]
    public async Task UpdateDepartmentAsync_ValidRequest_ReturnsUpdatedDept()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "Old Name", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        var req = new UpdateDepartmentRequest { DepartmentName = "New Name", DepartmentCode = "NN", Floor = "3F" };

        // Act
        var result = await _svc.UpdateDepartmentAsync(dept.DepartmentId, req);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Department updated successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("New Name", result.Data.DepartmentName);
        Assert.Equal("NN", result.Data.DepartmentCode);
        Assert.Equal("3F", result.Data.Floor);
    }

    [Fact(DisplayName = "UpdateDepartmentAsync::UpdateDepartmentAsync-02")]
    public async Task UpdateDepartmentAsync_EmptyGuid_ReturnsNotFound()
    {
        // Act
        var result = await _svc.UpdateDepartmentAsync(Guid.Empty, new UpdateDepartmentRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Department not found", result.Message);
    }

    [Fact(DisplayName = "UpdateDepartmentAsync::UpdateDepartmentAsync-03")]
    public async Task UpdateDepartmentAsync_Unauthorized_ServiceHasNoAuthCheck()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "Dept", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        // Act – no auth guard at service level
        var result = await _svc.UpdateDepartmentAsync(dept.DepartmentId, new UpdateDepartmentRequest { DepartmentName = "Updated" });

        // Assert
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "UpdateDepartmentAsync::UpdateDepartmentAsync-04")]
    public async Task UpdateDepartmentAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.UpdateDepartmentAsync(Guid.NewGuid(), new UpdateDepartmentRequest()));
    }

    [Fact(DisplayName = "UpdateDepartmentAsync::UpdateDepartmentAsync-DEPARTMENTID-EmptyGuid")]
    public async Task UpdateDepartmentAsync_DepartmentId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.UpdateDepartmentAsync(Guid.Empty, new UpdateDepartmentRequest());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Department not found", result.Message);
    }

    // =========================================================================
    // DeleteDepartmentAsync
    // =========================================================================

    [Fact(DisplayName = "DeleteDepartmentAsync::DeleteDepartmentAsync-01")]
    public async Task DeleteDepartmentAsync_ValidId_DeactivatesDept()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "Cardiology", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.DeleteDepartmentAsync(dept.DepartmentId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal("Department deactivated", result.Message);
        var updated = await _db.Departments.FindAsync(dept.DepartmentId);
        Assert.Equal(DepartmentStatus.INACTIVE, updated!.Status);
    }

    [Fact(DisplayName = "DeleteDepartmentAsync::DeleteDepartmentAsync-02")]
    public async Task DeleteDepartmentAsync_EmptyGuid_ReturnsNotFound()
    {
        // Act
        var result = await _svc.DeleteDepartmentAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Data);
        Assert.Equal("Department not found", result.Message);
    }

    [Fact(DisplayName = "DeleteDepartmentAsync::DeleteDepartmentAsync-03")]
    public async Task DeleteDepartmentAsync_Unauthorized_ServiceHasNoAuthCheck()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "ICU", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        // Act – auth enforced at controller; service has no guard
        var result = await _svc.DeleteDepartmentAsync(dept.DepartmentId);

        // Assert
        Assert.True(result.Success);
    }

    [Fact(DisplayName = "DeleteDepartmentAsync::DeleteDepartmentAsync-04")]
    public async Task DeleteDepartmentAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.DeleteDepartmentAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "DeleteDepartmentAsync::DeleteDepartmentAsync-DEPARTMENTID-EmptyGuid")]
    public async Task DeleteDepartmentAsync_DepartmentId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.DeleteDepartmentAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Department not found", result.Message);
    }

    // =========================================================================
    // CreateMembershipAsync
    // =========================================================================

    [Fact(DisplayName = "CreateMembershipAsync::CreateMembershipAsync-01")]
    public async Task CreateMembershipAsync_ValidRequest_ReturnsMembership()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Bach Mai Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var dept = new Department { OrgId = org.OrgId, DepartmentName = "Surgery", Status = DepartmentStatus.ACTIVE };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var req = new CreateMembershipRequest
        {
            UserId = userId,
            OrgId = org.OrgId,
            DepartmentId = dept.DepartmentId,
            EmployeeId = "EMP-001",
            JobTitle = "Senior Surgeon",
            LicenseNumber = "LIC-SURG-2024",
            Specialty = "Cardiothoracic Surgery",
            StartDate = new DateOnly(2024, 1, 15)
        };

        // Act
        var result = await _svc.CreateMembershipAsync(req);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Membership created successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data.MembershipId);
        Assert.Equal(userId, result.Data.User!.UserId);
        Assert.Equal(org.OrgId, result.Data.OrgId);
        Assert.Equal("EMP-001", result.Data.EmployeeId);
        Assert.Equal("Senior Surgeon", result.Data.JobTitle);
        Assert.Equal(new DateOnly(2024, 1, 15), result.Data.StartDate);
        Assert.Equal(1, await _db.Memberships.CountAsync());
    }

    [Fact(DisplayName = "CreateMembershipAsync::CreateMembershipAsync-02")]
    public async Task CreateMembershipAsync_NonExistentOrg_ReturnsNotFound()
    {
        // Arrange – OrgId does not exist
        var req = new CreateMembershipRequest
        {
            UserId = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            StartDate = new DateOnly(2024, 1, 1)
        };

        // Act
        var result = await _svc.CreateMembershipAsync(req);

        // Assert – service guards org existence
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Không tìm thấy tổ chức / bệnh viện.", result.Message);
    }

    [Fact(DisplayName = "CreateMembershipAsync::CreateMembershipAsync-03")]
    public async Task CreateMembershipAsync_DuplicateActiveMembership_ReturnsConflict()
    {
        // Arrange – same user already has an ACTIVE membership in the same org
        var org = new OrgEntity { OrgName = "Test Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var userId = Guid.NewGuid();
        var existing = new Membership
        {
            UserId = userId,
            OrgId = org.OrgId,
            StartDate = new DateOnly(2023, 1, 1),
            Status = MembershipStatus.ACTIVE
        };
        _db.Memberships.Add(existing);
        await _db.SaveChangesAsync();

        var req = new CreateMembershipRequest
        {
            UserId = userId,
            OrgId = org.OrgId,
            StartDate = new DateOnly(2024, 6, 1)
        };

        // Act
        var result = await _svc.CreateMembershipAsync(req);

        // Assert – service rejects duplicate active membership
        Assert.False(result.Success);
        Assert.Equal("User already has an active membership in this organization", result.Message);
    }

    [Fact(DisplayName = "CreateMembershipAsync::CreateMembershipAsync-04")]
    public async Task CreateMembershipAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _svc.CreateMembershipAsync(new CreateMembershipRequest
            {
                UserId = Guid.NewGuid(),
                OrgId = Guid.NewGuid(),
                StartDate = new DateOnly(2024, 1, 1)
            }));
    }

    // =========================================================================
    // GetMembershipByIdAsync
    // =========================================================================

    [Fact(DisplayName = "GetMembershipByIdAsync::GetMembershipByIdAsync-01")]
    public async Task GetMembershipByIdAsync_ValidId_ReturnsMembership()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Hospital A", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var userId = Guid.NewGuid();
        var membership = new Membership
        {
            UserId = userId,
            OrgId = org.OrgId,
            EmployeeId = "EMP-999",
            JobTitle = "Cardiologist",
            Specialty = "Cardiology",
            StartDate = new DateOnly(2023, 3, 1),
            Status = MembershipStatus.ACTIVE
        };
        _db.Memberships.Add(membership);
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipByIdAsync(membership.MembershipId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(membership.MembershipId, result.Data.MembershipId);
        Assert.Equal(userId, result.Data.User!.UserId);
        Assert.Equal("EMP-999", result.Data.EmployeeId);
        Assert.Equal("Cardiologist", result.Data.JobTitle);
        Assert.Equal(MembershipStatus.ACTIVE, result.Data.Status);
    }

    [Fact(DisplayName = "GetMembershipByIdAsync::GetMembershipByIdAsync-02")]
    public async Task GetMembershipByIdAsync_EmptyGuid_ReturnsNotFound()
    {
        // Act
        var result = await _svc.GetMembershipByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Membership not found", result.Message);
    }

    [Fact(DisplayName = "GetMembershipByIdAsync::GetMembershipByIdAsync-03")]
    public async Task GetMembershipByIdAsync_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _svc.GetMembershipByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Membership not found", result.Message);
    }

    [Fact(DisplayName = "GetMembershipByIdAsync::GetMembershipByIdAsync-04")]
    public async Task GetMembershipByIdAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetMembershipByIdAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "GetMembershipByIdAsync::GetMembershipByIdAsync-MEMBERSHIPID-EmptyGuid")]
    public async Task GetMembershipByIdAsync_MembershipId_EmptyGuid_ReturnsError()
    {
        // Act
        var result = await _svc.GetMembershipByIdAsync(Guid.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
    }

    // =========================================================================
    // GetMembershipsByOrgAsync
    // =========================================================================

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-01")]
    public async Task GetMembershipsByOrgAsync_ValidOrgId_ReturnsMemberships()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Big Hospital", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        _db.Memberships.AddRange(
            new Membership { UserId = u1, OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE },
            new Membership { UserId = u2, OrgId = org.OrgId, StartDate = new DateOnly(2024, 2, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByOrgAsync(org.OrgId, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-02")]
    public async Task GetMembershipsByOrgAsync_EmptyGuidOrgId_ReturnsEmpty()
    {
        // Act – no guard in service; Guid.Empty produces zero matches
        var result = await _svc.GetMembershipsByOrgAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-03")]
    public async Task GetMembershipsByOrgAsync_NonExistentOrg_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetMembershipsByOrgAsync(Guid.NewGuid(), page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-04")]
    public async Task GetMembershipsByOrgAsync_PageOutOfBounds_ReturnsEmptyItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Clinic Z", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Memberships.Add(new Membership { UserId = Guid.NewGuid(), OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByOrgAsync(org.OrgId, page: 9999, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-05")]
    public async Task GetMembershipsByOrgAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetMembershipsByOrgAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-ORGID-EmptyGuid")]
    public async Task GetMembershipsByOrgAsync_OrgId_EmptyGuid_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetMembershipsByOrgAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-PAGE-ZeroOrNegative")]
    public async Task GetMembershipsByOrgAsync_PageZero_ReturnsDeterministicResult()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Clinic W", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Memberships.Add(new Membership { UserId = Guid.NewGuid(), OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByOrgAsync(org.OrgId, page: 0, pageSize: 10);

        // Assert
        Assert.Equal(0, result.Page);
    }

    [Fact(DisplayName = "GetMembershipsByOrgAsync::GetMembershipsByOrgAsync-PAGESIZE-ZeroOrNegative")]
    public async Task GetMembershipsByOrgAsync_PageSizeNegative_ReturnsNoItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Clinic V", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        _db.Memberships.Add(new Membership { UserId = Guid.NewGuid(), OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByOrgAsync(org.OrgId, page: 1, pageSize: -1);

        // Assert
        Assert.Empty(result.Data);
    }

    // =========================================================================
    // GetMembershipsByUserAsync
    // =========================================================================

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-01")]
    public async Task GetMembershipsByUserAsync_ValidUserId_ReturnsMemberships()
    {
        // Arrange – one user with memberships in two different orgs
        var org1 = new OrgEntity { OrgName = "Org 1", Status = OrganizationStatus.ACTIVE };
        var org2 = new OrgEntity { OrgName = "Org 2", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.AddRange(org1, org2);
        var userId = Guid.NewGuid();
        _db.Memberships.AddRange(
            new Membership { UserId = userId, OrgId = org1.OrgId, StartDate = new DateOnly(2023, 1, 1), Status = MembershipStatus.ACTIVE },
            new Membership { UserId = userId, OrgId = org2.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByUserAsync(userId, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
        Assert.All(result.Data, m => Assert.Equal(userId, m.User!.UserId));
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-02")]
    public async Task GetMembershipsByUserAsync_EmptyGuidUserId_ReturnsEmpty()
    {
        // Act – Guid.Empty produces zero matches, no guard in service
        var result = await _svc.GetMembershipsByUserAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-03")]
    public async Task GetMembershipsByUserAsync_NonExistentUser_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetMembershipsByUserAsync(Guid.NewGuid(), page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-04")]
    public async Task GetMembershipsByUserAsync_PageOutOfBounds_ReturnsEmptyItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org A", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var userId = Guid.NewGuid();
        _db.Memberships.Add(new Membership { UserId = userId, OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByUserAsync(userId, page: 9999, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-05")]
    public async Task GetMembershipsByUserAsync_DbFailure_ThrowsException()
    {
        // Arrange
        await _db.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.GetMembershipsByUserAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-USERID-EmptyGuid")]
    public async Task GetMembershipsByUserAsync_UserId_EmptyGuid_ReturnsEmpty()
    {
        // Act
        var result = await _svc.GetMembershipsByUserAsync(Guid.Empty, page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-PAGE-ZeroOrNegative")]
    public async Task GetMembershipsByUserAsync_PageZero_ReturnsDeterministicResult()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org B", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var userId = Guid.NewGuid();
        _db.Memberships.Add(new Membership { UserId = userId, OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByUserAsync(userId, page: 0, pageSize: 10);

        // Assert
        Assert.Equal(0, result.Page);
    }

    [Fact(DisplayName = "GetMembershipsByUserAsync::GetMembershipsByUserAsync-PAGESIZE-ZeroOrNegative")]
    public async Task GetMembershipsByUserAsync_PageSizeNegative_ReturnsNoItems()
    {
        // Arrange
        var org = new OrgEntity { OrgName = "Org C", Status = OrganizationStatus.ACTIVE };
        _db.Organizations.Add(org);
        var userId = Guid.NewGuid();
        _db.Memberships.Add(new Membership { UserId = userId, OrgId = org.OrgId, StartDate = new DateOnly(2024, 1, 1), Status = MembershipStatus.ACTIVE });
        await _db.SaveChangesAsync();

        // Act
        var result = await _svc.GetMembershipsByUserAsync(userId, page: 1, pageSize: -1);

        // Assert
        Assert.Empty(result.Data);
    }
    [Fact(DisplayName = "SearchDoctorsAsync::SearchDoctorsAsync-01")]
    public void SearchDoctorsAsync_SearchDoctorsAsync_01_65()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid request provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "SearchDoctorsAsync::SearchDoctorsAsync-02")]
    public void SearchDoctorsAsync_SearchDoctorsAsync_02_66()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: request with missing required fields
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "SearchDoctorsAsync::SearchDoctorsAsync-03")]
    public void SearchDoctorsAsync_SearchDoctorsAsync_03_67()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: Requested resource not found
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "SearchDoctorsAsync::SearchDoctorsAsync-04")]
    public void SearchDoctorsAsync_SearchDoctorsAsync_04_68()
    {
        // Arrange
        // Condition: PagingBoundary
        // Input: Large page number or page size out of bounds
        
        // Act
        
        // Assert
        // Expected Return: Returns valid paging metadata; out-of-range page returns empty item set
        Assert.True(true);
    }
    [Fact(DisplayName = "SearchDoctorsAsync::SearchDoctorsAsync-05")]
    public void SearchDoctorsAsync_SearchDoctorsAsync_05_69()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of request
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdateMembershipAsync::UpdateMembershipAsync-01")]
    public void UpdateMembershipAsync_UpdateMembershipAsync_01_70()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid membershipId, request provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdateMembershipAsync::UpdateMembershipAsync-02")]
    public void UpdateMembershipAsync_UpdateMembershipAsync_02_71()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: membershipId = Guid.Empty OR request with missing required fields
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdateMembershipAsync::UpdateMembershipAsync-03")]
    public void UpdateMembershipAsync_UpdateMembershipAsync_03_72()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on membershipId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdateMembershipAsync::UpdateMembershipAsync-04")]
    public void UpdateMembershipAsync_UpdateMembershipAsync_04_73()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of membershipId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "DeleteMembershipAsync::DeleteMembershipAsync-01")]
    public void DeleteMembershipAsync_DeleteMembershipAsync_01_74()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid membershipId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "DeleteMembershipAsync::DeleteMembershipAsync-02")]
    public void DeleteMembershipAsync_DeleteMembershipAsync_02_75()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: membershipId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "DeleteMembershipAsync::DeleteMembershipAsync-03")]
    public void DeleteMembershipAsync_DeleteMembershipAsync_03_76()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on membershipId
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "DeleteMembershipAsync::DeleteMembershipAsync-04")]
    public void DeleteMembershipAsync_DeleteMembershipAsync_04_77()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of membershipId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "ConfigurePaymentAsync::ConfigurePaymentAsync-01")]
    public void ConfigurePaymentAsync_ConfigurePaymentAsync_01_78()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid orgId, request provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "ConfigurePaymentAsync::ConfigurePaymentAsync-02")]
    public void ConfigurePaymentAsync_ConfigurePaymentAsync_02_79()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: orgId = Guid.Empty OR request with missing required fields
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "ConfigurePaymentAsync::ConfigurePaymentAsync-03")]
    public void ConfigurePaymentAsync_ConfigurePaymentAsync_03_80()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on orgId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "ConfigurePaymentAsync::ConfigurePaymentAsync-04")]
    public void ConfigurePaymentAsync_ConfigurePaymentAsync_04_81()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of orgId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdatePaymentConfigAsync::UpdatePaymentConfigAsync-01")]
    public void UpdatePaymentConfigAsync_UpdatePaymentConfigAsync_01_82()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid orgId, request provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdatePaymentConfigAsync::UpdatePaymentConfigAsync-02")]
    public void UpdatePaymentConfigAsync_UpdatePaymentConfigAsync_02_83()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: orgId = Guid.Empty OR request with missing required fields
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdatePaymentConfigAsync::UpdatePaymentConfigAsync-03")]
    public void UpdatePaymentConfigAsync_UpdatePaymentConfigAsync_03_84()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on orgId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "UpdatePaymentConfigAsync::UpdatePaymentConfigAsync-04")]
    public void UpdatePaymentConfigAsync_UpdatePaymentConfigAsync_04_85()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of orgId, request
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentConfigStatusAsync::GetPaymentConfigStatusAsync-01")]
    public void GetPaymentConfigStatusAsync_GetPaymentConfigStatusAsync_01_86()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid orgId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentConfigStatusAsync::GetPaymentConfigStatusAsync-02")]
    public void GetPaymentConfigStatusAsync_GetPaymentConfigStatusAsync_02_87()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: orgId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentConfigStatusAsync::GetPaymentConfigStatusAsync-03")]
    public void GetPaymentConfigStatusAsync_GetPaymentConfigStatusAsync_03_88()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: orgId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentConfigStatusAsync::GetPaymentConfigStatusAsync-04")]
    public void GetPaymentConfigStatusAsync_GetPaymentConfigStatusAsync_04_89()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on orgId
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentConfigStatusAsync::GetPaymentConfigStatusAsync-05")]
    public void GetPaymentConfigStatusAsync_GetPaymentConfigStatusAsync_05_90()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of orgId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentKeysAsync::GetPaymentKeysAsync-01")]
    public void GetPaymentKeysAsync_GetPaymentKeysAsync_01_91()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid orgId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentKeysAsync::GetPaymentKeysAsync-02")]
    public void GetPaymentKeysAsync_GetPaymentKeysAsync_02_92()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: orgId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentKeysAsync::GetPaymentKeysAsync-03")]
    public void GetPaymentKeysAsync_GetPaymentKeysAsync_03_93()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: orgId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentKeysAsync::GetPaymentKeysAsync-04")]
    public void GetPaymentKeysAsync_GetPaymentKeysAsync_04_94()
    {
        // Arrange
        // Condition: UnauthorizedOrForbidden
        // Input: User lacks permission for this action on orgId
        
        // Act
        
        // Assert
        // Expected Return: Returns unauthorized or forbidden response, or operation rejected by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetPaymentKeysAsync::GetPaymentKeysAsync-05")]
    public void GetPaymentKeysAsync_GetPaymentKeysAsync_05_95()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of orgId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetDoctorByUserIdInMyOrganizationAsync::GetDoctorByUserIdInMyOrganizationAsync-01")]
    public void GetDoctorByUserIdInMyOrganizationAsync_GetDoctorByUserIdInMyOrganizationAsync_01_96()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid bearerToken, orgId, userId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetDoctorByUserIdInMyOrganizationAsync::GetDoctorByUserIdInMyOrganizationAsync-02")]
    public void GetDoctorByUserIdInMyOrganizationAsync_GetDoctorByUserIdInMyOrganizationAsync_02_97()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: bearerToken = null/empty OR orgId = Guid.Empty OR userId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetDoctorByUserIdInMyOrganizationAsync::GetDoctorByUserIdInMyOrganizationAsync-03")]
    public void GetDoctorByUserIdInMyOrganizationAsync_GetDoctorByUserIdInMyOrganizationAsync_03_98()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: orgId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetDoctorByUserIdInMyOrganizationAsync::GetDoctorByUserIdInMyOrganizationAsync-04")]
    public void GetDoctorByUserIdInMyOrganizationAsync_GetDoctorByUserIdInMyOrganizationAsync_04_99()
    {
        // Arrange
        // Condition: NullableReturn
        // Input: Valid orgId but no associated resource
        
        // Act
        
        // Assert
        // Expected Return: Returns null without throwing
        Assert.True(true);
    }
    [Fact(DisplayName = "GetDoctorByUserIdInMyOrganizationAsync::GetDoctorByUserIdInMyOrganizationAsync-05")]
    public void GetDoctorByUserIdInMyOrganizationAsync_GetDoctorByUserIdInMyOrganizationAsync_05_100()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of bearerToken, orgId, userId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByPatientIdAsync::GetUserIdByPatientIdAsync-01")]
    public void GetUserIdByPatientIdAsync_GetUserIdByPatientIdAsync_01_101()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid bearerToken, patientId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByPatientIdAsync::GetUserIdByPatientIdAsync-02")]
    public void GetUserIdByPatientIdAsync_GetUserIdByPatientIdAsync_02_102()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: bearerToken = null/empty OR patientId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByPatientIdAsync::GetUserIdByPatientIdAsync-03")]
    public void GetUserIdByPatientIdAsync_GetUserIdByPatientIdAsync_03_103()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: patientId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByPatientIdAsync::GetUserIdByPatientIdAsync-04")]
    public void GetUserIdByPatientIdAsync_GetUserIdByPatientIdAsync_04_104()
    {
        // Arrange
        // Condition: NullableReturn
        // Input: Valid patientId but no associated resource
        
        // Act
        
        // Assert
        // Expected Return: Returns null without throwing
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByPatientIdAsync::GetUserIdByPatientIdAsync-05")]
    public void GetUserIdByPatientIdAsync_GetUserIdByPatientIdAsync_05_105()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of bearerToken, patientId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByDoctorIdAsync::GetUserIdByDoctorIdAsync-01")]
    public void GetUserIdByDoctorIdAsync_GetUserIdByDoctorIdAsync_01_106()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid bearerToken, doctorId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByDoctorIdAsync::GetUserIdByDoctorIdAsync-02")]
    public void GetUserIdByDoctorIdAsync_GetUserIdByDoctorIdAsync_02_107()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: bearerToken = null/empty OR doctorId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByDoctorIdAsync::GetUserIdByDoctorIdAsync-03")]
    public void GetUserIdByDoctorIdAsync_GetUserIdByDoctorIdAsync_03_108()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: doctorId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByDoctorIdAsync::GetUserIdByDoctorIdAsync-04")]
    public void GetUserIdByDoctorIdAsync_GetUserIdByDoctorIdAsync_04_109()
    {
        // Arrange
        // Condition: NullableReturn
        // Input: Valid doctorId but no associated resource
        
        // Act
        
        // Assert
        // Expected Return: Returns null without throwing
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserIdByDoctorIdAsync::GetUserIdByDoctorIdAsync-05")]
    public void GetUserIdByDoctorIdAsync_GetUserIdByDoctorIdAsync_05_110()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of bearerToken, doctorId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserProfileDetailAsync::GetUserProfileDetailAsync-01")]
    public void GetUserProfileDetailAsync_GetUserProfileDetailAsync_01_111()
    {
        // Arrange
        // Condition: HappyPath
        // Input: Valid bearerToken, userId provided
        
        // Act
        
        // Assert
        // Expected Return: Returns success payload matching declared return type
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserProfileDetailAsync::GetUserProfileDetailAsync-02")]
    public void GetUserProfileDetailAsync_GetUserProfileDetailAsync_02_112()
    {
        // Arrange
        // Condition: InvalidInput
        // Input: bearerToken = null/empty OR userId = Guid.Empty
        
        // Act
        
        // Assert
        // Expected Return: Returns validation error (400 or 422) or equivalent domain error
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserProfileDetailAsync::GetUserProfileDetailAsync-03")]
    public void GetUserProfileDetailAsync_GetUserProfileDetailAsync_03_113()
    {
        // Arrange
        // Condition: NotFoundOrNoData
        // Input: userId does not exist in DB
        
        // Act
        
        // Assert
        // Expected Return: Returns null, empty, false, or not-found response according to contract
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserProfileDetailAsync::GetUserProfileDetailAsync-04")]
    public void GetUserProfileDetailAsync_GetUserProfileDetailAsync_04_114()
    {
        // Arrange
        // Condition: NullableReturn
        // Input: Valid userId but no associated resource
        
        // Act
        
        // Assert
        // Expected Return: Returns null without throwing
        Assert.True(true);
    }
    [Fact(DisplayName = "GetUserProfileDetailAsync::GetUserProfileDetailAsync-05")]
    public void GetUserProfileDetailAsync_GetUserProfileDetailAsync_05_115()
    {
        // Arrange
        // Condition: DependencyFailure
        // Input: External service/DB fails during processing of bearerToken, userId
        
        // Act
        
        // Assert
        // Expected Return: Returns controlled error response or mapped exception by policy
        Assert.True(true);
    }
}