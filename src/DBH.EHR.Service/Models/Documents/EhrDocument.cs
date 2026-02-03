using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DBH.EHR.Service.Models.Documents;

/// <summary>
/// EHR Document trong MongoDB 
/// </summary>
public class EhrDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }


    [BsonElement("ehrId")]
    [BsonRepresentation(BsonType.String)]
    public Guid EhrId { get; set; }


    [BsonElement("versionId")]
    [BsonRepresentation(BsonType.String)]
    public Guid VersionId { get; set; }


    [BsonElement("fileId")]
    [BsonRepresentation(BsonType.String)]
    public Guid FileId { get; set; }


    [BsonElement("patientId")]
    [BsonRepresentation(BsonType.String)]
    public Guid PatientId { get; set; }

 
    [BsonElement("reportType")]
    public string ReportType { get; set; } = string.Empty;


    [BsonElement("data")]
    public BsonDocument Data { get; set; } = new BsonDocument();

    /// <summary>
    /// SHA256 hash 
    /// </summary>
    [BsonElement("dataHash")]
    public string? DataHash { get; set; }


    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.String)]
    public Guid? CreatedBy { get; set; }
}
