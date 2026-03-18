using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DBH.Shared.Infrastructure.Blockchain.Services;

/// <summary>
/// Implementation: Quản lý Consent trên Hyperledger Fabric
/// </summary>
public class ConsentBlockchainService : IConsentBlockchainService
{
    private static readonly JsonSerializerSettings ChaincodeJsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly IFabricGateway _gateway;
    private readonly ILogger<ConsentBlockchainService> _logger;

    public ConsentBlockchainService(
        IFabricGateway gateway,
        ILogger<ConsentBlockchainService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<BlockchainTransactionResult> GrantConsentAsync(ConsentRecord record)
    {
        _logger.LogInformation(
            "Granting consent on blockchain: ConsentId={ConsentId}, Patient={PatientDid}, Grantee={GranteeDid}",
            record.ConsentId, record.PatientDid, record.GranteeDid);

        var recordJson = JsonConvert.SerializeObject(record, ChaincodeJsonSettings);

        var result = await _gateway.SubmitTransactionAsync(
            FabricChannels.ConsentChannel,
            FabricChaincodes.ConsentChaincode,
            ChaincodeFunctions.GrantConsent,
            record.ConsentId,
            record.PatientDid,
            record.GranteeDid,
            recordJson,
            record.EncryptedAesKey ?? string.Empty);

        if (result.Success)
        {
            _logger.LogInformation(
                "Consent granted on blockchain: ConsentId={ConsentId}, TxHash={TxHash}",
                record.ConsentId, result.TxHash);
        }

        return result;
    }

    public async Task<BlockchainTransactionResult> RevokeConsentAsync(
        string consentId, string revokedAt, string? reason)
    {
        _logger.LogInformation(
            "Revoking consent on blockchain: ConsentId={ConsentId}", consentId);

        var result = await _gateway.SubmitTransactionAsync(
            FabricChannels.ConsentChannel,
            FabricChaincodes.ConsentChaincode,
            ChaincodeFunctions.RevokeConsent,
            consentId,
            revokedAt,
            reason ?? string.Empty);

        if (result.Success)
        {
            _logger.LogInformation(
                "Consent revoked on blockchain: ConsentId={ConsentId}, TxHash={TxHash}",
                consentId, result.TxHash);
        }

        return result;
    }

    public async Task<ConsentRecord?> GetConsentAsync(string consentId)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.ConsentChannel,
                FabricChaincodes.ConsentChaincode,
                ChaincodeFunctions.GetConsent,
                consentId);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return null;

            return JsonConvert.DeserializeObject<ConsentRecord>(resultJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent from blockchain: ConsentId={ConsentId}", consentId);
            return null;
        }
    }

    public async Task<bool> VerifyConsentAsync(string consentId, string granteeDid)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.ConsentChannel,
                FabricChaincodes.ConsentChaincode,
                ChaincodeFunctions.VerifyConsent,
                consentId,
                granteeDid);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return false;

            var payload = JObject.Parse(resultJson);
            var validToken = payload.GetValue("valid", StringComparison.OrdinalIgnoreCase);
            return validToken?.Value<bool>() ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to verify consent on blockchain: ConsentId={ConsentId}, Grantee={Grantee}",
                consentId, granteeDid);
            return false;
        }
    }

    public async Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientDid)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.ConsentChannel,
                FabricChaincodes.ConsentChaincode,
                ChaincodeFunctions.GetPatientConsents,
                patientDid);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return new List<ConsentRecord>();

            return JsonConvert.DeserializeObject<List<ConsentRecord>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get patient consents from blockchain: PatientDid={PatientDid}", patientDid);
            return new List<ConsentRecord>();
        }
    }

    public async Task<List<ConsentRecord>> GetConsentHistoryAsync(string consentId)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.ConsentChannel,
                FabricChaincodes.ConsentChaincode,
                ChaincodeFunctions.GetConsentHistory,
                consentId);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return new List<ConsentRecord>();

            return JsonConvert.DeserializeObject<List<ConsentRecord>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get consent history from blockchain: ConsentId={ConsentId}", consentId);
            return new List<ConsentRecord>();
        }
    }
}
