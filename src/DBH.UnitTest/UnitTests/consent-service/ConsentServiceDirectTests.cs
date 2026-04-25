using Xunit;
using Xunit.Abstractions;
using Moq;
using Moq.Protected;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using DBH.Consent.Service.Services;
using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Notification;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace DBH.UnitTest.UnitTests;

public class ConsentServiceDirectTests
{
    private readonly DbContextOptions<ConsentDbContext> _dbContextOptions;
    private readonly Mock<ILogger<ConsentService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IBlockchainSyncService> _blockchainSyncServiceMock = new();
    private readonly Mock<IConsentBlockchainService> _consentBlockchainServiceMock = new();
    private readonly Mock<IEhrBlockchainService> _ehrBlockchainServiceMock = new();
    private readonly Mock<INotificationServiceClient> _notificationClientMock = new();
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ConsentServiceDirectTests(ITestOutputHelper output)
    {
        _output = output;
        _dbContextOptions = new DbContextOptionsBuilder<ConsentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer test-token";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        // Mock default successful HTTP response for any external service calls (Auth, EHR)
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{ \"success\": true, \"data\": { \"encryptedPrivateKey\": \"fake-key\", \"publicKey\": \"fake-key\", \"id\": \"00000000-0000-0000-0000-000000000000\" } }")
            });

        var client = new HttpClient(mockHttpMessageHandler.Object) { BaseAddress = new Uri("http://localhost/") };
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    private ConsentService CreateService(ConsentDbContext context) =>
        new ConsentService(context, _loggerMock.Object, _httpClientFactoryMock.Object, _httpContextAccessorMock.Object,
            _blockchainSyncServiceMock.Object, _consentBlockchainServiceMock.Object, _ehrBlockchainServiceMock.Object, _notificationClientMock.Object);

    private async Task<T> RunAndLog<T>(
        Func<Task<T>> action,
        [CallerMemberName] string testName = "")
    {
        var res = await action();
        _output.WriteLine($"{testName} response:");
        _output.WriteLine(JsonSerializer.Serialize(res, LogJsonOptions));
        return res;
    }

    [Fact] public async Task GrantConsentAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var req = new GrantConsentRequest { PatientId = Guid.NewGuid(), GranteeId = Guid.NewGuid(), Permission = ConsentPermission.READ };
        var res = await CreateService(ctx).GrantConsentAsync(req);
        _output.WriteLine("======================================");
        _output.WriteLine("RETURN DATA: " + JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("======================================");
        Assert.True(res.Success);
    }
    [Fact] public async Task GrantConsentAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GrantConsentAsync(new GrantConsentRequest { PatientId = Guid.Empty });
        Assert.NotNull(res);
    }
    [Fact] public async Task GrantConsentAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { PatientId = id, GranteeId = id, Status = ConsentStatus.ACTIVE });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GrantConsentAsync(new GrantConsentRequest { PatientId = id, GranteeId = id });
        _output.WriteLine("======================================");
        _output.WriteLine("ERROR RESPONSE: " + JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("======================================");
        Assert.False(res.Success);
    }
    [Fact] public async Task GrantConsentAsync_04() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Throws(new Exception("Network"));
        await Assert.ThrowsAnyAsync<Exception>(() => CreateService(ctx).GrantConsentAsync(new GrantConsentRequest()));
    }

    [Fact] public async Task GetConsentByIdAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var consent = new DBH.Consent.Service.Models.Entities.Consent { ConsentId = Guid.NewGuid(), Status = ConsentStatus.ACTIVE };
        ctx.Consents.Add(consent); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetConsentByIdAsync(consent.ConsentId);
        Assert.True(res.Success);
    }
    [Fact] public async Task GetConsentByIdAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetConsentByIdAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact] public async Task GetConsentByIdAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetConsentByIdAsync(Guid.NewGuid());
        Assert.False(res.Success);
    }
    [Fact] public async Task GetConsentByIdAsync_04() {
        var ctx = new ConsentDbContext(_dbContextOptions);
        ctx.Dispose(); // Simulate a DB failure by using a disposed context
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetConsentByIdAsync(Guid.NewGuid()));
    }
    [Fact(DisplayName = "GetConsentByIdAsync::GetConsentByIdAsync-CONSENTID-EmptyGuid")]
    public async Task GetConsentByIdAsync_CONSENTID_EmptyGuid() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var consentId = Guid.Empty;
        var res = await CreateService(ctx).GetConsentByIdAsync(consentId);
        Assert.False(res.Success);
        Assert.Equal("Consent not found", res.Message);
    }

    [Fact] public async Task GetConsentsByPatientAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { PatientId = id, Status = ConsentStatus.ACTIVE });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetConsentsByPatientAsync(id);
        Assert.NotEmpty(res.Data);
    }
    [Fact] public async Task GetConsentsByPatientAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetConsentsByPatientAsync(Guid.Empty);
        Assert.Empty(res.Data);
    }
    [Fact] public async Task GetConsentsByPatientAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetConsentsByPatientAsync(Guid.NewGuid());
        Assert.Empty(res.Data);
    }
    [Fact(DisplayName = "GetConsentsByPatientAsync::GetConsentsByPatientAsync-04")]
    public async Task GetConsentsByPatientAsync_04() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { PatientId = patientId, Status = ConsentStatus.ACTIVE });
        await ctx.SaveChangesAsync();
        var page = 999;
        var pageSize = 10;
        var res = await CreateService(ctx).GetConsentsByPatientAsync(patientId, page, pageSize);
        Assert.Empty(res.Data);
        Assert.Equal(1, res.TotalCount);
        Assert.Equal(999, res.Page);
        Assert.Equal(10, res.PageSize);
    }
    [Fact(DisplayName = "GetConsentsByPatientAsync::GetConsentsByPatientAsync-PATIENTID-EmptyGuid")]
    public async Task GetConsentsByPatientAsync_PATIENTID_EmptyGuid() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.Empty;
        var res = await CreateService(ctx).GetConsentsByPatientAsync(patientId);
        Assert.Empty(res.Data);
        Assert.Equal(0, res.TotalCount);
    }
    [Fact(DisplayName = "GetConsentsByPatientAsync::GetConsentsByPatientAsync-PAGE-ZeroOrNegative")]
    public async Task GetConsentsByPatientAsync_PAGE_ZeroOrNegative() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.NewGuid();
        var page = 0;
        var pageSize = 10;
        var res = await CreateService(ctx).GetConsentsByPatientAsync(patientId, page, pageSize);
        Assert.Empty(res.Data);
    }


    [Fact] public async Task GetConsentsByGranteeAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { GranteeId = id, Status = ConsentStatus.ACTIVE });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetConsentsByGranteeAsync(id);
        Assert.NotEmpty(res.Data);
    }
    [Fact] public async Task GetConsentsByGranteeAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetConsentsByGranteeAsync(Guid.Empty);
        Assert.Empty(res.Data);
    }

    [Fact] public async Task SearchConsentsAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { PatientId = id, Status = ConsentStatus.ACTIVE });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).SearchConsentsAsync(new ConsentQueryParams { PatientId = id });
        Assert.NotEmpty(res.Data);
    }
    [Fact] public async Task SearchConsentsAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).SearchConsentsAsync(new ConsentQueryParams { PatientId = Guid.NewGuid() });
        Assert.Empty(res.Data);
    }

    [Fact] public async Task RevokeConsentAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var consent = new DBH.Consent.Service.Models.Entities.Consent { ConsentId = Guid.NewGuid(), Status = ConsentStatus.ACTIVE };
        ctx.Consents.Add(consent); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).RevokeConsentAsync(consent.ConsentId, new RevokeConsentRequest { RevokeReason = "Test" });
        Assert.True(res.Success);
    }
    [Fact] public async Task RevokeConsentAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RevokeConsentAsync(Guid.Empty, new RevokeConsentRequest());
        Assert.False(res.Success);
    }
    [Fact] public async Task RevokeConsentAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var consent = new DBH.Consent.Service.Models.Entities.Consent { ConsentId = Guid.NewGuid(), Status = ConsentStatus.REVOKED };
        ctx.Consents.Add(consent); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).RevokeConsentAsync(consent.ConsentId, new RevokeConsentRequest());
        Assert.False(res.Success);
    }

    [Fact] public async Task VerifyConsentAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var pId = Guid.NewGuid(); var gId = Guid.NewGuid();
        ctx.Consents.Add(new DBH.Consent.Service.Models.Entities.Consent { PatientId = pId, GranteeId = gId, Status = ConsentStatus.ACTIVE, Permission = ConsentPermission.READ });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).VerifyConsentAsync(new VerifyConsentRequest { PatientId = pId, GranteeId = gId });
        Assert.True(res.HasAccess);
    }
    [Fact] public async Task VerifyConsentAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).VerifyConsentAsync(new VerifyConsentRequest { PatientId = Guid.Empty });
        Assert.False(res.HasAccess);
    }

    [Fact] public async Task SyncFromBlockchainAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var consent = new DBH.Consent.Service.Models.Entities.Consent { BlockchainConsentId = "BC123", Status = ConsentStatus.ACTIVE };
        ctx.Consents.Add(consent); await ctx.SaveChangesAsync();
        _consentBlockchainServiceMock.Setup(x => x.GetConsentAsync("BC123")).ReturnsAsync(new ConsentRecord { Status = "REVOKED" });
        var res = await CreateService(ctx).SyncFromBlockchainAsync("BC123");
        Assert.True(res.Success);
    }
    [Fact] public async Task SyncFromBlockchainAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).SyncFromBlockchainAsync("BC123");
        Assert.False(res.Success);
    }

    [Fact] public async Task CreateAccessRequestAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var req = new CreateAccessRequestDto { PatientId = Guid.NewGuid(), RequesterId = Guid.NewGuid() };
        var res = await CreateService(ctx).CreateAccessRequestAsync(req);
        Assert.True(res.Success);
    }
    [Fact] public async Task CreateAccessRequestAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var req = new CreateAccessRequestDto { PatientId = Guid.Empty };
        var res = await CreateService(ctx).CreateAccessRequestAsync(req);
        Assert.NotNull(res);
    }

    [Fact] public async Task GetAccessRequestByIdAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var ar = new DBH.Consent.Service.Models.Entities.AccessRequest { RequestId = Guid.NewGuid() };
        ctx.AccessRequests.Add(ar); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetAccessRequestByIdAsync(ar.RequestId);
        Assert.True(res.Success);
    }
    [Fact] public async Task GetAccessRequestByIdAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetAccessRequestByIdAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact(DisplayName = "GetAccessRequestByIdAsync::GetAccessRequestByIdAsync-03")]
    public async Task GetAccessRequestByIdAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var requestId = Guid.NewGuid();
        var res = await CreateService(ctx).GetAccessRequestByIdAsync(requestId);
        Assert.False(res.Success);
        Assert.Equal("Access request not found", res.Message);
    }
    [Fact(DisplayName = "GetAccessRequestByIdAsync::GetAccessRequestByIdAsync-REQUESTID-EmptyGuid")]
    public async Task GetAccessRequestByIdAsync_REQUESTID_EmptyGuid() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var requestId = Guid.Empty;
        var res = await CreateService(ctx).GetAccessRequestByIdAsync(requestId);
        Assert.False(res.Success);
        Assert.Equal("Access request not found", res.Message);
    }

    [Fact] public async Task GetAccessRequestsByPatientAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.AccessRequests.Add(new DBH.Consent.Service.Models.Entities.AccessRequest { PatientId = id });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(id, null);
        Assert.NotEmpty(res.Data);
    }
    [Fact] public async Task GetAccessRequestsByPatientAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(Guid.Empty, null);
        Assert.Empty(res.Data);
    }
    [Fact(DisplayName = "GetAccessRequestsByPatientAsync::GetAccessRequestsByPatientAsync-03")]
    public async Task GetAccessRequestsByPatientAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.NewGuid();
        AccessRequestStatus? status = null;
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(patientId, status);
        Assert.Empty(res.Data);
        Assert.Equal(0, res.TotalCount);
    }
    [Fact(DisplayName = "GetAccessRequestsByPatientAsync::GetAccessRequestsByPatientAsync-04")]
    public async Task GetAccessRequestsByPatientAsync_04() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.NewGuid();
        ctx.AccessRequests.Add(new DBH.Consent.Service.Models.Entities.AccessRequest { PatientId = patientId });
        await ctx.SaveChangesAsync();
        AccessRequestStatus? status = null;
        var page = 999;
        var pageSize = 10;
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(patientId, status, page, pageSize);
        Assert.Empty(res.Data);
        Assert.Equal(1, res.TotalCount);
        Assert.Equal(999, res.Page);
    }
    [Fact(DisplayName = "GetAccessRequestsByPatientAsync::GetAccessRequestsByPatientAsync-PATIENTID-EmptyGuid")]
    public async Task GetAccessRequestsByPatientAsync_PATIENTID_EmptyGuid() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.Empty;
        AccessRequestStatus? status = null;
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(patientId, status);
        Assert.Empty(res.Data);
        Assert.Equal(0, res.TotalCount);
    }
    [Fact(DisplayName = "GetAccessRequestsByPatientAsync::GetAccessRequestsByPatientAsync-PAGE-ZeroOrNegative")]
    public async Task GetAccessRequestsByPatientAsync_PAGE_ZeroOrNegative() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var patientId = Guid.NewGuid();
        AccessRequestStatus? status = null;
        var page = 0;
        var pageSize = 10;
        var res = await CreateService(ctx).GetAccessRequestsByPatientAsync(patientId, status, page, pageSize);
        Assert.Empty(res.Data);
    }
   

    [Fact] public async Task GetAccessRequestsByRequesterAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var id = Guid.NewGuid();
        ctx.AccessRequests.Add(new DBH.Consent.Service.Models.Entities.AccessRequest { RequesterId = id });
        await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetAccessRequestsByRequesterAsync(id, null);
        Assert.NotEmpty(res.Data);
    }

    [Fact] public async Task RespondToAccessRequestAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var ar = new DBH.Consent.Service.Models.Entities.AccessRequest { RequestId = Guid.NewGuid(), Status = AccessRequestStatus.PENDING };
        ctx.AccessRequests.Add(ar); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).RespondToAccessRequestAsync(ar.RequestId, new RespondAccessRequestDto { Approve = false });
        Assert.True(res.Success);
    }
    [Fact] public async Task RespondToAccessRequestAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RespondToAccessRequestAsync(Guid.Empty, new RespondAccessRequestDto());
        Assert.False(res.Success);
    }

    [Fact] public async Task CancelAccessRequestAsync_01() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var ar = new DBH.Consent.Service.Models.Entities.AccessRequest { RequestId = Guid.NewGuid(), Status = AccessRequestStatus.PENDING };
        ctx.AccessRequests.Add(ar); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).CancelAccessRequestAsync(ar.RequestId);
        Assert.True(res.Success);
    }
    [Fact] public async Task CancelAccessRequestAsync_02() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CancelAccessRequestAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact(DisplayName = "CancelAccessRequestAsync::CancelAccessRequestAsync-03")]
    public async Task CancelAccessRequestAsync_03() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        // Create a non-pending request — service rejects cancel
        var ar = new DBH.Consent.Service.Models.Entities.AccessRequest { RequestId = Guid.NewGuid(), Status = AccessRequestStatus.APPROVED };
        ctx.AccessRequests.Add(ar); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).CancelAccessRequestAsync(ar.RequestId);
        Assert.False(res.Success);
        Assert.Equal("Only pending requests can be cancelled", res.Message);
    }
    [Fact(DisplayName = "CancelAccessRequestAsync::CancelAccessRequestAsync-REQUESTID-EmptyGuid")]
    public async Task CancelAccessRequestAsync_REQUESTID_EmptyGuid() {
        using var ctx = new ConsentDbContext(_dbContextOptions);
        var requestId = Guid.Empty;
        var res = await CreateService(ctx).CancelAccessRequestAsync(requestId);
        Assert.False(res.Success);
        Assert.Equal("Access request not found", res.Message);
    }
}
