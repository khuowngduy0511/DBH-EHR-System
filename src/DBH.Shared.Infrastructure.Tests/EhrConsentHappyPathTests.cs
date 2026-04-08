using System.Net;
using System.Net.Http;
using System.Text.Json;
using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Consent.Service.Services;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.EHR.Service.Services;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBH.Shared.Infrastructure.Tests;

public class EhrConsentHappyPathTests
{
    [Fact]
    public async Task CreateEhr_ThenConsentGrant_ChangesAccessFromDeniedToAllowed()
    {
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var sync = new RecordingBlockchainSyncService();
        var notifications = new RecordingNotificationServiceClient();

        var ehrRepo = new InMemoryEhrRecordRepository();
        var ehrService = new EhrService(
            ehrRepo,
            NullLogger<EhrService>.Instance,
            new StubHttpClientFactory(),
            new StubEhrAuthServiceClient(),
            new HttpContextAccessor(),
            sync,
            blockchainService: new DummyEhrBlockchainService(),
            consentBlockchainService: null,
            notificationClient: notifications);

        using var payload = JsonDocument.Parse("{\"resourceType\":\"Bundle\",\"type\":\"document\",\"entry\":[{\"resource\":{\"resourceType\":\"Condition\",\"code\":{\"text\":\"Common Cold\"}}}]}");

        var created = await ehrService.CreateEhrRecordAsync(new CreateEhrRecordDto
        {
            PatientId = patientId,
            OrgId = orgId,
            Data = payload.RootElement
        });

        Assert.NotEqual(Guid.Empty, created.EhrId);
        Assert.Equal(patientId, created.PatientId);
        Assert.NotNull(created.VersionId);
        Assert.NotNull(created.FileId);
        Assert.False(string.IsNullOrWhiteSpace(created.DataHash));

        Assert.Single(ehrRepo.Records);
        Assert.Single(ehrRepo.Versions);
        Assert.Single(ehrRepo.Files);

        Assert.Equal(1, sync.EhrHashCount);
        Assert.NotNull(sync.LastEhrHash);
        Assert.Equal(created.EhrId.ToString(), sync.LastEhrHash!.EhrId);

        Assert.Contains(notifications.Sent, n => n.Type == "EhrCreated" && n.RecipientUserId == patientId);

        await using var consentDb = CreateConsentDbContext();
        var consentService = new ConsentService(
            consentDb,
            NullLogger<ConsentService>.Instance,
            new StubHttpClientFactory(),
            sync,
            blockchainService: new DummyConsentBlockchainService(),
            ehrBlockchainService: null,
            notificationClient: notifications);

        var before = await consentService.VerifyConsentAsync(new VerifyConsentRequest
        {
            PatientId = patientId,
            GranteeId = doctorId,
            EhrId = created.EhrId,
            RequiredPermission = ConsentPermission.READ
        });

        Assert.False(before.HasAccess);

        var grant = await consentService.GrantConsentAsync(new GrantConsentRequest
        {
            PatientId = patientId,
            PatientDid = $"did:fabric:patient:{patientId}",
            GranteeId = doctorId,
            GranteeDid = $"did:fabric:doctor:{doctorId}",
            GranteeType = GranteeType.DOCTOR,
            EhrId = created.EhrId,
            Permission = ConsentPermission.READ,
            Purpose = ConsentPurpose.TREATMENT,
            DurationDays = 30
        });

        Assert.True(grant.Success);
        Assert.NotNull(grant.Data);
        Assert.Equal(created.EhrId, grant.Data!.EhrId);

        var after = await consentService.VerifyConsentAsync(new VerifyConsentRequest
        {
            PatientId = patientId,
            GranteeId = doctorId,
            EhrId = created.EhrId,
            RequiredPermission = ConsentPermission.READ
        });

        Assert.True(after.HasAccess);

        Assert.Equal(1, sync.ConsentGrantCount);
        Assert.Contains(notifications.Sent, n => n.Type == "ConsentGranted" && n.RecipientUserId == doctorId);
    }

    private static ConsentDbContext CreateConsentDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsentDbContext>()
            .UseInMemoryDatabase($"ehr-consent-tests-{Guid.NewGuid()}")
            .Options;

        return new ConsentDbContext(options);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class StubEhrAuthServiceClient : IAuthServiceClient
    {
        public Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId, string bearerToken) => Task.FromResult<Guid?>(null);

        public Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId, string bearerToken) => Task.FromResult<Guid?>(null);

        public Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId, string bearerToken)
            => Task.FromResult<AuthUserProfileDetailDto?>(null);
    }

    private sealed class InMemoryEhrRecordRepository : IEhrRecordRepository
    {
        public List<EhrRecord> Records { get; } = new();
        public List<EhrVersion> Versions { get; } = new();
        public List<EhrFile> Files { get; } = new();

        public Task<EhrRecord> CreateAsync(EhrRecord record)
        {
            if (record.EhrId == Guid.Empty)
            {
                record.EhrId = Guid.NewGuid();
            }

            Records.Add(record);
            return Task.FromResult(record);
        }

        public Task<EhrVersion> CreateVersionAsync(EhrVersion version)
        {
            if (version.VersionId == Guid.Empty)
            {
                version.VersionId = Guid.NewGuid();
            }

            Versions.Add(version);
            return Task.FromResult(version);
        }

        public Task<EhrFile> CreateFileAsync(EhrFile file)
        {
            if (file.FileId == Guid.Empty)
            {
                file.FileId = Guid.NewGuid();
            }

            Files.Add(file);
            return Task.FromResult(file);
        }

        public Task<EhrRecord> UpdateAsync(EhrRecord record) => Task.FromResult(record);

        public Task<EhrVersion> UpdateVersionAsync(EhrVersion version) => Task.FromResult(version);

        public Task<EhrRecord?> GetByIdAsync(Guid ehrId)
            => Task.FromResult<EhrRecord?>(Records.FirstOrDefault(r => r.EhrId == ehrId));

        public Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId)
        {
            var record = Records.FirstOrDefault(r => r.EhrId == ehrId);
            if (record != null)
            {
                record.Versions = Versions.Where(v => v.EhrId == ehrId).ToList();
                record.Files = Files.Where(f => f.EhrId == ehrId).ToList();
            }

            return Task.FromResult<EhrRecord?>(record);
        }

        public Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId)
            => Task.FromResult<IEnumerable<EhrRecord>>(Records.Where(r => r.PatientId == patientId).ToList());

        public Task<IEnumerable<EhrRecord>> GetByOrgIdAsync(Guid orgId)
            => Task.FromResult<IEnumerable<EhrRecord>>(Records.Where(r => r.OrgId == orgId).ToList());

        public Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId)
            => Task.FromResult<EhrVersion?>(Versions.Where(v => v.EhrId == ehrId).OrderByDescending(v => v.VersionNumber).FirstOrDefault());

        public Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId)
            => Task.FromResult<IEnumerable<EhrVersion>>(Versions.Where(v => v.EhrId == ehrId).ToList());

        public Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId)
            => Task.FromResult<IEnumerable<EhrFile>>(Files.Where(f => f.EhrId == ehrId).ToList());

        public Task<EhrVersion?> GetVersionByIdAsync(Guid ehrId, Guid versionId)
            => Task.FromResult<EhrVersion?>(Versions.FirstOrDefault(v => v.EhrId == ehrId && v.VersionId == versionId));

        public Task<EhrFile?> GetFileByIdAsync(Guid ehrId, Guid fileId)
            => Task.FromResult<EhrFile?>(Files.FirstOrDefault(f => f.EhrId == ehrId && f.FileId == fileId));

        public Task DeleteFileAsync(EhrFile file)
        {
            Files.Remove(file);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBlockchainSyncService : IBlockchainSyncService
    {
        public int EhrHashCount { get; private set; }
        public int ConsentGrantCount { get; private set; }
        public EhrHashRecord? LastEhrHash { get; private set; }
        public ConsentRecord? LastGrantedConsent { get; private set; }
        public int PendingCount => 0;

        public void EnqueueEhrHash(EhrHashRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null)
        {
            EhrHashCount++;
            LastEhrHash = record;
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
        }

        public void EnqueueFabricCaEnrollment(string enrollmentId, string username, string role, Func<string, Task>? onFailure = null)
        {
        }
    }

    private sealed class DummyEhrBlockchainService : IEhrBlockchainService
    {
        public Task<BlockchainTransactionResult> CommitEhrHashAsync(EhrHashRecord record)
            => Task.FromResult(new BlockchainTransactionResult { Success = true });

        public Task<EhrHashRecord?> GetEhrHashAsync(string ehrId, int version)
            => Task.FromResult<EhrHashRecord?>(null);

        public Task<List<EhrHashRecord>> GetEhrHistoryAsync(string ehrId)
            => Task.FromResult(new List<EhrHashRecord>());

        public Task<bool> VerifyEhrIntegrityAsync(string ehrId, int version, string currentHash)
            => Task.FromResult(false);
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
