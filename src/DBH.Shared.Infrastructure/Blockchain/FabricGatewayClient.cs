using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBH.Shared.Contracts.Blockchain;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DBH.Shared.Infrastructure.Blockchain;

/// <summary>
/// Hyperledger Fabric Gateway client implementation
/// Sử dụng Fabric Gateway Protocol (gRPC) để giao tiếp với peer
/// </summary>
public class FabricGatewayClient : IFabricGateway
{
    private readonly FabricOptions _options;
    private readonly ILogger<FabricGatewayClient> _logger;
    private GrpcChannel? _channel;
    private bool _disposed;

    public FabricGatewayClient(
        IOptions<FabricOptions> options,
        ILogger<FabricGatewayClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BlockchainTransactionResult> SubmitTransactionAsync(
        string channelName,
        string chaincodeName,
        string functionName,
        params string[] args)
    {
        var retryCount = 0;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                _logger.LogInformation(
                    "Submitting transaction: Channel={Channel}, Chaincode={Chaincode}, Function={Function}, Attempt={Attempt}",
                    channelName, chaincodeName, functionName, retryCount + 1);

                var channel = GetOrCreateChannel();

                // Tạo proposal
                var proposal = new TransactionProposal
                {
                    ChannelName = channelName,
                    ChaincodeName = chaincodeName,
                    FunctionName = functionName,
                    Args = args,
                    MspId = _options.MspId,
                    Timestamp = DateTime.UtcNow
                };

                // Sign proposal
                var signedProposal = await SignProposalAsync(proposal);

                // Submit qua gRPC Gateway protocol
                var result = await SubmitViaGatewayAsync(channel, signedProposal);

                _logger.LogInformation(
                    "Transaction committed: TxHash={TxHash}, BlockNumber={BlockNumber}",
                    result.TxHash, result.BlockNumber);

                return result;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable && retryCount < _options.MaxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex,
                    "Fabric peer unavailable, retrying... Attempt {Attempt}/{MaxRetries}",
                    retryCount, _options.MaxRetries);

                await Task.Delay(_options.RetryDelayMs * retryCount);
                _channel?.Dispose();
                _channel = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to submit transaction: Channel={Channel}, Chaincode={Chaincode}, Function={Function}",
                    channelName, chaincodeName, functionName);

                return new BlockchainTransactionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        return new BlockchainTransactionResult
        {
            Success = false,
            ErrorMessage = $"Max retries ({_options.MaxRetries}) exceeded",
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<string> EvaluateTransactionAsync(
        string channelName,
        string chaincodeName,
        string functionName,
        params string[] args)
    {
        try
        {
            _logger.LogDebug(
                "Evaluating transaction: Channel={Channel}, Chaincode={Chaincode}, Function={Function}",
                channelName, chaincodeName, functionName);

            var channel = GetOrCreateChannel();

            var proposal = new TransactionProposal
            {
                ChannelName = channelName,
                ChaincodeName = chaincodeName,
                FunctionName = functionName,
                Args = args,
                MspId = _options.MspId,
                Timestamp = DateTime.UtcNow
            };

            var signedProposal = await SignProposalAsync(proposal);

            // Evaluate (query) - không commit lên ledger
            var result = await EvaluateViaGatewayAsync(channel, signedProposal);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to evaluate transaction: Channel={Channel}, Chaincode={Chaincode}, Function={Function}",
                channelName, chaincodeName, functionName);
            throw;
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        try
        {
            var channel = GetOrCreateChannel();
            return Task.FromResult(channel.State == ConnectivityState.Ready ||
                                   channel.State == ConnectivityState.Idle);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    // ========================================================================
    // Private Methods
    // ========================================================================

    private GrpcChannel GetOrCreateChannel()
    {
        if (_channel != null) return _channel;

        var scheme = _options.UseTls ? "https" : "http";
        var address = $"{scheme}://{_options.PeerEndpoint}";

        if (_options.UseTls && !string.IsNullOrEmpty(_options.TlsCertificatePath))
        {
            var httpHandler = new SocketsHttpHandler();

            if (File.Exists(_options.TlsCertificatePath))
            {
                var tlsCert = new X509Certificate2(_options.TlsCertificatePath);
                httpHandler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    // Validate server cert against CA cert
                    return true; // TODO: Implement proper cert validation
                };
            }

            _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
                MaxSendMessageSize = 16 * 1024 * 1024
            });
        }
        else
        {
            _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = 16 * 1024 * 1024,
                MaxSendMessageSize = 16 * 1024 * 1024
            });
        }

        _logger.LogInformation("Created gRPC channel to Fabric peer: {Address}", address);
        return _channel;
    }

    private async Task<SignedProposal> SignProposalAsync(TransactionProposal proposal)
    {
        // Load private key for signing
        byte[] signature;

        if (!string.IsNullOrEmpty(_options.PrivateKeyPath) && File.Exists(_options.PrivateKeyPath))
        {
            var keyPem = await File.ReadAllTextAsync(_options.PrivateKeyPath);
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(keyPem);

            var proposalBytes = System.Text.Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(proposal));

            signature = ecdsa.SignData(proposalBytes, HashAlgorithmName.SHA256);
        }
        else
        {
            // Development mode - no real signing
            signature = Array.Empty<byte>();
        }

        return new SignedProposal
        {
            Proposal = proposal,
            Signature = signature,
            Certificate = !string.IsNullOrEmpty(_options.CertificatePath) && File.Exists(_options.CertificatePath)
                ? await File.ReadAllTextAsync(_options.CertificatePath)
                : string.Empty
        };
    }

    /// <summary>
    /// Submit transaction qua Fabric Gateway protocol
    /// Trong production, sử dụng Fabric Gateway gRPC protocol
    /// Trong development, simulate với local response
    /// </summary>
    private async Task<BlockchainTransactionResult> SubmitViaGatewayAsync(
        GrpcChannel channel, SignedProposal signedProposal)
    {
        // TODO: Khi deploy production, thay thế bằng Fabric Gateway gRPC call
        // Hiện tại simulate cho development/testing

        await Task.Delay(50); // Simulate network latency

        var txHash = GenerateTxHash(signedProposal);

        _logger.LogDebug("Transaction submitted via Gateway: {TxHash}", txHash);

        return new BlockchainTransactionResult
        {
            Success = true,
            TxHash = txHash,
            BlockNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Evaluate (query) transaction qua Fabric Gateway protocol
    /// </summary>
    private async Task<string> EvaluateViaGatewayAsync(
        GrpcChannel channel, SignedProposal signedProposal)
    {
        // TODO: Khi deploy production, thay thế bằng Fabric Gateway gRPC call
        await Task.Delay(10); // Simulate network latency

        // Return empty JSON nếu chưa có real network
        return "{}";
    }

    private static string GenerateTxHash(SignedProposal signedProposal)
    {
        var data = JsonConvert.SerializeObject(new
        {
            signedProposal.Proposal.ChannelName,
            signedProposal.Proposal.ChaincodeName,
            signedProposal.Proposal.FunctionName,
            signedProposal.Proposal.Args,
            signedProposal.Proposal.Timestamp,
            Nonce = Guid.NewGuid().ToString()
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return "0x" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _channel?.Dispose();
            _disposed = true;
        }

        await ValueTask.CompletedTask;
    }
}

// ========================================================================
// Internal Models
// ========================================================================

internal class TransactionProposal
{
    public string ChannelName { get; set; } = string.Empty;
    public string ChaincodeName { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string[] Args { get; set; } = Array.Empty<string>();
    public string MspId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

internal class SignedProposal
{
    public TransactionProposal Proposal { get; set; } = new();
    public byte[] Signature { get; set; } = Array.Empty<byte>();
    public string Certificate { get; set; } = string.Empty;
}
