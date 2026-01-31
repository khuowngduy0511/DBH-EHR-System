using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DBH.EHR.Service.Models.Documents;

/// <summary>
/// EHR Document lưu MongoDB (off-chain).
/// </summary>
public class EhrDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [BsonElement("recordType")]
    public string? RecordType { get; set; }


    [BsonElement("content")]
    public BsonDocument Content { get; set; } = new BsonDocument();

    /// <summary>
    /// SHA256 hash 
    /// </summary>
    [BsonElement("contentHash")]
    public string? ContentHash { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;
}