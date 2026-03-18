using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DBH.Shared.Infrastructure.Blockchain.Services;

/// <summary>
/// Implementation: Ghi Audit log lên Hyperledger Fabric
/// </summary>
public class AuditBlockchainService : IAuditBlockchainService
{
    private static readonly JsonSerializerSettings ChaincodeJsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly IFabricGateway _gateway;
    private readonly ILogger<AuditBlockchainService> _logger;

    public AuditBlockchainService(
        IFabricGateway gateway,
        ILogger<AuditBlockchainService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<BlockchainTransactionResult> CommitAuditEntryAsync(AuditEntry entry)
    {
        _logger.LogInformation(
            "Committing audit entry to blockchain: AuditId={AuditId}, Action={Action}, Target={TargetType}:{TargetId}",
            entry.AuditId, entry.Action, entry.TargetType, entry.TargetId);

        var entryJson = JsonConvert.SerializeObject(entry, ChaincodeJsonSettings);

        var result = await _gateway.SubmitTransactionAsync(
            FabricChannels.AuditChannel,
            FabricChaincodes.AuditChaincode,
            ChaincodeFunctions.CreateAuditEntry,
            entry.AuditId,
            entryJson);

        if (result.Success)
        {
            _logger.LogInformation(
                "Audit entry committed: AuditId={AuditId}, TxHash={TxHash}",
                entry.AuditId, result.TxHash);
        }

        return result;
    }

    public async Task<AuditEntry?> GetAuditEntryAsync(string auditId)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.AuditChannel,
                FabricChaincodes.AuditChaincode,
                ChaincodeFunctions.GetAuditEntry,
                auditId);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return null;

            return JsonConvert.DeserializeObject<AuditEntry>(resultJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit entry from blockchain: AuditId={AuditId}", auditId);
            return null;
        }
    }

    public async Task<List<AuditEntry>> GetAuditsByPatientAsync(string patientDid)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.AuditChannel,
                FabricChaincodes.AuditChaincode,
                ChaincodeFunctions.GetAuditsByPatient,
                patientDid);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return new List<AuditEntry>();

            return JsonConvert.DeserializeObject<List<AuditEntry>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get audits by patient from blockchain: PatientDid={PatientDid}", patientDid);
            return new List<AuditEntry>();
        }
    }

    public async Task<List<AuditEntry>> GetAuditsByActorAsync(string actorDid)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.AuditChannel,
                FabricChaincodes.AuditChaincode,
                ChaincodeFunctions.GetAuditsByActor,
                actorDid);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return new List<AuditEntry>();

            return JsonConvert.DeserializeObject<List<AuditEntry>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get audits by actor from blockchain: ActorDid={ActorDid}", actorDid);
            return new List<AuditEntry>();
        }
    }
}
