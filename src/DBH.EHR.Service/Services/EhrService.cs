using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using MongoDB.Bson;

namespace DBH.EHR.Service.Services;

public class EhrService : IEhrService
{
    private readonly IChangeRequestRepository _changeRequestRepo;
    private readonly IEhrDocumentRepository _ehrDocumentRepo;
    private readonly ILogger<EhrService> _logger;

    public EhrService(
        IChangeRequestRepository changeRequestRepo,
        IEhrDocumentRepository ehrDocumentRepo,
        ILogger<EhrService> logger)
    {
        _changeRequestRepo = changeRequestRepo;
        _ehrDocumentRepo = ehrDocumentRepo;
        _logger = logger;
    }

    public async Task<CreateEhrResponseDto> CreateChangeRequestAsync(CreateEhrRequestDto request)
    {
        _logger.LogInformation(
            "Creating EHR change request for patient {PatientId}, purpose: {Purpose}",
            request.PatientId, request.Purpose);

        // Step 1: Store document in MongoDB (primary)
        var documentJson = request.Document.GetRawText();
        var ehrDocument = new EhrDocument
        {
            PatientId = request.PatientId,
            RecordType = request.RecordType,
            Content = BsonDocument.Parse(documentJson),
            ContentHash = ComputeHash(documentJson),
            Version = 1
        };

        var savedDocument = await _ehrDocumentRepo.CreateAsync(ehrDocument);
        
        _logger.LogInformation(
            "Stored EHR document {DocId} in MongoDB PRIMARY for patient {PatientId}",
            savedDocument.Id, request.PatientId);

        // Step 2: Create change request in PostgreSQL (primary) with PENDING status
        var changeRequest = new ChangeRequest
        {
            PatientId = request.PatientId,
            Purpose = request.Purpose,
            RequestedScope = request.RequestedScope,
            TtlMinutes = request.TtlMinutes,
            Status = RequestStatus.PENDING,
            OffchainDocId = savedDocument.Id,
            Approvals = "[]" // Empty approvals array
        };

        var savedRequest = await _changeRequestRepo.CreateAsync(changeRequest);
        
        _logger.LogInformation(
            "Created change request {RequestId} in PostgreSQL PRIMARY with status PENDING",
            savedRequest.Id);

        return new CreateEhrResponseDto
        {
            ChangeRequestId = savedRequest.Id,
            OffchainDocId = savedDocument.Id,
            Status = savedRequest.Status.ToString(),
            CreatedAt = savedRequest.CreatedAt,
            WriteToNode = new WriteNodeMetadata
            {
                PostgresNode = "pg_primary",
                MongoNode = "mongo1 (primary)"
            }
        };
    }

    public async Task<ChangeRequest?> GetChangeRequestAsync(Guid requestId)
    {
        return await _changeRequestRepo.GetByIdAsync(requestId);
    }

    public async Task<IEnumerable<ChangeRequest>> GetChangeRequestsByPatientAsync(string patientId)
    {
        return await _changeRequestRepo.GetByPatientIdAsync(patientId);
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
