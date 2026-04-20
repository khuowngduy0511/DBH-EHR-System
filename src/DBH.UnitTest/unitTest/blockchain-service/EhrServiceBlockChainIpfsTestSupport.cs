using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.EHR.Service.Services;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBH.UnitTest.UnitTests;

internal static class EhrServiceBlockChainIpfsTestSupport
{
    internal static EhrService CreateService(
        IEhrRecordRepository repo,
        RecordingBlockchainSyncService sync,
        IEhrBlockchainService? blockchainService,
        bool includeUserIdentity = false,
        AuthResponseMode authMode = AuthResponseMode.NotFound)
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContext(includeUserIdentity)
        };

        return new EhrService(
            repo,
            NullLogger<EhrService>.Instance,
            new StubHttpClientFactory(authMode),
            new StubEhrAuthServiceClient(),
            httpContextAccessor,
            sync,
            blockchainService: blockchainService,
            consentBlockchainService: null,
            notificationClient: null);
    }

    internal static HttpContext CreateHttpContext(bool includeUserIdentity)
    {
        var context = new DefaultHttpContext();

        if (!includeUserIdentity)
        {
            return context;
        }

        var userId = Guid.NewGuid();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                "test"));

        context.Request.Headers.Authorization = "Bearer unit-test-token";
        return context;
    }

    internal enum AuthResponseMode
    {
        NotFound,
        MissingPublicKey,
        MissingEncryptedPrivateKey,
        ValidKeys
    }

    internal sealed class StubHttpClientFactory(AuthResponseMode mode) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(mode))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    internal sealed class StubHttpMessageHandler(AuthResponseMode mode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (mode == AuthResponseMode.NotFound)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var keyJson = mode switch
            {
                AuthResponseMode.MissingPublicKey => "{\"publicKey\":\"\",\"encryptedPrivateKey\":\"placeholder\"}",
                AuthResponseMode.MissingEncryptedPrivateKey => "{\"publicKey\":\"placeholder\",\"encryptedPrivateKey\":\"\"}",
                AuthResponseMode.ValidKeys => BuildValidKeysJson(),
                _ => "{}"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(keyJson)
            });
        }

        private static string BuildValidKeysJson()
        {
            var pair = AsymmetricEncryptionService.GenerateKeyPair();
            var encryptedPrivateKey = MasterKeyEncryptionService.Encrypt(pair.PrivateKey);

            return JsonSerializer.Serialize(new
            {
                userId = Guid.NewGuid(),
                publicKey = pair.PublicKey,
                encryptedPrivateKey
            });
        }
    }

    internal sealed class StubEhrAuthServiceClient : IAuthServiceClient
    {
        public Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId, string bearerToken) => Task.FromResult<Guid?>(null);

        public Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId, string bearerToken) => Task.FromResult<Guid?>(null);

        public Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId, string bearerToken)
            => Task.FromResult<AuthUserProfileDetailDto?>(null);
    }

    internal sealed class RecordingBlockchainSyncService : IBlockchainSyncService
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

    internal sealed class DummyEhrBlockchainService : IEhrBlockchainService
    {
        public Task<BlockchainTransactionResult> CommitEhrHashAsync(EhrHashRecord record)
            => Task.FromResult(new BlockchainTransactionResult { Success = true });

        public Task<EhrHashRecord?> GetEhrHashAsync(string ehrId, int version)
            => Task.FromResult<EhrHashRecord?>(null);

        public Task<List<EhrHashRecord>> GetEhrHistoryAsync(string ehrId)
            => Task.FromResult(new List<EhrHashRecord>());

        public Task<bool> VerifyEhrIntegrityAsync(string ehrId, int version, string currentHash)
            => Task.FromResult(true);

        public Task<List<EhrHashRecord>> GetEhrByPatientAsync(string patientDid)
            => Task.FromResult(new List<EhrHashRecord>());
    }

    internal sealed class InMemoryEhrRecordRepository : IEhrRecordRepository
    {
        private readonly List<EhrRecord> _records = new();
        private readonly List<EhrVersion> _versions = new();
        private readonly List<EhrFile> _files = new();

        internal void SeedRecord(Guid ehrId, Guid patientId, Guid orgId)
        {
            _records.Add(new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                OrgId = orgId
            });
        }

        internal void SeedVersion(Guid ehrId, int versionNumber, string? ipfsCid)
        {
            _versions.Add(new EhrVersion
            {
                EhrId = ehrId,
                VersionId = Guid.NewGuid(),
                VersionNumber = versionNumber,
                IpfsCid = ipfsCid
            });
        }

        public Task<EhrRecord> CreateAsync(EhrRecord record)
        {
            if (record.EhrId == Guid.Empty)
            {
                record.EhrId = Guid.NewGuid();
            }

            _records.Add(record);
            return Task.FromResult(record);
        }

        public Task<EhrVersion> CreateVersionAsync(EhrVersion version)
        {
            if (version.VersionId == Guid.Empty)
            {
                version.VersionId = Guid.NewGuid();
            }

            _versions.Add(version);
            return Task.FromResult(version);
        }

        public Task<EhrFile> CreateFileAsync(EhrFile file)
        {
            if (file.FileId == Guid.Empty)
            {
                file.FileId = Guid.NewGuid();
            }

            _files.Add(file);
            return Task.FromResult(file);
        }

        public Task<EhrRecord> UpdateAsync(EhrRecord record) => Task.FromResult(record);

        public Task<EhrVersion> UpdateVersionAsync(EhrVersion version) => Task.FromResult(version);

        public Task<EhrRecord?> GetByIdAsync(Guid ehrId)
            => Task.FromResult<EhrRecord?>(_records.FirstOrDefault(r => r.EhrId == ehrId));

        public Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId)
        {
            var record = _records.FirstOrDefault(r => r.EhrId == ehrId);
            if (record != null)
            {
                record.Versions = _versions.Where(v => v.EhrId == ehrId).ToList();
                record.Files = _files.Where(f => f.EhrId == ehrId).ToList();
            }

            return Task.FromResult<EhrRecord?>(record);
        }

        public Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId)
            => Task.FromResult<IEnumerable<EhrRecord>>(_records.Where(r => r.PatientId == patientId).ToList());

        public Task<IEnumerable<EhrRecord>> GetByOrgIdAsync(Guid orgId)
            => Task.FromResult<IEnumerable<EhrRecord>>(_records.Where(r => r.OrgId == orgId).ToList());

        public Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId)
            => Task.FromResult<EhrVersion?>(_versions.Where(v => v.EhrId == ehrId).OrderByDescending(v => v.VersionNumber).FirstOrDefault());

        public Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId)
            => Task.FromResult<IEnumerable<EhrVersion>>(_versions.Where(v => v.EhrId == ehrId).ToList());

        public Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId)
            => Task.FromResult<IEnumerable<EhrFile>>(_files.Where(f => f.EhrId == ehrId).ToList());

        public Task<EhrVersion?> GetVersionByIdAsync(Guid ehrId, Guid versionId)
            => Task.FromResult<EhrVersion?>(_versions.FirstOrDefault(v => v.EhrId == ehrId && v.VersionId == versionId));

        public Task<EhrFile?> GetFileByIdAsync(Guid ehrId, Guid fileId)
            => Task.FromResult<EhrFile?>(_files.FirstOrDefault(f => f.EhrId == ehrId && f.FileId == fileId));

        public Task DeleteFileAsync(EhrFile file)
        {
            _files.Remove(file);
            return Task.CompletedTask;
        }
    }
}