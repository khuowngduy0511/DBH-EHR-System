using DBH.EHR.Service.Data;
using DBH.EHR.Service.Models.Documents;
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

    public async Task<EhrDocument> CreateAsync(EhrDocument document)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        
        // Write to primary (default behavior)
        await _mongoContext.EhrDocuments.InsertOneAsync(document);
        
        _logger.LogInformation(
            "Created EhrDocument {DocId} for patient {PatientId} on MongoDB PRIMARY",
            document.Id, document.PatientId);
        
        return document;
    }

    public async Task<EhrDocument?> GetByIdAsync(string id, bool useSecondary = false)
    {
        var nodeName = useSecondary ? "SECONDARY" : "PRIMARY";
        _logger.LogDebug("Reading EhrDocument {DocId} from MongoDB {Node}", id, nodeName);
        
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.Id, id);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EhrDocument>> GetByPatientIdAsync(string patientId, bool useSecondary = false)
    {
        var collection = useSecondary 
            ? _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.SecondaryPreferred)
            : _mongoContext.EhrDocuments;
        
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.PatientId, patientId);
        return await collection.Find(filter)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsOnSecondaryAsync(string id)
    {
        var collection = _mongoContext.GetEhrDocumentsWithReadPreference(ReadPreference.Secondary);
        var filter = Builders<EhrDocument>.Filter.Eq(d => d.Id, id);
        return await collection.Find(filter).AnyAsync();
    }
}
