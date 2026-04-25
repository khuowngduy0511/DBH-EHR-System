using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AppointmentEntity = DBH.Appointment.Service.Models.Entities.Appointment;
using EncounterEntity = DBH.Appointment.Service.Models.Entities.Encounter;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;
using DBH.Appointment.Service.Services;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBH.UnitTest.UnitTests;

internal sealed class AppointmentServiceTestSupport
{
    internal static TestFixture CreateFixture()
    {
        var db = CreateDbContext();
        var httpContextAccessor = new StubHttpContextAccessor();
        var authResponses = new AuthResponses();
        var organizationResponses = new OrganizationResponses();
        var consentResponses = new ConsentResponses();
        var ehrResponses = new EhrResponses();

        var httpClientFactory = new ScenarioHttpClientFactory(authResponses, organizationResponses, consentResponses, ehrResponses);
        var authClient = new AuthServiceClient(httpClientFactory, httpContextAccessor, NullLogger<AuthServiceClient>.Instance);
        var messages = new RecordingMessagePublisher();
        var notifications = new RecordingNotificationServiceClient();

        var sut = new AppointmentService(
            db,
            NullLogger<AppointmentService>.Instance,
            httpClientFactory,
            httpContextAccessor,
            authClient,
            messages,
            notifications);

        return new TestFixture(
            sut,
            authClient,
            db,
            httpContextAccessor,
            authResponses,
            organizationResponses,
            consentResponses,
            ehrResponses,
            messages,
            notifications);
    }

    internal static AppointmentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppointmentDbContext>()
            .UseInMemoryDatabase($"appointment-tests-{Guid.NewGuid()}")
            .Options;

        return new AppointmentDbContext(options);
    }

    internal static DefaultHttpContext CreateHttpContext(Guid userId, string role = "Doctor", string? token = "Bearer unit-test-token")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "test"));

        if (!string.IsNullOrWhiteSpace(token))
        {
            context.Request.Headers.Authorization = token;
        }

        return context;
    }

    internal static AppointmentEntity SeedAppointment(
        AppointmentDbContext db,
        Guid patientId,
        Guid doctorId,
        Guid orgId,
        AppointmentStatus status,
        DateTime scheduledAtUtc)
    {
        var appointment = new AppointmentEntity
        {
            PatientId = patientId,
            DoctorId = doctorId,
            OrgId = orgId,
            Status = status,
            ScheduledAt = scheduledAtUtc,
            CreatedAt = scheduledAtUtc.AddHours(-1)
        };

        db.Appointments.Add(appointment);
        db.SaveChanges();
        return appointment;
    }

    internal static EncounterEntity SeedEncounter(
        AppointmentDbContext db,
        Guid patientId,
        Guid doctorId,
        Guid appointmentId,
        Guid orgId,
        string? notes = null)
    {
        var encounter = new EncounterEntity
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = appointmentId,
            OrgId = orgId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        db.Encounters.Add(encounter);
        db.SaveChanges();
        return encounter;
    }

    internal static AuthUserProfileDetailDto BuildProfile(Guid userId, string fullName)
    {
        return new AuthUserProfileDetailDto
        {
            UserId = userId,
            FullName = fullName,
            Email = $"{fullName.Replace(" ", ".", StringComparison.OrdinalIgnoreCase).ToLowerInvariant()}@example.com",
            Phone = "0900000000",
            Gender = "Other",
            DateOfBirth = new DateTime(1990, 1, 1),
            Status = "ACTIVE",
            Roles = new[] { "User" }
        };
    }

    internal sealed record TestFixture(
        AppointmentService Sut,
        AuthServiceClient AuthClient,
        AppointmentDbContext Db,
        StubHttpContextAccessor HttpContextAccessor,
        AuthResponses AuthResponses,
        OrganizationResponses OrganizationResponses,
        ConsentResponses ConsentResponses,
        EhrResponses EhrResponses,
        RecordingMessagePublisher Messages,
        RecordingNotificationServiceClient Notifications)
    {
        public void SetActor(Guid userId, string role = "Doctor", string? token = "Bearer unit-test-token")
        {
            HttpContextAccessor.HttpContext = CreateHttpContext(userId, role, token);
        }
    }

    internal sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    internal sealed class ScenarioHttpClientFactory(
        AuthResponses authResponses,
        OrganizationResponses organizationResponses,
        ConsentResponses consentResponses,
        EhrResponses ehrResponses) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return name switch
            {
                "AuthService" => new HttpClient(new ScenarioHttpMessageHandler(authResponses.Handle)) { BaseAddress = new Uri("http://localhost") },
                "OrganizationService" => new HttpClient(new ScenarioHttpMessageHandler(organizationResponses.Handle)) { BaseAddress = new Uri("http://localhost") },
                "ConsentService" => new HttpClient(new ScenarioHttpMessageHandler(consentResponses.Handle)) { BaseAddress = new Uri("http://localhost") },
                "EhrService" => new HttpClient(new ScenarioHttpMessageHandler(ehrResponses.Handle)) { BaseAddress = new Uri("http://localhost") },
                _ => new HttpClient(new ScenarioHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))) { BaseAddress = new Uri("http://localhost") }
            };
        }
    }

    internal sealed class ScenarioHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }

    internal sealed class AuthResponses
    {
        public Dictionary<Guid, Guid> PatientUserIds { get; } = new();
        public Dictionary<Guid, Guid> DoctorUserIds { get; } = new();
        public Dictionary<Guid, AuthUserProfileDetailDto> Profiles { get; } = new();
        public bool ThrowOnLookup { get; set; }

        public HttpResponseMessage Handle(HttpRequestMessage request)
        {
            if (ThrowOnLookup)
            {
                throw new HttpRequestException("Auth service unavailable");
            }

            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (path.Contains("/api/v1/auth/user-id?patientId=", StringComparison.OrdinalIgnoreCase))
            {
                var patientId = ParseGuidQueryValue(path, "patientId");
                return CreateUserIdResponse(patientId.HasValue && PatientUserIds.TryGetValue(patientId.Value, out var userId) ? userId : null);
            }

            if (path.Contains("/api/v1/auth/user-id?doctorId=", StringComparison.OrdinalIgnoreCase))
            {
                var doctorId = ParseGuidQueryValue(path, "doctorId");
                return CreateUserIdResponse(doctorId.HasValue && DoctorUserIds.TryGetValue(doctorId.Value, out var userId) ? userId : null);
            }

            if (path.StartsWith("/api/v1/auth/users/", StringComparison.OrdinalIgnoreCase))
            {
                var userIdText = path.Split('/').Last();
                if (Guid.TryParse(userIdText, out var userId) && Profiles.TryGetValue(userId, out var profile))
                {
                    return JsonOk(profile);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    internal sealed class OrganizationResponses
    {
        public Dictionary<Guid, bool> Organizations { get; } = new();
        public Dictionary<Guid, List<object>> MembershipsByUser { get; } = new();
        public List<object> SearchResults { get; } = new();
        public bool ReturnServerErrorForSearch { get; set; }

        public HttpResponseMessage Handle(HttpRequestMessage request)
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;

            if (path.StartsWith("/api/v1/organizations/", StringComparison.OrdinalIgnoreCase))
            {
                var orgText = path.Split('/').Last();
                if (Guid.TryParse(orgText, out var orgId) && Organizations.TryGetValue(orgId, out var exists) && exists)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (path.StartsWith("/api/v1/memberships/by-user/", StringComparison.OrdinalIgnoreCase))
            {
                var userText = path.Split('/').Last().Split('?')[0];
                if (Guid.TryParse(userText, out var userId) && MembershipsByUser.TryGetValue(userId, out var memberships))
                {
                    return JsonOk(new { data = memberships });
                }

                return JsonOk(new { data = Array.Empty<object>() });
            }

            if (path.StartsWith("/api/v1/memberships/search", StringComparison.OrdinalIgnoreCase))
            {
                if (ReturnServerErrorForSearch)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                return JsonOk(new { data = SearchResults, page = 1, pageSize = 10, totalCount = SearchResults.Count });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    internal sealed class ConsentResponses
    {
        public Dictionary<Guid, List<object>> ConsentsByDoctor { get; } = new();
        public bool ReturnServerError { get; set; }

        public HttpResponseMessage Handle(HttpRequestMessage request)
        {
            if (ReturnServerError)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (path.StartsWith("/api/v1/consents/by-grantee/", StringComparison.OrdinalIgnoreCase))
            {
                var doctorText = path.Split('/').Last().Split('?')[0];
                if (Guid.TryParse(doctorText, out var doctorId) && ConsentsByDoctor.TryGetValue(doctorId, out var consents))
                {
                    return JsonOk(new { data = consents });
                }

                return JsonOk(new { data = Array.Empty<object>() });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    internal sealed class EhrResponses
    {
        public bool ReturnServerError { get; set; }
        public Guid CreatedEhrId { get; set; } = Guid.NewGuid();

        public HttpResponseMessage Handle(HttpRequestMessage request)
        {
            if (ReturnServerError)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.Contains("/api/v1/ehr/records", StringComparison.OrdinalIgnoreCase) == true)
            {
                return JsonOk(new { ehrId = CreatedEhrId });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    internal sealed class RecordingMessagePublisher : IMessagePublisher
    {
        public List<string> PublishedTypes { get; } = new();

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            PublishedTypes.Add(typeof(T).Name);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(Uri destinationAddress, T message, CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;

        public Task SendToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;

        public Task ScheduleAsync<T>(T message, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;
    }

    internal sealed class RecordingNotificationServiceClient : INotificationServiceClient
    {
        public List<(Guid RecipientUserId, string Title, string Body, string Type)> Sent { get; } = new();

        public Task SendAsync(Guid recipientUserId, string title, string body, string type, string priority = "Normal", string? referenceId = null, string? referenceType = null, string? actionUrl = null)
        {
            Sent.Add((recipientUserId, title, body, type));
            return Task.CompletedTask;
        }
    }

    private static Guid? ParseGuidQueryValue(string path, string key)
    {
        var marker = $"{key}=";
        var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var value = path[(index + marker.Length)..].Split('&')[0];
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static HttpResponseMessage CreateUserIdResponse(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return JsonOk(new { userId = userId.Value });
    }

    private static HttpResponseMessage JsonOk(object payload)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
}