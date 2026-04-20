using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DBH.Shared.Infrastructure.Blockchain.Services;

/// <summary>
/// Implementation: Emergency access log on Hyperledger Fabric.
/// </summary>
public class EmergencyBlockchainService : IEmergencyBlockchainService
{
    private static readonly JsonSerializerSettings ChaincodeJsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly IFabricGateway _gateway;
    private readonly ILogger<EmergencyBlockchainService> _logger;

    public EmergencyBlockchainService(
        IFabricGateway gateway,
        ILogger<EmergencyBlockchainService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<BlockchainTransactionResult> EmergencyAccessAsync(EmergencyAccessRecord record)
    {
        _logger.LogInformation(
            "Recording emergency access on blockchain: LogId={LogId}, RecordDid={RecordDid}, AccessorDid={AccessorDid}",
            record.LogId, record.TargetRecordDid, record.AccessorDid);

        _ = JsonConvert.SerializeObject(record, ChaincodeJsonSettings);

        var result = await _gateway.SubmitTransactionAsync(
            FabricChannels.EhrChannel,
            FabricChaincodes.EhrChaincode,
            ChaincodeFunctions.EmergencyAccess,
            record.TargetRecordDid,
            record.AccessorDid,
            record.Reason);

        if (result.Success)
        {
            _logger.LogInformation(
                "Emergency access committed: LogId={LogId}, TxHash={TxHash}",
                record.LogId, result.TxHash);
        }

        return result;
    }

    public async Task<List<EmergencyAccessRecord>> GetEmergencyAccessByRecordAsync(string targetRecordDid)
    {
        return await EvaluateListAsync(
            targetRecordDid,
            ChaincodeFunctions.GetEmergencyAccessByRecord,
            "record",
            targetRecordDid);
    }

    public async Task<List<EmergencyAccessRecord>> GetEmergencyAccessByAccessorAsync(string accessorDid)
    {
        return await EvaluateListAsync(
            accessorDid,
            ChaincodeFunctions.GetEmergencyAccessByAccessor,
            "accessor",
            accessorDid);
    }

    public async Task<List<EmergencyAccessRecord>> GetAllEmergencyAccessAsync()
    {
        return await EvaluateListAsync(
            string.Empty,
            ChaincodeFunctions.GetAllEmergencyAccess,
            "all",
            string.Empty);
    }

    private async Task<List<EmergencyAccessRecord>> EvaluateListAsync(string arg, string functionName, string operationLabel, string logValue)
    {
        try
        {
            var resultJson = string.IsNullOrWhiteSpace(arg)
                ? await _gateway.EvaluateTransactionAsync(FabricChannels.EhrChannel, FabricChaincodes.EhrChaincode, functionName)
                : await _gateway.EvaluateTransactionAsync(FabricChannels.EhrChannel, FabricChaincodes.EhrChaincode, functionName, arg);

            if (string.IsNullOrWhiteSpace(resultJson) || resultJson == "{}")
            {
                return new List<EmergencyAccessRecord>();
            }

            return JsonConvert.DeserializeObject<List<EmergencyAccessRecord>>(resultJson) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get emergency access {Operation} logs from blockchain: {Value}", operationLabel, logValue);
            return new List<EmergencyAccessRecord>();
        }
    }
}
