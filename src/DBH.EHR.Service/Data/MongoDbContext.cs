using DBH.EHR.Service.Models.Documents;
using MongoDB.Driver;

namespace DBH.EHR.Service.Data;


public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    
    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _client = client;
        _database = client.GetDatabase(databaseName);
    }
    
    public IMongoClient Client => _client;
    
    public IMongoCollection<EhrDocument> EhrDocuments => 
        _database.GetCollection<EhrDocument>("ehr_documents");
    
    public IMongoCollection<EhrDocument> GetEhrDocumentsWithReadPreference(ReadPreference readPreference)
    {
        return _database
            .WithReadPreference(readPreference)
            .GetCollection<EhrDocument>("ehr_documents");
    }
}
