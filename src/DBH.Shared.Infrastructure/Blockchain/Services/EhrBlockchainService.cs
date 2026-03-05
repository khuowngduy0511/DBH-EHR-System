using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DBH.Shared.Infrastructure.Blockchain.Services;

/// <summary>
/// Implementation: Quản lý EHR hash trên Hyperledger Fabric
/// </summary>
public class EhrBlockchainService : IEhrBlockchainService
{
    private readonly IFabricGateway _gateway;
    private readonly ILogger<EhrBlockchainService> _logger;

    public EhrBlockchainService(
        IFabricGateway gateway,
        ILogger<EhrBlockchainService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<BlockchainTransactionResult> CommitEhrHashAsync(EhrHashRecord record)
    {
        _logger.LogInformation(
            "Committing EHR hash to blockchain: EhrId={EhrId}, Version={Version}, Hash={Hash}",
            record.EhrId, record.Version, record.ContentHash);

        var recordJson = JsonConvert.SerializeObject(record);

        var result = await _gateway.SubmitTransactionAsync(
            FabricChannels.EhrHashChannel,
            FabricChaincodes.EhrChaincode,
            ChaincodeFunctions.CreateEhrHash,
            record.EhrId,
            record.Version.ToString(),
            recordJson);

        if (result.Success)
        {
            _logger.LogInformation(
                "EHR hash committed: EhrId={EhrId}, TxHash={TxHash}, Block={Block}",
                record.EhrId, result.TxHash, result.BlockNumber);
        }

        return result;
    }

    public async Task<EhrHashRecord?> GetEhrHashAsync(string ehrId, int version)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.EhrHashChannel,
                FabricChaincodes.EhrChaincode,
                ChaincodeFunctions.GetEhrHash,
                ehrId,
                version.ToString());

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return null;

            return JsonConvert.DeserializeObject<EhrHashRecord>(resultJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get EHR hash from blockchain: EhrId={EhrId}, Version={Version}",
                ehrId, version);
            return null;
        }
    }

    public async Task<List<EhrHashRecord>> GetEhrHistoryAsync(string ehrId)
    {
        try
        {
            var resultJson = await _gateway.EvaluateTransactionAsync(
                FabricChannels.EhrHashChannel,
                FabricChaincodes.EhrChaincode,
                ChaincodeFunctions.GetEhrHistory,
                ehrId);

            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
                return new List<EhrHashRecord>();

            return JsonConvert.DeserializeObject<List<EhrHashRecord>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get EHR history from blockchain: EhrId={EhrId}", ehrId);
            return new List<EhrHashRecord>();
        }
    }

    public async Task<bool> VerifyEhrIntegrityAsync(string ehrId, int version, string currentHash)
    {
        try
        {
            var blockchainRecord = await GetEhrHashAsync(ehrId, version);

            if (blockchainRecord == null)
            {
                _logger.LogWarning(
                    "No blockchain record found for verification: EhrId={EhrId}, Version={Version}",
                    ehrId, version);
                return false;
            }

            var isValid = string.Equals(
                blockchainRecord.ContentHash, currentHash,
                StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "EHR integrity check FAILED: EhrId={EhrId}, Version={Version}, " +
                    "Expected={Expected}, Actual={Actual}",
                    ehrId, version, blockchainRecord.ContentHash, currentHash);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error verifying EHR integrity: EhrId={EhrId}, Version={Version}",
                ehrId, version);
            return false;
        }
    }
}
