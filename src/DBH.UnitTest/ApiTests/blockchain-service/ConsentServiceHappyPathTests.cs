using System.Net;
using System.Net.Http;
using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Consent.Service.Services;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBH.UnitTest.ApiTests;

public class ConsentServiceHappyPathTests
{
    [Fact]
    public async Task DoctorAccessFlow_BeforeAndAfterConsent_HappyPath()
    {
        await using var db = CreateDbContext();
        var sync = new RecordingBlockchainSyncService();
        var notifications = new RecordingNotificationServiceClient();

        var sut = new ConsentService(
            db,
            NullLogger<ConsentService>.Instance,
            new StubHttpClientFactory(),
            new StubHttpContextAccessor(),
            sync,
            blockchainService: new DummyConsentBlockchainService(),
            ehrBlockchainService: null,
            notificationClient: notifications);

        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var before = await sut.VerifyConsentAsync(new VerifyConsentRequest
        {
            PatientId = patientId,
            GranteeId = doctorId,
            RequiredPermission = ConsentPermission.READ
        });

        Assert.False(before.HasAccess);

        var grant = await sut.GrantConsentAsync(new GrantConsentRequest
        {
            PatientId = patientId,
            PatientDid = $"did:fabric:patient:{patientId}",
            GranteeId = doctorId,
            GranteeDid = $"did:fabric:doctor:{doctorId}",
            GranteeType = GranteeType.DOCTOR,
            Permission = ConsentPermission.READ,
            Purpose = ConsentPurpose.TREATMENT,
            DurationDays = 30
        });

        Assert.True(grant.Success);
        Assert.NotNull(grant.Data);
        Assert.Equal(ConsentStatus.ACTIVE, grant.Data!.Status);

        var after = await sut.VerifyConsentAsync(new VerifyConsentRequest
        {
            PatientId = patientId,
            GranteeId = doctorId,
            RequiredPermission = ConsentPermission.READ
        });

        Assert.True(after.HasAccess);
        Assert.NotNull(after.ConsentId);
        Assert.Equal(ConsentPermission.READ, after.Permission);

        var persisted = await db.Consents.SingleAsync();
        Assert.Equal(patientId, persisted.PatientId);
        Assert.Equal(doctorId, persisted.GranteeId);
        Assert.Equal(ConsentStatus.ACTIVE, persisted.Status);

        Assert.Equal(1, sync.ConsentGrantCount);
        Assert.NotNull(sync.LastGrantedConsent);
        Assert.Equal(persisted.BlockchainConsentId, sync.LastGrantedConsent!.ConsentId);
        Assert.EndsWith("Z", sync.LastGrantedConsent!.GrantedAt);
        Assert.DoesNotContain("+07:00", sync.LastGrantedConsent.GrantedAt);

        Assert.Single(notifications.Sent);
        Assert.Equal(doctorId, notifications.Sent[0].RecipientUserId);
        Assert.Equal("ConsentGranted", notifications.Sent[0].Type);
    }

    private static ConsentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsentDbContext>()
            .UseInMemoryDatabase($"consent-tests-{Guid.NewGuid()}")
            .Options;

        return new ConsentDbContext(options);
    }

    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class RecordingBlockchainSyncService : IBlockchainSyncService
    {
        public int ConsentGrantCount { get; private set; }
        public ConsentRecord? LastGrantedConsent { get; private set; }
        public AuditEntry? LastAuditEntry { get; private set; }
        public int PendingCount => 0;

        public void EnqueueEhrHash(EhrHashRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null)
        {
        }

        public void EnqueueConsentGrant(ConsentRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null)
        {
            ConsentGrantCount++;
            LastGrantedConsent = record;
        }

        public void EnqueueConsentRevoke(string consentId, string revokedAt, string? reason, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null)
        {
        }

        public void EnqueueAuditEntry(AuditEntry entry, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null)
        {
            LastAuditEntry = entry;
        }

        public void EnqueueFabricCaEnrollment(string enrollmentId, string username, string role, Func<string, Task>? onFailure = null)
        {
        }
    }

    private sealed class DummyConsentBlockchainService : IConsentBlockchainService
    {
        public Task<BlockchainTransactionResult> GrantConsentAsync(ConsentRecord record)
            => Task.FromResult(new BlockchainTransactionResult { Success = true });

        public Task<BlockchainTransactionResult> RevokeConsentAsync(string consentId, string revokedAt, string? reason)
            => Task.FromResult(new BlockchainTransactionResult { Success = true });

        public Task<ConsentRecord?> GetConsentAsync(string consentId)
            => Task.FromResult<ConsentRecord?>(null);

        public Task<bool> VerifyConsentAsync(string consentId, string granteeDid)
            => Task.FromResult(false);

        public Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientDid)
            => Task.FromResult(new List<ConsentRecord>());

        public Task<List<ConsentRecord>> GetConsentHistoryAsync(string consentId)
            => Task.FromResult(new List<ConsentRecord>());
    }

    private sealed class RecordingNotificationServiceClient : INotificationServiceClient
    {
        public List<SentNotification> Sent { get; } = new();

        public Task SendAsync(
            Guid recipientUserId,
            string title,
            string body,
            string type,
            string priority = "Normal",
            string? referenceId = null,
            string? referenceType = null,
            string? actionUrl = null)
        {
            Sent.Add(new SentNotification(recipientUserId, title, body, type, priority, referenceId, referenceType, actionUrl));
            return Task.CompletedTask;
        }
    }

    private sealed record SentNotification(
        Guid RecipientUserId,
        string Title,
        string Body,
        string Type,
        string Priority,
        string? ReferenceId,
        string? ReferenceType,
        string? ActionUrl);
}
