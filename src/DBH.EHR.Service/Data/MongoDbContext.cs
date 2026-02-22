using DBH.EHR.Service.Models.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DBH.EHR.Service.Data;

/// <summary>
/// MongoDB configuration
/// </summary>
public class MongoDbConfiguration
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "dbh_ehr_fhir";
}

/// <summary>
/// MongoDB context cho FHIR documents
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    
    public MongoDbContext(IOptions<MongoDbConfiguration> options)
    {
        var config = options.Value;
        _client = new MongoClient(config.ConnectionString);
        _database = _client.GetDatabase(config.DatabaseName);
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
