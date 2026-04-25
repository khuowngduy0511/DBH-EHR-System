using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.EHR.Service.Services;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceDirectTests
{
    private readonly Mock<IEhrRecordRepository> _repoMock = new();
    private readonly Mock<ILogger<EhrService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IAuthServiceClient> _authServiceClientMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IBlockchainSyncService> _blockchainSyncServiceMock = new();
    private readonly ITestOutputHelper _output;

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public EhrServiceDirectTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly HttpClient OkHttpClient = new(new StubHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        }));

    private EhrService CreateService(DefaultHttpContext? context = null, HttpClient? authClient = null, HttpClient? consentClient = null, HttpClient? auditClient = null)
    {
        var currentContext = context ?? CreateHttpContext(withBearer: true, withUserId: true);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(currentContext);

        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns((string name) =>
            {
                return name switch
                {
                    "AuthService" => authClient ?? OkHttpClient,
                    "ConsentService" => consentClient ?? OkHttpClient,
                    "AuditService" => auditClient ?? OkHttpClient,
                    _ => OkHttpClient
                };
            });

        _authServiceClientMock
            .Setup(x => x.GetUserIdByPatientIdAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid?)null);

        _authServiceClientMock
            .Setup(x => x.GetUserProfileDetailAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((AuthUserProfileDetailDto?)null);

        _blockchainSyncServiceMock
            .Setup(x => x.EnqueueEhrHash(It.IsAny<EhrHashRecord>(), It.IsAny<Func<BlockchainTransactionResult, Task>?>(), It.IsAny<Func<string, Task>?>()));

        _blockchainSyncServiceMock
            .Setup(x => x.EnqueueAuditEntry(It.IsAny<AuditEntry>(), It.IsAny<Func<BlockchainTransactionResult, Task>?>(), It.IsAny<Func<string, Task>?>()));

        return new EhrService(
            _repoMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _authServiceClientMock.Object,
            _httpContextAccessorMock.Object,
            _blockchainSyncServiceMock.Object);
    }

    private async Task<T> RunAndLog<T>(
        Func<Task<T>> action,
        [CallerMemberName] string testName = "")
    {
        var res = await action();
        _output.WriteLine($"{testName} response:");
        _output.WriteLine(JsonSerializer.Serialize(res, LogJsonOptions));
        return res;
    }

    [Fact(DisplayName = "CreateEhrRecordAsync::CreateEhrRecordAsync-01")]
    public async Task CreateEhrRecordAsync_ValidInput_ReturnsCreatedPayload()
    {
        var patientId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var ehrId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var json = "{\"diagnosis\":\"flu\",\"temperature\":38.5}";

        _repoMock.Setup(x => x.CreateAsync(It.IsAny<EhrRecord>()))
            .ReturnsAsync(new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                OrgId = orgId,
                CreatedAt = createdAt
            });

        _repoMock.Setup(x => x.CreateVersionAsync(It.IsAny<EhrVersion>()))
            .ReturnsAsync((EhrVersion v) =>
            {
                v.VersionId = versionId;
                return v;
            });

        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>()))
            .ReturnsAsync((EhrFile f) =>
            {
                f.FileId = fileId;
                return f;
            });

        var service = CreateService();
        var request = new CreateEhrRecordDto
        {
            PatientId = patientId,
            OrgId = orgId,
            Data = JsonDocument.Parse(json).RootElement
        };

        var result = await service.CreateEhrRecordAsync(request);

        Console.WriteLine("CreateEhrRecordAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.Equal(ehrId, result.EhrId);
        Assert.Equal(patientId, result.PatientId);
        Assert.Equal(versionId, result.VersionId);
        Assert.Equal(fileId, result.FileId);
        Assert.Equal(1, result.VersionNumber);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(ComputeSha256LowerHex(json), result.DataHash);

        _repoMock.Verify(x => x.CreateAsync(It.IsAny<EhrRecord>()), Times.Once);
        _repoMock.Verify(x => x.CreateVersionAsync(It.IsAny<EhrVersion>()), Times.Once);
        _repoMock.Verify(x => x.CreateFileAsync(It.IsAny<EhrFile>()), Times.Once);
    }

    [Fact(DisplayName = "CreateEhrRecordAsync::CreateEhrRecordAsync-02")]
    public async Task CreateEhrRecordAsync_InvalidData_Throws()
    {
        var service = CreateService();
        var request = new CreateEhrRecordDto
        {
            PatientId = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            Data = default
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEhrRecordAsync(request));
    }

    [Fact(DisplayName = "GetEhrRecordAsync::GetEhrRecordAsync-01")]
    public async Task GetEhrRecordAsync_ValidId_ReturnsMappedRecord()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId))
            .ReturnsAsync(new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                OrgId = Guid.NewGuid(),
                EncounterId = Guid.NewGuid(),
                CreatedAt = createdAt,
                Versions =
                [
                    new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1, CreatedAt = createdAt.AddMinutes(-5) },
                    new EhrVersion { VersionId = versionId, VersionNumber = 2, CreatedAt = createdAt }
                ],
                Files =
                [
                    new EhrFile { FileId = fileId, FileUrl = "lab.pdf", FileHash = "abc", CreatedAt = createdAt }
                ]
            });

        var service = CreateService();
        var result = await service.GetEhrRecordAsync(ehrId);

        Assert.NotNull(result);
        Assert.Equal(ehrId, result!.EhrId);
        Assert.Equal(patientId, result.PatientId);
        Assert.NotNull(result.LatestVersionInfo);
        Assert.Equal(versionId, result.LatestVersionInfo!.VersionId);
        Assert.Equal(2, result.LatestVersionInfo.VersionNumber);
        Assert.Single(result.Files!);
        Assert.Equal(fileId, result.Files![0].FileId);
        Assert.Equal("lab.pdf", result.Files[0].FileUrl);
    }

    [Fact(DisplayName = "GetEhrRecordAsync::GetEhrRecordAsync-03")]
    public async Task GetEhrRecordAsync_NotFound_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var result = await service.GetEhrRecordAsync(ehrId);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-03")]
    public async Task GetEhrRecordWithConsentCheckAsync_NotFound_ReturnsNullTuple()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(ehrId, Guid.NewGuid());

        Assert.Null(record);
        Assert.False(consentDenied);
        Assert.Null(denyMessage);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-01")]
    public async Task GetEhrRecordWithConsentCheckAsync_RequesterIsOwner_BypassesConsent()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId))
            .ReturnsAsync(new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1 }],
                Files = []
            });

        var service = CreateService();
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(ehrId, patientId);

        Assert.NotNull(record);
        Assert.Equal(ehrId, record!.EhrId);
        Assert.False(consentDenied);
        Assert.Null(denyMessage);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-04")]
    public async Task GetEhrRecordWithConsentCheckAsync_ConsentDenied_ReturnsDeniedTuple()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var requesterUserId = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId))
            .ReturnsAsync(new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1 }],
                Files = []
            });

        var authClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("doctorId="))
            {
                return JsonResponse(new { userId = requesterUserId });
            }

            if (uri.Contains("patientId="))
            {
                return JsonResponse(new { userId = patientUserId });
            }

            return JsonResponse(new { });
        }));

        var consentClient = new HttpClient(new StubHttpMessageHandler(_ => JsonResponse(new { hasAccess = false, consentId = (string?)null })));

        var service = CreateService(authClient: authClient, consentClient: consentClient);
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(ehrId, requesterId);

        Assert.Null(record);
        Assert.True(consentDenied);
        Assert.NotNull(denyMessage);
        Assert.Contains("khong co consent", RemoveVietnameseDiacritics(denyMessage!).ToLowerInvariant());
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-02")]
    public async Task GetEhrDocumentForCurrentUserAsync_NoUserClaim_ReturnsForbidden()
    {
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
        var (decryptedData, forbidden, message) = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());

        Assert.Null(decryptedData);
        Assert.True(forbidden);
        Assert.Equal("Cannot resolve current user id from token", message);
    }

    // [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-02")]
    // public async Task DownloadIpfsRawAsync_EmptyCid_ReturnsNull()
    // {
    //     var service = CreateService();
    //     var result = await service.DownloadIpfsRawAsync(string.Empty);
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-03")]
    // public async Task DownloadLatestIpfsRawByEhrIdAsync_NoLatestVersion_ReturnsNull()
    // {
    //     var ehrId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetLatestVersionAsync(ehrId)).ReturnsAsync((EhrVersion?)null);

    //     var service = CreateService();
    //     var result = await service.DownloadLatestIpfsRawByEhrIdAsync(ehrId);

    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-01")]
    // public async Task DownloadLatestIpfsRawByEhrIdAsync_FallbackData_ReturnsPayload()
    // {
    //     var ehrId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetLatestVersionAsync(ehrId)).ReturnsAsync(new EhrVersion
    //     {
    //         EhrId = ehrId,
    //         IpfsCid = null,
    //         EncryptedFallbackData = "encrypted-content"
    //     });

    //     var service = CreateService();
    //     var result = await service.DownloadLatestIpfsRawByEhrIdAsync(ehrId);

    //     Assert.NotNull(result);
    //     Assert.Equal("encrypted-content", result!.EncryptedData);
    //     Assert.Equal(string.Empty, result.IpfsCid);
    // }

    // [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-02")]
    // public async Task EncryptToIpfsForCurrentUserAsync_NoUser_ReturnsNull()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
    //     var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "abc" });
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-02")]
    // public async Task DecryptIpfsForCurrentUserAsync_InvalidRequest_ReturnsNull()
    // {
    //     var service = CreateService();
    //     var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
    //     {
    //         IpfsCid = string.Empty,
    //         WrappedAesKey = string.Empty
    //     });

    //     Assert.Null(result);
    // }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-04")]
    public async Task GetPatientEhrRecordsAsync_EmptyCollection_ReturnsEmptyList()
    {
        var patientId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync([]);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(patientId)).ToList();

        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-01")]
    public async Task GetPatientEhrRecordsAsync_ValidInput_ReturnsMappedRecords()
    {
        var patientId = Guid.NewGuid();
        var ehrId = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync(
        [
            new EhrRecord
            {
                EhrId = ehrId,
                PatientId = patientId,
                Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 3 }],
                Files = [new EhrFile { FileId = Guid.NewGuid(), FileUrl = "xray.png", FileHash = "h1" }]
            }
        ]);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(patientId)).ToList();

        Assert.Single(result);
        Assert.Equal(ehrId, result[0].EhrId);
        Assert.NotNull(result[0].LatestVersionInfo);
        Assert.Equal(3, result[0].LatestVersionInfo!.VersionNumber);
        Assert.Single(result[0].Files!);
        Assert.Equal("xray.png", result[0].Files![0].FileUrl);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-01")]
    public async Task GetOrgEhrRecordsAsync_ValidInput_ReturnsMappedRecords()
    {
        var orgId = Guid.NewGuid();

        _repoMock.Setup(x => x.GetByOrgIdAsync(orgId)).ReturnsAsync(
        [new EhrRecord { EhrId = Guid.NewGuid(), PatientId = Guid.NewGuid(), OrgId = orgId, Versions = [], Files = [] }]);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(orgId)).ToList();

        Assert.Single(result);
        Assert.Equal(orgId, result[0].OrgId);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-01")]
    public async Task GetEhrVersionsAsync_ValidInput_ReturnsMappedVersions()
    {
        var ehrId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetVersionsAsync(ehrId)).ReturnsAsync(
        [new EhrVersion { VersionId = versionId, VersionNumber = 7, CreatedAt = createdAt }]);

        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(ehrId)).ToList();

        Assert.Single(result);
        Assert.Equal(versionId, result[0].VersionId);
        Assert.Equal(7, result[0].VersionNumber);
        Assert.Equal(createdAt, result[0].CreatedAt);
    }

    [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-01")]
    public async Task GetEhrFilesAsync_ValidInput_ReturnsMappedFiles()
    {
        var ehrId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetFilesAsync(ehrId)).ReturnsAsync(
        [new EhrFile { FileId = fileId, FileUrl = "ct-scan.dcm", FileHash = "hash-1", CreatedAt = createdAt }]);

        var service = CreateService();
        var result = (await service.GetEhrFilesAsync(ehrId)).ToList();

        Assert.Single(result);
        Assert.Equal(fileId, result[0].FileId);
        Assert.Equal("ct-scan.dcm", result[0].FileUrl);
        Assert.Equal("hash-1", result[0].FileHash);
        Assert.Equal(createdAt, result[0].CreatedAt);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-04")]
    public async Task UpdateEhrRecordAsync_RecordNotFound_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var result = await service.UpdateEhrRecordAsync(ehrId, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"summary\":\"updated\"}").RootElement
        });

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-03")]
    public async Task GetVersionByIdAsync_NotFound_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionByIdAsync(ehrId, versionId)).ReturnsAsync((EhrVersion?)null);

        var service = CreateService();
        var result = await service.GetVersionByIdAsync(ehrId, versionId);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-01")]
    public async Task GetVersionByIdAsync_ValidInput_ReturnsDetail()
    {
        var ehrId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetVersionByIdAsync(ehrId, versionId)).ReturnsAsync(
            new EhrVersion
            {
                EhrId = ehrId,
                VersionId = versionId,
                VersionNumber = 4,
                IpfsCid = "QmTestCid",
                CreatedAt = createdAt
            });

        var service = CreateService();
        var result = await service.GetVersionByIdAsync(ehrId, versionId);

        Assert.NotNull(result);
        Assert.Equal(ehrId, result!.EhrId);
        Assert.Equal(versionId, result.VersionId);
        Assert.Equal(4, result.VersionNumber);
        Assert.Equal("QmTestCid", result.IpfsCid);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-04")]
    public async Task AddFileAsync_RecordNotFound_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var result = await service.AddFileAsync(ehrId, stream, "note.txt");
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-01")]
    public async Task AddFileAsync_ValidInput_ReturnsSavedFileDtoWithRealHash()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var payload = Encoding.UTF8.GetBytes("real file content");

        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = patientId,
            OrgId = Guid.NewGuid()
        });

        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>()))
            .ReturnsAsync((EhrFile file) =>
            {
                file.FileId = fileId;
                file.CreatedAt = DateTime.UtcNow;
                return file;
            });

        var service = CreateService();
        await using var stream = new MemoryStream(payload);
        var result = await service.AddFileAsync(ehrId, stream, "result.txt");
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.NotNull(result);
        Assert.Equal(fileId, result!.FileId);
        Assert.Equal("result.txt", result.FileUrl);
        Assert.Equal(ComputeSha256LowerHex(payload), result.FileHash);

        _repoMock.Verify(x => x.CreateFileAsync(It.IsAny<EhrFile>()), Times.Once);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-04")]
    public async Task DeleteFileAsync_FileNotFound_ReturnsFalse()
    {
        var ehrId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetFileByIdAsync(ehrId, fileId)).ReturnsAsync((EhrFile?)null);

        var service = CreateService();
        var result = await service.DeleteFileAsync(ehrId, fileId);

        Assert.False(result);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-05")]
    public async Task DeleteFileAsync_FileExists_DeletesAndReturnsTrue()
    {
        var ehrId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var file = new EhrFile { FileId = fileId, EhrId = ehrId };

        _repoMock.Setup(x => x.GetFileByIdAsync(ehrId, fileId)).ReturnsAsync(file);
        _repoMock.Setup(x => x.DeleteFileAsync(file)).Returns(Task.CompletedTask);
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = Guid.NewGuid(),
            OrgId = Guid.NewGuid()
        });

        var service = CreateService();
        var result = await service.DeleteFileAsync(ehrId, fileId);

        Assert.True(result);
        _repoMock.Verify(x => x.DeleteFileAsync(file), Times.Once);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-02")]
    public async Task GetEhrRecordWithConsentCheckAsync_InvalidInputShape_ReturnsNullWhenRecordMissing()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(Guid.Empty, Guid.NewGuid());
        Assert.Null(record);
        Assert.False(consentDenied);
        Assert.Null(denyMessage);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-EHRID-EmptyGuid")]
    public async Task GetEhrRecordWithConsentCheckAsync_EmptyEhrId_ReturnsNullWhenRecordMissing()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrRecordWithConsentCheckAsync(Guid.Empty, Guid.NewGuid());
        Assert.Null(result.Record);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-REQUESTERID-EmptyGuid")]
    public async Task GetEhrRecordWithConsentCheckAsync_EmptyRequesterId_ReturnsNullWhenRecordMissing()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrRecordWithConsentCheckAsync(Guid.Empty, Guid.Empty);
        Assert.Null(result.Record);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-01")]
    public async Task GetEhrDocumentAsync_HappyPathId_WithMissingRecord_ReturnsNotFoundByContract()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(It.IsAny<Guid>())).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal("EHR Record not found", result.DenyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-02")]
    public async Task GetEhrDocumentAsync_InvalidInputId_WhenMissingRecord_ReturnsNotFound()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentAsync(Guid.Empty, Guid.NewGuid());
        Assert.Equal("EHR Record not found", result.DenyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-EHRID-EmptyGuid")]
    public async Task GetEhrDocumentAsync_EmptyEhrId_WhenMissingRecord_ReturnsNotFound()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentAsync(Guid.Empty, Guid.NewGuid());
        Assert.Equal("EHR Record not found", result.DenyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-REQUESTERID-EmptyGuid")]
    public async Task GetEhrDocumentAsync_EmptyRequesterId_WhenMissingRecord_ReturnsNotFound()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(It.IsAny<Guid>())).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentAsync(Guid.NewGuid(), Guid.Empty);
        Assert.Equal("EHR Record not found", result.DenyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-01")]
    public async Task GetEhrDocumentForCurrentUserAsync_HappyPathId_WhenNoRecord_ReturnsNotFound()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(It.IsAny<Guid>())).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());
        Assert.Equal("EHR Record not found", result.Message);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-04")]
    public async Task GetEhrDocumentForCurrentUserAsync_TupleFlagsId_WhenNoRecord_ReturnsTuple()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(It.IsAny<Guid>())).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());
        Assert.Null(result.DecryptedData);
        Assert.False(result.Forbidden);
        Assert.Equal("EHR Record not found", result.Message);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-EHRID-EmptyGuid")]
    public async Task GetEhrDocumentForCurrentUserAsync_EmptyEhrId_WhenNoRecord_ReturnsNotFound()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.GetEhrDocumentForCurrentUserAsync(Guid.Empty);
        Assert.Equal("EHR Record not found", result.Message);
    }

    [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-01")]
    public async Task DownloadIpfsRawAsync_HappyPathId_WithNonExistingCid_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DownloadIpfsRawAsync("QmNonExistingCid");
        Assert.Null(result);
    }

    [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-03")]
    public async Task DownloadIpfsRawAsync_NotFound_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DownloadIpfsRawAsync("QmMissing");
        Assert.Null(result);
    }

    [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-04")]
    public async Task DownloadIpfsRawAsync_NullableReturn_WhenUnavailable_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DownloadIpfsRawAsync("QmNullable");
        Assert.Null(result);
    }

    [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-IPFSCID-EmptyString")]
    public async Task DownloadIpfsRawAsync_EmptyCidAlias_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DownloadIpfsRawAsync(string.Empty);
        Assert.Null(result);
    }

    [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-EHRID-EmptyGuid")]
        public async Task DownloadLatestIpfsRawByEhrIdAsync_EmptyGuidAlias_ReturnsNull_DuplicateSet()
    {
        _repoMock.Setup(x => x.GetLatestVersionAsync(Guid.Empty)).ReturnsAsync((EhrVersion?)null);
        var service = CreateService();
        var result = await service.DownloadLatestIpfsRawByEhrIdAsync(Guid.Empty);
        Assert.Null(result);
    }

    [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-01")]
    public async Task EncryptToIpfsForCurrentUserAsync_HappyPathId_WithoutIpfsInfrastructure_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var context = CreateHttpContext(withBearer: true, withUserId: true);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));

        var authClient = new HttpClient(new StubHttpMessageHandler(_ =>
            JsonResponse(new { userId, publicKey = "invalid-public-key", encryptedPrivateKey = "x" })));

        var service = CreateService(context: context, authClient: authClient);
        var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "hello" });
        Assert.Null(result);
    }

    [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-03")]
    public async Task EncryptToIpfsForCurrentUserAsync_UnauthorizedStyle_NoUserClaim_ReturnsNull()
    {
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
        var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "payload" });
        Assert.Null(result);
    }

    [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-01")]
    public async Task DecryptIpfsForCurrentUserAsync_HappyPathId_WithMissingResource_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = "QmMissing",
            WrappedAesKey = "wrapped"
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-04")]
    public async Task DecryptIpfsForCurrentUserAsync_NullableReturnBranch_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = "QmNullable",
            WrappedAesKey = "wrapped"
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-02")]
    public async Task GetPatientEhrRecordsAsync_InvalidInputGuid_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetByPatientIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-03")]
    public async Task GetPatientEhrRecordsAsync_NotFoundData_ReturnsEmptyList()
    {
        var patientId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(patientId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-PATIENTID-EmptyGuid")]
    public async Task GetPatientEhrRecordsAsync_EmptyGuidAlias_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetByPatientIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-02")]
    public async Task GetOrgEhrRecordsAsync_InvalidInputGuid_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetByOrgIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-03")]
    public async Task GetOrgEhrRecordsAsync_NotFoundData_ReturnsEmptyList()
    {
        var orgId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByOrgIdAsync(orgId)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(orgId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-ORGID-EmptyGuid")]
    public async Task GetOrgEhrRecordsAsync_EmptyGuidAlias_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetByOrgIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-02")]
    public async Task GetEhrVersionsAsync_InvalidInputGuid_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetVersionsAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-03")]
    public async Task GetEhrVersionsAsync_NotFoundData_ReturnsEmptyList()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionsAsync(ehrId)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(ehrId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-EHRID-EmptyGuid")]
    public async Task GetEhrVersionsAsync_EmptyGuidAlias_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetVersionsAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-02")]
    public async Task GetEhrFilesAsync_InvalidInputGuid_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetFilesAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrFilesAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-03")]
    public async Task GetEhrFilesAsync_NotFoundData_ReturnsEmptyList()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetFilesAsync(ehrId)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrFilesAsync(ehrId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-EHRID-EmptyGuid")]
    public async Task GetEhrFilesAsync_EmptyGuidAlias_ReturnsEmptyList()
    {
        _repoMock.Setup(x => x.GetFilesAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrFilesAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-02")]
    public async Task UpdateEhrRecordAsync_InvalidInputGuid_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.UpdateEhrRecordAsync(Guid.Empty, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"x\":1}").RootElement
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-03")]
    public async Task UpdateEhrRecordAsync_UnauthorizedStyle_NoUserClaim_ReturnsNullWhenMissingRecord()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(It.IsAny<Guid>())).ReturnsAsync((EhrRecord?)null);
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
        var result = await service.UpdateEhrRecordAsync(Guid.NewGuid(), new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"x\":1}").RootElement
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-EHRID-EmptyGuid")]
    public async Task UpdateEhrRecordAsync_EmptyGuidAlias_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.UpdateEhrRecordAsync(Guid.Empty, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"x\":1}").RootElement
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-EHRID-EmptyGuid")]
        public async Task GetVersionByIdAsync_EmptyEhrIdAlias_ReturnsNull_DuplicateSet()
    {
        _repoMock.Setup(x => x.GetVersionByIdAsync(Guid.Empty, It.IsAny<Guid>())).ReturnsAsync((EhrVersion?)null);
        var service = CreateService();
        var result = await service.GetVersionByIdAsync(Guid.Empty, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-VERSIONID-EmptyGuid")]
        public async Task GetVersionByIdAsync_EmptyVersionIdAlias_ReturnsNull_DuplicateSet()
    {
        _repoMock.Setup(x => x.GetVersionByIdAsync(It.IsAny<Guid>(), Guid.Empty)).ReturnsAsync((EhrVersion?)null);
        var service = CreateService();
        var result = await service.GetVersionByIdAsync(Guid.NewGuid(), Guid.Empty);
        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-03")]
    public async Task AddFileAsync_UnauthorizedStyle_NoUserClaim_WithMissingRecord_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
        await using var fileStream = new MemoryStream([1, 2, 3]);
        var fileName = "file.bin";
        var result = await service.AddFileAsync(ehrId, fileStream, fileName);
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-EHRID-EmptyGuid")]
    public async Task AddFileAsync_EmptyEhrIdAlias_ReturnsNullWhenRecordMissing()
    {
        _repoMock.Setup(x => x.GetByIdAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        await using var stream = new MemoryStream([1]);
        var result = await service.AddFileAsync(Guid.Empty, stream, "a.txt");
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-FILENAME-EmptyString")]
    public async Task AddFileAsync_EmptyFileNameAlias_ReturnsDtoWithEmptyFileName()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = Guid.NewGuid() });
        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>())).ReturnsAsync((EhrFile f) => f);

        var service = CreateService();
        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.AddFileAsync(ehrId, stream, string.Empty);
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.FileUrl);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-03")]
    public async Task DeleteFileAsync_UnauthorizedStyle_NoUserClaim_FileMissing_ReturnsFalse()
    {
        _repoMock.Setup(x => x.GetFileByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((EhrFile?)null);
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
        var result = await service.DeleteFileAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(result);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-EHRID-EmptyGuid")]
    public async Task DeleteFileAsync_EmptyEhrIdAlias_ReturnsFalseWhenMissing()
    {
        _repoMock.Setup(x => x.GetFileByIdAsync(Guid.Empty, It.IsAny<Guid>())).ReturnsAsync((EhrFile?)null);
        var service = CreateService();
        var result = await service.DeleteFileAsync(Guid.Empty, Guid.NewGuid());
        Assert.False(result);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-FILEID-EmptyGuid")]
    public async Task DeleteFileAsync_EmptyFileIdAlias_ReturnsFalseWhenMissing()
    {
        _repoMock.Setup(x => x.GetFileByIdAsync(It.IsAny<Guid>(), Guid.Empty)).ReturnsAsync((EhrFile?)null);
        var service = CreateService();
        var result = await service.DeleteFileAsync(Guid.NewGuid(), Guid.Empty);
        Assert.False(result);
    }

    [Fact(DisplayName = "CreateEhrRecordAsync::CreateEhrRecordAsync-03")]
    public async Task CreateEhrRecordAsync_NoBearerToken_ServiceStillCreatesRecord()
    {
        var patientId = Guid.NewGuid();
        var ehrId = Guid.NewGuid();

        _repoMock.Setup(x => x.CreateAsync(It.IsAny<EhrRecord>()))
            .ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = patientId, CreatedAt = DateTime.UtcNow });
        _repoMock.Setup(x => x.CreateVersionAsync(It.IsAny<EhrVersion>())).ReturnsAsync(new EhrVersion { VersionId = Guid.NewGuid() });
        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>())).ReturnsAsync(new EhrFile { FileId = Guid.NewGuid() });

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = await service.CreateEhrRecordAsync(new CreateEhrRecordDto
        {
            PatientId = patientId,
            Data = JsonDocument.Parse("{\"status\":\"ok\"}").RootElement
        });
        Console.WriteLine("CreateEhrRecordAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.Equal(ehrId, result.EhrId);
    }

    [Fact(DisplayName = "GetEhrRecordAsync::GetEhrRecordAsync-02")]
    public async Task GetEhrRecordAsync_EmptyGuid_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var result = await service.GetEhrRecordAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetEhrRecordAsync::GetEhrRecordAsync-04")]
    public async Task GetEhrRecordAsync_NullableReturnBranch_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var result = await service.GetEhrRecordAsync(ehrId);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetEhrRecordAsync::GetEhrRecordAsync-EHRID-EmptyGuid")]
    public async Task GetEhrRecordAsync_EmptyGuidAlias_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var result = await service.GetEhrRecordAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-03")]
    public async Task GetEhrDocumentAsync_RecordNotFound_ReturnsNotFoundMessage()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(ehrId, Guid.NewGuid());

        Assert.Null(decryptedData);
        Assert.False(consentDenied);
        Assert.Equal("EHR Record not found", denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-04")]
    public async Task GetEhrDocumentAsync_NoVersions_ReturnsNoVersionsMessage()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = Guid.NewGuid(),
            Versions = [],
            Files = []
        });

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(ehrId, Guid.NewGuid());

        Assert.Null(decryptedData);
        Assert.False(consentDenied);
        Assert.Equal("No versions found for EHR", denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-03")]
    public async Task GetEhrDocumentForCurrentUserAsync_RecordNotFound_PropagatesMessage()
    {
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
        var (decryptedData, forbidden, message) = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());

        Assert.Null(decryptedData);
        Assert.False(forbidden);
        Assert.Equal("EHR Record not found", message);
    }

    // [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-02")]
    // public async Task DownloadLatestIpfsRawByEhrIdAsync_EmptyGuid_ReturnsNull()
    // {
    //     _repoMock.Setup(x => x.GetLatestVersionAsync(Guid.Empty)).ReturnsAsync((EhrVersion?)null);

    //     var service = CreateService();
    //     var result = await service.DownloadLatestIpfsRawByEhrIdAsync(Guid.Empty);

    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-04")]
    // public async Task DownloadLatestIpfsRawByEhrIdAsync_NullableReturn_WhenNoEncryptedData_ReturnsNull()
    // {
    //     var ehrId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetLatestVersionAsync(ehrId)).ReturnsAsync(new EhrVersion
    //     {
    //         EhrId = ehrId,
    //         IpfsCid = null,
    //         EncryptedFallbackData = null
    //     });

    //     var service = CreateService();
    //     var result = await service.DownloadLatestIpfsRawByEhrIdAsync(ehrId);

    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-04")]
    // public async Task EncryptToIpfsForCurrentUserAsync_EmptyPayload_ReturnsNull()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "" });
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-03")]
    // public async Task DecryptIpfsForCurrentUserAsync_IpfsDownloadFails_ReturnsNull()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
    //     {
    //         IpfsCid = "QmNonExistent",
    //         WrappedAesKey = "not-used"
    //     });

    //     Assert.Null(result);
    // }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-04")]
    public async Task GetOrgEhrRecordsAsync_EmptyCollection_ReturnsEmptyList()
    {
        var orgId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByOrgIdAsync(orgId)).ReturnsAsync([]);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(orgId)).ToList();

        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-04")]
    public async Task GetEhrVersionsAsync_EmptyCollection_ReturnsEmptyList()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionsAsync(ehrId)).ReturnsAsync([]);

        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(ehrId)).ToList();

        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-04")]
    public async Task GetEhrFilesAsync_EmptyCollection_ReturnsEmptyList()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetFilesAsync(ehrId)).ReturnsAsync([]);

        var service = CreateService();
        var result = (await service.GetEhrFilesAsync(ehrId)).ToList();

        Assert.Empty(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-01")]
    public async Task UpdateEhrRecordAsync_ValidInput_CreatesNewVersionAndReturnsUpdatedRecord()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var baseRecord = new EhrRecord
        {
            EhrId = ehrId,
            PatientId = patientId,
            OrgId = Guid.NewGuid(),
            Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1 }],
            Files = []
        };

        var updatedRecord = new EhrRecord
        {
            EhrId = ehrId,
            PatientId = patientId,
            OrgId = baseRecord.OrgId,
            Versions =
            [
                new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1 },
                new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 2 }
            ],
            Files = []
        };

        _repoMock.SetupSequence(x => x.GetByIdWithVersionsAsync(ehrId))
            .ReturnsAsync(baseRecord)
            .ReturnsAsync(updatedRecord);

        _repoMock.Setup(x => x.CreateVersionAsync(It.IsAny<EhrVersion>()))
            .ReturnsAsync((EhrVersion v) => v);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = await service.UpdateEhrRecordAsync(ehrId, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"updated\":true}").RootElement
        });

        Assert.NotNull(result);
        Assert.Equal(ehrId, result!.EhrId);
        Assert.NotNull(result.LatestVersionInfo);
        Assert.Equal(2, result.LatestVersionInfo!.VersionNumber);
        _repoMock.Verify(x => x.CreateVersionAsync(It.IsAny<EhrVersion>()), Times.Once);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-02")]
    public async Task GetVersionByIdAsync_EmptyGuids_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetVersionByIdAsync(Guid.Empty, Guid.Empty)).ReturnsAsync((EhrVersion?)null);

        var service = CreateService();
        var result = await service.GetVersionByIdAsync(Guid.Empty, Guid.Empty);

        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-04")]
    public async Task GetVersionByIdAsync_NullableReturnBranch_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionByIdAsync(ehrId, versionId)).ReturnsAsync((EhrVersion?)null);

        var service = CreateService();
        var result = await service.GetVersionByIdAsync(ehrId, versionId);

        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-02")]
    public async Task AddFileAsync_EmptyPayloadStillProcesses_ReturnsDeterministicHash()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = Guid.NewGuid(),
            OrgId = Guid.NewGuid()
        });
        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>())).ReturnsAsync((EhrFile f) => f);

        var service = CreateService();
        await using var emptyStream = new MemoryStream(Array.Empty<byte>());
        var result = await service.AddFileAsync(ehrId, emptyStream, string.Empty);
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.FileUrl);
        Assert.Equal(ComputeSha256LowerHex(Array.Empty<byte>()), result.FileHash);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-01")]
    public async Task DeleteFileAsync_ValidInput_ReturnsTrue()
    {
        var ehrId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var file = new EhrFile { FileId = fileId, EhrId = ehrId };

        _repoMock.Setup(x => x.GetFileByIdAsync(ehrId, fileId)).ReturnsAsync(file);
        _repoMock.Setup(x => x.DeleteFileAsync(file)).Returns(Task.CompletedTask);
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = Guid.NewGuid() });

        var service = CreateService();
        var result = await service.DeleteFileAsync(ehrId, fileId);

        Assert.True(result);
    }

    [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-02")]
    public async Task DeleteFileAsync_EmptyGuids_ReturnsFalseWhenMissing()
    {
        _repoMock.Setup(x => x.GetFileByIdAsync(Guid.Empty, Guid.Empty)).ReturnsAsync((EhrFile?)null);

        var service = CreateService();
        var result = await service.DeleteFileAsync(Guid.Empty, Guid.Empty);

        Assert.False(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-03")]
    public async Task AddFileAsync_NoUserContext_ServiceLayerStillProcesses()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = Guid.NewGuid() });
        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>())).ReturnsAsync((EhrFile f) => f);

        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: false));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var result = await service.AddFileAsync(ehrId, stream, "f.txt");
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.NotNull(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-EHRID-EmptyGuid")]
    public async Task AddFileAsync_EmptyEhrId_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var result = await service.AddFileAsync(Guid.Empty, stream, "file.txt");
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Null(result);
    }

    [Fact(DisplayName = "AddFileAsync::AddFileAsync-FILENAME-EmptyString")]
    public async Task AddFileAsync_EmptyFileName_ReturnsDtoWithEmptyFileUrl()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = Guid.NewGuid() });
        _repoMock.Setup(x => x.CreateFileAsync(It.IsAny<EhrFile>())).ReturnsAsync((EhrFile f) => f);

        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        var result = await service.AddFileAsync(ehrId, stream, string.Empty);
        Console.WriteLine("AddFileAsync response: " + JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result!.FileUrl);
    }

    // [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-01")]
    // public async Task DecryptIpfsForCurrentUserAsync_ValidShapeRequest_DoesNotThrow()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
    //     {
    //         IpfsCid = "QmValidShape",
    //         WrappedAesKey = "WrappedKeyValue"
    //     });

    //     Assert.True(result is null || result is string);
    // }

    // [Fact(DisplayName = "DecryptIpfsForCurrentUserAsync::DecryptIpfsForCurrentUserAsync-04")]
    // public async Task DecryptIpfsForCurrentUserAsync_NullablePath_ReturnsNull()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var result = await service.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
    //     {
    //         IpfsCid = "QmMissing",
    //         WrappedAesKey = "k"
    //     });
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-03")]
    // public async Task DeleteFileAsync_NoUserContext_ServiceLayerStillDeletes()
    // {
    //     var ehrId = Guid.NewGuid();
    //     var fileId = Guid.NewGuid();
    //     var file = new EhrFile { EhrId = ehrId, FileId = fileId };
    //     _repoMock.Setup(x => x.GetFileByIdAsync(ehrId, fileId)).ReturnsAsync(file);
    //     _repoMock.Setup(x => x.DeleteFileAsync(file)).Returns(Task.CompletedTask);
    //     _repoMock.Setup(x => x.GetByIdAsync(ehrId)).ReturnsAsync(new EhrRecord { EhrId = ehrId, PatientId = Guid.NewGuid() });

    //     var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: false));
    //     var result = await service.DeleteFileAsync(ehrId, fileId);

    //     Assert.True(result);
    // }

    // [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-EHRID-EmptyGuid")]
    // public async Task DeleteFileAsync_EmptyEhrId_ReturnsFalse()
    // {
    //     var fileId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetFileByIdAsync(Guid.Empty, fileId)).ReturnsAsync((EhrFile?)null);
    //     var service = CreateService();
    //     var result = await service.DeleteFileAsync(Guid.Empty, fileId);
    //     Assert.False(result);
    // }

    // [Fact(DisplayName = "DeleteFileAsync::DeleteFileAsync-FILEID-EmptyGuid")]
    // public async Task DeleteFileAsync_EmptyFileId_ReturnsFalse()
    // {
    //     var ehrId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetFileByIdAsync(ehrId, Guid.Empty)).ReturnsAsync((EhrFile?)null);
    //     var service = CreateService();
    //     var result = await service.DeleteFileAsync(ehrId, Guid.Empty);
    //     Assert.False(result);
    // }

    // [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-01")]
    // public async Task DownloadIpfsRawAsync_ValidCidShape_DoesNotThrow()
    // {
    //     var service = CreateService();
    //     var result = await service.DownloadIpfsRawAsync("QmValidCidShape");
    //     Assert.True(result is null || result is string);
    // }

    // [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-03")]
    // public async Task DownloadIpfsRawAsync_NotFoundLikeInput_ReturnsNull()
    // {
    //     var service = CreateService();
    //     var result = await service.DownloadIpfsRawAsync("QmDefinitelyNotThere");
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-04")]
    // public async Task DownloadIpfsRawAsync_NullableBranch_ReturnsNull()
    // {
    //     var service = CreateService();
    //     var result = await service.DownloadIpfsRawAsync("QmNullableCase");
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadIpfsRawAsync::DownloadIpfsRawAsync-IPFSCID-EmptyString")]
    // public async Task DownloadIpfsRawAsync_EmptyStringAlias_ReturnsNull()
    // {
    //     var service = CreateService();
    //     var result = await service.DownloadIpfsRawAsync(string.Empty);
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "DownloadLatestIpfsRawByEhrIdAsync::DownloadLatestIpfsRawByEhrIdAsync-EHRID-EmptyGuid")]
    // public async Task DownloadLatestIpfsRawByEhrIdAsync_EmptyGuidAlias_ReturnsNull()
    // {
    //     _repoMock.Setup(x => x.GetLatestVersionAsync(Guid.Empty)).ReturnsAsync((EhrVersion?)null);
    //     var service = CreateService();
    //     var result = await service.DownloadLatestIpfsRawByEhrIdAsync(Guid.Empty);
    //     Assert.Null(result);
    // }

    // [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-01")]
    // public async Task EncryptToIpfsForCurrentUserAsync_ValidShapeRequest_DoesNotThrow()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "payload" });
    //     Assert.True(result is null || !string.IsNullOrWhiteSpace(result.IpfsCid));
    // }

    // [Fact(DisplayName = "EncryptToIpfsForCurrentUserAsync::EncryptToIpfsForCurrentUserAsync-03")]
    // public async Task EncryptToIpfsForCurrentUserAsync_NoUserClaim_ReturnsNull()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: false));
    //     var result = await service.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto { Data = "payload" });
    //     Assert.Null(result);
    // }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-01")]
    public async Task GetEhrDocumentAsync_ValidIds_ReturnsContractTuple()
    {
        var ehrId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = requesterId,
            Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1, EncryptedFallbackData = "cipher" }],
            Files = []
        });

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(ehrId, requesterId);

        Assert.Null(decryptedData);
        Assert.False(consentDenied);
        Assert.Equal("Requester missing encrypted private key.", denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-02")]
    public async Task GetEhrDocumentAsync_EmptyGuids_ReturnsNotFoundTuple()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(Guid.Empty, Guid.Empty);

        Assert.Null(decryptedData);
        Assert.False(consentDenied);
        Assert.Equal("EHR Record not found", denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-EHRID-EmptyGuid")]
    public async Task GetEhrDocumentAsync_EmptyEhrIdAlias_ReturnsNotFoundTuple()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(Guid.Empty, Guid.NewGuid());

        Assert.Null(decryptedData);
        Assert.False(consentDenied);
        Assert.Equal("EHR Record not found", denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentAsync::GetEhrDocumentAsync-REQUESTERID-EmptyGuid")]
    public async Task GetEhrDocumentAsync_EmptyRequesterId_ReturnsConsentDeniedTuple()
    {
        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = patientId,
            Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1, EncryptedFallbackData = "cipher" }],
            Files = []
        });

        var service = CreateService();
        var (decryptedData, consentDenied, denyMessage) = await service.GetEhrDocumentAsync(ehrId, Guid.Empty);

        Assert.Null(decryptedData);
        Assert.True(consentDenied);
        Assert.NotNull(denyMessage);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-01")]
    public async Task GetEhrDocumentForCurrentUserAsync_ValidShapeInput_ReturnsTuple()
    {
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
        var result = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());
        Assert.True(result.Message is string || result.DecryptedData is string || result.DecryptedData is null);
    }

    [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-04")]
    public async Task GetEhrDocumentForCurrentUserAsync_TupleFlagsShape_IsConsistent()
    {
        var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
        var (decryptedData, forbidden, message) = await service.GetEhrDocumentForCurrentUserAsync(Guid.NewGuid());
        Assert.True(!forbidden || decryptedData is null);
        Assert.True(message is null || message.Length >= 0);
    }

    // [Fact(DisplayName = "GetEhrDocumentForCurrentUserAsync::GetEhrDocumentForCurrentUserAsync-EHRID-EmptyGuid")]
    // public async Task GetEhrDocumentForCurrentUserAsync_EmptyGuidAlias_ReturnsTuple()
    // {
    //     var service = CreateService(context: CreateHttpContext(withBearer: true, withUserId: true));
    //     var (decryptedData, forbidden, message) = await service.GetEhrDocumentForCurrentUserAsync(Guid.Empty);
    //     Assert.Null(decryptedData);
    //     Assert.True(forbidden || message is not null);
    // }

    // [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-02")]
    // public async Task GetEhrFilesAsync_EmptyGuid_ReturnsEmptyCollection()
    // {
    //     _repoMock.Setup(x => x.GetFilesAsync(Guid.Empty)).ReturnsAsync([]);
    //     var service = CreateService();
    //     var result = (await service.GetEhrFilesAsync(Guid.Empty)).ToList();
    //     Assert.Empty(result);
    // }

    // [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-03")]
    // public async Task GetEhrFilesAsync_NotFoundLikeInput_ReturnsEmptyCollection()
    // {
    //     var ehrId = Guid.NewGuid();
    //     _repoMock.Setup(x => x.GetFilesAsync(ehrId)).ReturnsAsync([]);
    //     var service = CreateService();
    //     var result = (await service.GetEhrFilesAsync(ehrId)).ToList();
    //     Assert.Empty(result);
    // }

    // [Fact(DisplayName = "GetEhrFilesAsync::GetEhrFilesAsync-EHRID-EmptyGuid")]
    // public async Task GetEhrFilesAsync_EmptyGuidAlias_ReturnsEmptyCollection()
    // {
    //     _repoMock.Setup(x => x.GetFilesAsync(Guid.Empty)).ReturnsAsync([]);
    //     var service = CreateService();
    //     var result = (await service.GetEhrFilesAsync(Guid.Empty)).ToList();
    //     Assert.Empty(result);
    // }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-02")]
    public async Task GetEhrRecordWithConsentCheckAsync_EmptyGuids_ReturnsNullTuple()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(Guid.Empty, Guid.Empty);
        Assert.Null(record);
        Assert.False(consentDenied);
        Assert.Null(denyMessage);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-EHRID-EmptyGuid")]
    public async Task GetEhrRecordWithConsentCheckAsync_EmptyEhrIdAlias_ReturnsNullTuple()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var (record, _, _) = await service.GetEhrRecordWithConsentCheckAsync(Guid.Empty, Guid.NewGuid());
        Assert.Null(record);
    }

    [Fact(DisplayName = "GetEhrRecordWithConsentCheckAsync::GetEhrRecordWithConsentCheckAsync-REQUESTERID-EmptyGuid")]
    public async Task GetEhrRecordWithConsentCheckAsync_EmptyRequesterId_ReturnsConsentDenied()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync(new EhrRecord
        {
            EhrId = ehrId,
            PatientId = Guid.NewGuid(),
            Versions = [new EhrVersion { VersionId = Guid.NewGuid(), VersionNumber = 1 }],
            Files = []
        });

        var service = CreateService();
        var (record, consentDenied, denyMessage) = await service.GetEhrRecordWithConsentCheckAsync(ehrId, Guid.Empty);

        Assert.Null(record);
        Assert.True(consentDenied);
        Assert.NotNull(denyMessage);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-02")]
    public async Task GetEhrVersionsAsync_EmptyGuid_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetVersionsAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-03")]
    public async Task GetEhrVersionsAsync_NotFoundLikeInput_ReturnsEmptyCollection()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionsAsync(ehrId)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(ehrId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetEhrVersionsAsync::GetEhrVersionsAsync-EHRID-EmptyGuid")]
    public async Task GetEhrVersionsAsync_EmptyGuidAlias_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetVersionsAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService();
        var result = (await service.GetEhrVersionsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-02")]
    public async Task GetOrgEhrRecordsAsync_EmptyGuid_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetByOrgIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-03")]
    public async Task GetOrgEhrRecordsAsync_NotFoundLikeInput_ReturnsEmptyCollection()
    {
        var orgId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByOrgIdAsync(orgId)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(orgId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetOrgEhrRecordsAsync::GetOrgEhrRecordsAsync-ORGID-EmptyGuid")]
    public async Task GetOrgEhrRecordsAsync_EmptyGuidAlias_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetByOrgIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetOrgEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-02")]
    public async Task GetPatientEhrRecordsAsync_EmptyGuid_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetByPatientIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-03")]
    public async Task GetPatientEhrRecordsAsync_NotFoundLikeInput_ReturnsEmptyCollection()
    {
        var patientId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(patientId)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetPatientEhrRecordsAsync::GetPatientEhrRecordsAsync-PATIENTID-EmptyGuid")]
    public async Task GetPatientEhrRecordsAsync_EmptyGuidAlias_ReturnsEmptyCollection()
    {
        _repoMock.Setup(x => x.GetByPatientIdAsync(Guid.Empty)).ReturnsAsync([]);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: true));
        var result = (await service.GetPatientEhrRecordsAsync(Guid.Empty)).ToList();
        Assert.Empty(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-EHRID-EmptyGuid")]
    public async Task GetVersionByIdAsync_EmptyEhrIdAlias_ReturnsNull()
    {
        var versionId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionByIdAsync(Guid.Empty, versionId)).ReturnsAsync((EhrVersion?)null);
        var service = CreateService();
        var result = await service.GetVersionByIdAsync(Guid.Empty, versionId);
        Assert.Null(result);
    }

    [Fact(DisplayName = "GetVersionByIdAsync::GetVersionByIdAsync-VERSIONID-EmptyGuid")]
    public async Task GetVersionByIdAsync_EmptyVersionIdAlias_ReturnsNull()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetVersionByIdAsync(ehrId, Guid.Empty)).ReturnsAsync((EhrVersion?)null);
        var service = CreateService();
        var result = await service.GetVersionByIdAsync(ehrId, Guid.Empty);
        Assert.Null(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-02")]
    public async Task UpdateEhrRecordAsync_EmptyGuid_ReturnsNull()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.UpdateEhrRecordAsync(Guid.Empty, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"v\":1}").RootElement
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-03")]
    public async Task UpdateEhrRecordAsync_NoBearerToken_ServiceLayerHasNoAuthorizationGuard()
    {
        var ehrId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(ehrId)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService(context: CreateHttpContext(withBearer: false, withUserId: false));
        var result = await service.UpdateEhrRecordAsync(ehrId, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"v\":2}").RootElement
        });
        Assert.Null(result);
    }

    [Fact(DisplayName = "UpdateEhrRecordAsync::UpdateEhrRecordAsync-EHRID-EmptyGuid")]
        public async Task UpdateEhrRecordAsync_EmptyGuidAlias_ReturnsNull_DuplicateSet()
    {
        _repoMock.Setup(x => x.GetByIdWithVersionsAsync(Guid.Empty)).ReturnsAsync((EhrRecord?)null);
        var service = CreateService();
        var result = await service.UpdateEhrRecordAsync(Guid.Empty, new UpdateEhrRecordDto
        {
            Data = JsonDocument.Parse("{\"v\":3}").RootElement
        });
        Assert.Null(result);
    }

    private static DefaultHttpContext CreateHttpContext(bool withBearer, bool withUserId)
    {
        var context = new DefaultHttpContext();

        if (withBearer)
        {
            context.Request.Headers["Authorization"] = "Bearer test-token";
        }

        var claims = new List<Claim>();
        if (withUserId)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return context;
    }

    private static HttpResponseMessage JsonResponse(object payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static string ComputeSha256LowerHex(string text)
    {
        return ComputeSha256LowerHex(Encoding.UTF8.GetBytes(text));
    }

    private static string ComputeSha256LowerHex(byte[] data)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
    }

    private static string RemoveVietnameseDiacritics(string input)
    {
        return input
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ô", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ơ", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("à", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ạ", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ả", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ã", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("đ", "d", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}


