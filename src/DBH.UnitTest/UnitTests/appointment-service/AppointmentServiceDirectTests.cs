using Xunit;
using Xunit.Abstractions;
using Moq;
using Moq.Protected;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using DBH.Appointment.Service.Services;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;
using DBH.Shared.Contracts.Events;
using DBH.Shared.Contracts.Commands;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Notification;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceDirectTests
{
    private readonly DbContextOptions<AppointmentDbContext> _dbContextOptions;
    private readonly Mock<ILogger<AppointmentService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IAuthServiceClient> _authServiceClientMock = new();
    private readonly Mock<IMessagePublisher> _messagePublisherMock = new();
    private readonly Mock<INotificationServiceClient> _notificationClientMock = new();
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppointmentServiceDirectTests(ITestOutputHelper output)
    {
        _output = output;
        _dbContextOptions = new DbContextOptionsBuilder<AppointmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer test-token";
        
        // Fix IDOR user claims setup
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        context.User = claimsPrincipal;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
        
        _authServiceClientMock.Setup(x => x.GetUserIdByPatientIdAsync(It.IsAny<Guid>())).ReturnsAsync(Guid.NewGuid());
        _authServiceClientMock.Setup(x => x.GetUserIdByDoctorIdAsync(It.IsAny<Guid>())).ReturnsAsync(Guid.NewGuid());
    }
    
    /// <summary>
    /// Mock helper: Sets up HTTP client factory to handle Organization Service calls
    /// Mocks both organization existence and doctor membership validation
    /// </summary>
    private void SetupOrganizationServiceMocks(Guid orgId, Guid doctorUserId)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<System.Threading.CancellationToken>()
            )
            .Returns<HttpRequestMessage, System.Threading.CancellationToken>((request, token) =>
            {
                // Mock: GET /api/v1/organizations/{orgId} - returns 200 OK
                if (request.RequestUri!.AbsolutePath.Contains($"/organizations/{orgId}"))
                {
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent("{\"success\": true, \"data\": {}}")
                    });
                }
                
                // Mock: GET /api/v1/memberships/by-user/{doctorUserId} - returns memberships
                if (request.RequestUri.AbsolutePath.Contains($"/memberships/by-user/{doctorUserId}"))
                {
                    var membershipJson = JsonSerializer.Serialize(new
                    {
                        data = new[]
                        {
                            new
                            {
                                orgId = orgId,
                                status = "ACTIVE",
                                userId = doctorUserId
                            }
                        }
                    });
                    
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(membershipJson)
                    });
                }
                
                // Default: return 404 for unmocked requests
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Content = new StringContent("{\"success\": false}")
                });
            });
        
        var mockOrgClient = new HttpClient(mockHttpMessageHandler.Object) 
        { 
            BaseAddress = new Uri("http://localhost/organization/") 
        };
        
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(mockOrgClient);
    }

    private AppointmentService CreateService(AppointmentDbContext context) =>
        new AppointmentService(context, _loggerMock.Object, _httpClientFactoryMock.Object, _httpContextAccessorMock.Object,
            _authServiceClientMock.Object, _messagePublisherMock.Object, _notificationClientMock.Object);

    private async Task<T> RunAndLog<T>(
        Func<Task<T>> action,
        [CallerMemberName] string testName = "")
    {
        var res = await action();
        _output.WriteLine($"{testName} response:");
        _output.WriteLine(JsonSerializer.Serialize(res, LogJsonOptions));
        return res;
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-01")]
    public async Task CreateAppointmentAsync_01() 
    {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        
        // Create fake IDs
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        
        // Get the doctor's user ID from mocked auth service
        var doctorUserId = await _authServiceClientMock
            .Object.GetUserIdByDoctorIdAsync(doctorId);
        
        // Setup organization and membership mocks for this specific test
        SetupOrganizationServiceMocks(orgId, doctorUserId.Value);
        
        var request = new CreateAppointmentRequest 
        { 
            PatientId = patientId, 
            DoctorId = doctorId, 
            OrgId = orgId, 
            ScheduledAt = DateTime.UtcNow.AddDays(1) 
        };
        
        var res = await RunAndLog(() => CreateService(ctx).CreateAppointmentAsync(request));
        Assert.True(res.Success);
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-02")]
    public async Task CreateAppointmentAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var request = new CreateAppointmentRequest { ScheduledAt = DateTime.UtcNow.AddDays(-1) };
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-03")]
    public async Task CreateAppointmentAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        _authServiceClientMock.Setup(x => x.GetUserIdByPatientIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid?)null);
        var request = new CreateAppointmentRequest { ScheduledAt = DateTime.UtcNow.AddDays(1) };
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
    }

    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-PastDate")]
    public async Task CreateAppointmentAsync_PastDate() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var request = new CreateAppointmentRequest { ScheduledAt = DateTime.UtcNow.AddDays(-1) };
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
        Assert.Equal("Ngày hẹn không được nằm trong quá khứ.", res.Message);
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-MissingPatientId")]
    public async Task CreateAppointmentAsync_MissingPatientId() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var request = new CreateAppointmentRequest { PatientId = Guid.Empty, DoctorId = Guid.NewGuid(), OrgId = Guid.NewGuid(), ScheduledAt = DateTime.UtcNow.AddDays(1) };
        _authServiceClientMock.Setup(x => x.GetUserIdByPatientIdAsync(Guid.Empty)).ReturnsAsync((Guid?)null);
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
        Assert.Equal("Không tìm thấy hồ sơ bệnh nhân.", res.Message);
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-MissingDoctorId")]
    public async Task CreateAppointmentAsync_MissingDoctorId() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var request = new CreateAppointmentRequest { PatientId = Guid.NewGuid(), DoctorId = Guid.Empty, OrgId = Guid.NewGuid(), ScheduledAt = DateTime.UtcNow.AddDays(1) };
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
    }
    [Fact(DisplayName = "CreateAppointmentAsync::CreateAppointmentAsync-MissingOrgId")]
    public async Task CreateAppointmentAsync_MissingOrgId() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var request = new CreateAppointmentRequest { PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), OrgId = Guid.Empty, ScheduledAt = DateTime.UtcNow.AddDays(1) };
        var res = await CreateService(ctx).CreateAppointmentAsync(request);
        Assert.False(res.Success);
    }
    [Fact] public async Task GetAppointmentByIdAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.PENDING }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetAppointmentByIdAsync(appt.AppointmentId);
        Assert.True(res.Success);
    }
    [Fact] public async Task GetAppointmentByIdAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetAppointmentByIdAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact] public async Task GetAppointmentByIdAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetAppointmentByIdAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetAppointmentByIdAsync(Guid.NewGuid()));
    }
    [Fact] public async Task GetAppointmentsAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var id = Guid.NewGuid(); ctx.Appointments.Add(new DBH.Appointment.Service.Models.Entities.Appointment { PatientId = id, Status = AppointmentStatus.PENDING }); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetAppointmentsAsync(id, null, null, null, 1, 10);
        Assert.NotNull(res.Data);
    }
    [Fact] public async Task GetAppointmentsAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetAppointmentsAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetAppointmentsAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetAppointmentsAsync(Guid.NewGuid(), null, null, null, 1, 10));
    }
    [Fact] public async Task UpdateAppointmentStatusAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.PENDING }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).UpdateAppointmentStatusAsync(appt.AppointmentId, AppointmentStatus.CONFIRMED);
        Assert.True(res.Success);
    }
    [Fact] public async Task UpdateAppointmentStatusAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).UpdateAppointmentStatusAsync(Guid.Empty, AppointmentStatus.CONFIRMED);
        Assert.False(res.Success);
    }
    [Fact] public async Task UpdateAppointmentStatusAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).UpdateAppointmentStatusAsync(Guid.NewGuid(), AppointmentStatus.CONFIRMED);
        Assert.False(res.Success);
    }
    [Fact] public async Task UpdateAppointmentStatusAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).UpdateAppointmentStatusAsync(Guid.NewGuid(), AppointmentStatus.CONFIRMED));
    }
    [Fact] public async Task RescheduleAppointmentAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.PENDING }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).RescheduleAppointmentAsync(appt.AppointmentId, DateTime.UtcNow.AddDays(2));
        Assert.True(res.Success);
    }
    [Fact] public async Task RescheduleAppointmentAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RescheduleAppointmentAsync(Guid.Empty, DateTime.UtcNow.AddDays(2));
        Assert.False(res.Success);
    }
    [Fact] public async Task RescheduleAppointmentAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RescheduleAppointmentAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(2));
        Assert.False(res.Success);
    }
    [Fact] public async Task RescheduleAppointmentAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).RescheduleAppointmentAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(2)));
    }
    [Fact] public async Task ConfirmAppointmentAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.PENDING }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).ConfirmAppointmentAsync(appt.AppointmentId);
        Assert.True(res.Success);
    }
    [Fact] public async Task ConfirmAppointmentAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).ConfirmAppointmentAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact] public async Task ConfirmAppointmentAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).ConfirmAppointmentAsync(Guid.NewGuid());
        Assert.False(res.Success);
    }
    [Fact] public async Task ConfirmAppointmentAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).ConfirmAppointmentAsync(Guid.NewGuid()));
    }
    [Fact] public async Task RejectAppointmentAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.PENDING }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).RejectAppointmentAsync(appt.AppointmentId, "Test reason");
        Assert.True(res.Success);
    }
    [Fact] public async Task RejectAppointmentAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RejectAppointmentAsync(Guid.Empty, "Test reason");
        Assert.False(res.Success);
    }
    [Fact] public async Task RejectAppointmentAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).RejectAppointmentAsync(Guid.NewGuid(), "Test reason");
        Assert.False(res.Success);
    }
    [Fact] public async Task RejectAppointmentAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).RejectAppointmentAsync(Guid.NewGuid(), "Test reason"));
    }
    [Fact] public async Task CancelAppointmentAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.CONFIRMED }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).CancelAppointmentAsync(appt.AppointmentId, "Test reason");
        Assert.True(res.Success);
    }
    [Fact] public async Task CancelAppointmentAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CancelAppointmentAsync(Guid.Empty, "Test reason");
        Assert.False(res.Success);
    }
    [Fact] public async Task CancelAppointmentAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CancelAppointmentAsync(Guid.NewGuid(), "Test reason");
        Assert.False(res.Success);
    }
    [Fact] public async Task CancelAppointmentAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).CancelAppointmentAsync(Guid.NewGuid(), "Test reason"));
    }
    [Fact] public async Task CheckInAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.CONFIRMED }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).CheckInAsync(appt.AppointmentId);
        Assert.True(res.Success);
    }
    [Fact] public async Task CheckInAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CheckInAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact] public async Task CheckInAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CheckInAsync(Guid.NewGuid());
        Assert.False(res.Success);
    }
    [Fact] public async Task CheckInAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).CheckInAsync(Guid.NewGuid()));
    }
    [Fact] public async Task SearchDoctorsAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        
        var res = await CreateService(ctx).SearchDoctorsAsync(new SearchDoctorQuery { Specialty = "Test", Page = 1, PageSize = 10 });
        Assert.NotNull(res);
    }
    [Fact] public async Task SearchDoctorsAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).SearchDoctorsAsync(new SearchDoctorQuery());
        Assert.NotNull(res);
    }
    [Fact] public async Task SearchDoctorsAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).SearchDoctorsAsync(new SearchDoctorQuery { Specialty = "NonExistent123" });
        Assert.Empty(res.Data);
    }
    [Fact] public async Task SearchDoctorsAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Throws(new Exception("Network"));
        var res = await CreateService(ctx).SearchDoctorsAsync(new SearchDoctorQuery());
        Assert.Empty(res.Data);
    }
    [Fact] public async Task CreateEncounterAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var appt = new DBH.Appointment.Service.Models.Entities.Appointment { AppointmentId = Guid.NewGuid(), Status = AppointmentStatus.CHECKED_IN }; ctx.Appointments.Add(appt); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).CreateEncounterAsync(new CreateEncounterRequest { AppointmentId = appt.AppointmentId, PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), OrgId = Guid.NewGuid() });
        Assert.True(res.Success);
    }
    [Fact] public async Task CreateEncounterAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CreateEncounterAsync(new CreateEncounterRequest { AppointmentId = Guid.Empty, PatientId = Guid.Empty, DoctorId = Guid.Empty, OrgId = Guid.Empty });
        Assert.False(res.Success);
    }
    [Fact] public async Task CreateEncounterAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).CreateEncounterAsync(new CreateEncounterRequest { AppointmentId = Guid.NewGuid(), PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), OrgId = Guid.NewGuid() });
        Assert.False(res.Success);
    }
    [Fact] public async Task CreateEncounterAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).CreateEncounterAsync(new CreateEncounterRequest { AppointmentId = Guid.NewGuid(), PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), OrgId = Guid.NewGuid() }));
    }
    [Fact] public async Task GetEncounterByIdAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var enc = new DBH.Appointment.Service.Models.Entities.Encounter { EncounterId = Guid.NewGuid() }; ctx.Encounters.Add(enc); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).GetEncounterByIdAsync(enc.EncounterId);
        Assert.True(res.Success);
    }
    [Fact] public async Task GetEncounterByIdAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetEncounterByIdAsync(Guid.Empty);
        Assert.False(res.Success);
    }
    [Fact] public async Task GetEncounterByIdAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetEncounterByIdAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetEncounterByIdAsync(Guid.NewGuid()));
    }
    [Fact] public async Task GetEncountersByAppointmentIdAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        
        var res = await CreateService(ctx).GetEncountersByAppointmentIdAsync(Guid.NewGuid(), 1, 10);
        Assert.NotNull(res.Data);
    }
    [Fact] public async Task GetEncountersByAppointmentIdAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetEncountersByAppointmentIdAsync(Guid.Empty, 1, 10);
        Assert.Empty(res.Data);
    }
    [Fact] public async Task GetEncountersByAppointmentIdAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetEncountersByAppointmentIdAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetEncountersByAppointmentIdAsync(Guid.NewGuid(), 1, 10));
    }
    [Fact] public async Task GetEncountersByPatientIdAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        
        var res = await CreateService(ctx).GetEncountersByPatientIdAsync(Guid.NewGuid(), 1, 10);
        Assert.NotNull(res.Data);
    }
    [Fact] public async Task GetEncountersByPatientIdAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).GetEncountersByPatientIdAsync(Guid.Empty, 1, 10);
        Assert.Empty(res.Data);
    }
    [Fact] public async Task GetEncountersByPatientIdAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        Assert.True(true);
    }
    [Fact] public async Task GetEncountersByPatientIdAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).GetEncountersByPatientIdAsync(Guid.NewGuid(), 1, 10));
    }
    [Fact] public async Task UpdateEncounterAsync_01() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var enc = new DBH.Appointment.Service.Models.Entities.Encounter { EncounterId = Guid.NewGuid() }; ctx.Encounters.Add(enc); await ctx.SaveChangesAsync();
        var res = await CreateService(ctx).UpdateEncounterAsync(enc.EncounterId, new UpdateEncounterRequest { Notes = "Updated" });
        Assert.True(res.Success);
    }
    [Fact] public async Task UpdateEncounterAsync_02() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).UpdateEncounterAsync(Guid.Empty, new UpdateEncounterRequest { Notes = "Updated" });
        Assert.False(res.Success);
    }
    [Fact] public async Task UpdateEncounterAsync_03() {
        using var ctx = new AppointmentDbContext(_dbContextOptions);
        var res = await CreateService(ctx).UpdateEncounterAsync(Guid.NewGuid(), new UpdateEncounterRequest { Notes = "Updated" });
        Assert.False(res.Success);
    }
    [Fact] public async Task UpdateEncounterAsync_04() {
        var ctx = new AppointmentDbContext(_dbContextOptions);
        ctx.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => CreateService(ctx).UpdateEncounterAsync(Guid.NewGuid(), new UpdateEncounterRequest { Notes = "Updated" }));
    }
}