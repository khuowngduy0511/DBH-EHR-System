using DBH.EHR.Service.Data;
using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBH.EHR.Service.Repositories.Mongo;


public class EhrDocumentRepository : IEhrDocumentRepository
{
    private readonly MongoDbContext _mongoContext;
    private readonly ILogger<EhrDocumentRepository> _logger;

    public EhrDocumentRepository(
        MongoDbContext mongoContext,
        ILogger<EhrDocumentRepository> logger)
    {
        _mongoContext = mongoContext;
        _logger = logger;
    }

    // Ghi (Primary) 

    public async Task<EhrDocument> CreateAsync(EhrDocument document)
    {
        document.CreatedAt = DateTime.UtcNow;
        
        await _mongoContext.EhrDocuments.InsertOneAsync(document);
        
        _logger.LogInformation(
            "Tạo EhrDocument {DocId} cho EHR {EhrId} file {FileId} trên MongoDB PRIMARY",
            document.Id, document.EhrId, document.FileId);
        
        return document;
    }

    // Đọc (Primary hoặc Secondary) 

    public async Task<EhrDocument?> GetByIdAsync(string id, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocument {DocId} từ MongoDB {Node}", id, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.Id, id);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<EhrDocument?> GetByFileIdAsync(Guid fileId, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocument theo FileId {FileId} từ MongoDB {Node}", fileId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.FileId, fileId);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<EhrDocument?> GetByEhrIdAsync(Guid ehrId, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocument theo EhrId {EhrId} từ MongoDB {Node}", ehrId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.EhrId, ehrId);
        return await collection.Find(filter)
            .SortByDescending(d => d.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<EhrDocument?> GetByVersionIdAsync(Guid versionId, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocument theo VersionId {VersionId} từ MongoDB {Node}", versionId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.VersionId, versionId);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EhrDocument>> GetByPatientIdAsync(Guid patientId, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocuments cho bệnh nhân {PatientId} từ MongoDB {Node}", patientId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.PatientId, patientId);
        return await collection.Find(filter)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrDocument>> GetVersionsByEhrIdAsync(Guid ehrId, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc tất cả version cho EHR {EhrId} từ MongoDB {Node}", ehrId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.EhrId, ehrId);
        return await collection.Find(filter)
            .SortByDescending(d => d.Version)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrDocument>> GetByReportTypeAsync(Guid patientId, ReportType reportType, bool useSecondary = false)
    {
        var node = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Đọc EhrDocuments loại {ReportType} cho bệnh nhân {PatientId} từ MongoDB {Node}", 
            reportType, patientId, node);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.And(
            Builders<EhrDocument>.Filter.Eq(d => d.PatientId, patientId),
            Builders<EhrDocument>.Filter.Eq(d => d.ReportType, reportType.ToString())
        );
        return await collection.Find(filter)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    //  Kiểm tra replication

    public async Task<bool> ExistsOnSecondaryAsync(string id)
    {
        var collection = _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.Secondary);
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.Id, id);
        return await collection.Find(filter).AnyAsync();
    }

    public async Task<string> GetReadNodeInfoAsync(bool useSecondary = false)
    {
        try
        {
            var collection = useSecondary
                ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
                : _mongoContext.EhrDocuments;
            
            var db = collection.Database;
            var serverStatus = await db.RunCommandAsync<BsonDocument>(new BsonDocument("serverStatus", 1));
            var host = serverStatus.GetValue("host", "unknown").AsString;
            var ismaster = await db.RunCommandAsync<BsonDocument>(new BsonDocument("isMaster", 1));
            var isPrimary = ismaster.GetValue("ismaster", false).AsBoolean;
            
            return isPrimary ? $"PRIMARY ({host})" : $"SECONDARY ({host})";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi lấy thông tin node MongoDB");
            return useSecondary ? "SECONDARY (unknown)" : "PRIMARY (unknown)";
        }
    }
}
