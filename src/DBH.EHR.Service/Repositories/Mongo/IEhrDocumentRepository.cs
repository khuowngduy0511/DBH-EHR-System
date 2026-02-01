using DBH.EHR.Service.Models.Documents;
using MongoDB.Bson;

namespace DBH.EHR.Service.Repositories.Mongo;

public interface IEhrDocumentRepository
{
    Task<EhrDocument> CreateAsync(EhrDocument document);
    Task<EhrDocument?> GetByIdAsync(string id, bool useSecondary = false);
    Task<IEnumerable<EhrDocument>> GetByPatientIdAsync(string patientId, bool useSecondary = false);
    Task<bool> ExistsOnSecondaryAsync(string id);
}
