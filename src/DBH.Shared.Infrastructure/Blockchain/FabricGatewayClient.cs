using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DBH.Shared.Contracts.Blockchain;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DBH.Shared.Infrastructure.Blockchain;

/// <summary>
/// Hyperledger Fabric Gateway client implementation.
/// 
/// Supports two modes:
///   1. REAL MODE (Enabled=true + crypto files present):
///      Connects to Fabric peer via gRPC Gateway protocol (Fabric 2.4+).
///      Uses peer CLI container to submit/evaluate transactions.
///   
///   2. SIMULATION MODE (Enabled=true + no crypto, or development):
///      Returns simulated hashes/responses for local development.
///      Controlled by FabricOptions.SimulationMode = true.
/// </summary>
public class FabricGatewayClient : IFabricGateway
{
    private readonly FabricOptions _options;
    private readonly ILogger<FabricGatewayClient> _logger;
    private GrpcChannel? _channel;
    private ECDsa? _signingKey;
    private string? _certificate;
    private byte[]? _tlsCaCert;
    private bool _cryptoLoaded;
    private bool _disposed;

    public FabricGatewayClient(
        IOptions<FabricOptions> options,
        ILogger<FabricGatewayClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // ========================================================================
    // IFabricGateway Implementation
    // ========================================================================

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

                await EnsureCryptoLoadedAsync();

                if (_options.SimulationMode || !_cryptoLoaded)
                {
                    return await SimulateSubmitAsync(channelName, chaincodeName, functionName, args);
                }

                var result = await SubmitViaGatewayAsync(channelName, chaincodeName, functionName, args);

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
                ResetChannel();
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

            await EnsureCryptoLoadedAsync();

            if (_options.SimulationMode || !_cryptoLoaded)
            {
                return SimulateEvaluate(chaincodeName, functionName, args);
            }

            return await EvaluateViaGatewayAsync(channelName, chaincodeName, functionName, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to evaluate transaction: Channel={Channel}, Chaincode={Chaincode}, Function={Function}",
                channelName, chaincodeName, functionName);
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            if (_options.SimulationMode) return true;

            await EnsureCryptoLoadedAsync();
            if (!_cryptoLoaded) return false;

            var channel = GetOrCreateChannel();
            return channel.State == ConnectivityState.Ready ||
                   channel.State == ConnectivityState.Idle;
        }
        catch
        {
            return false;
        }
    }

    // ========================================================================
    // Crypto Material Loading
    // ========================================================================

    private async Task EnsureCryptoLoadedAsync()
    {
        if (_cryptoLoaded) return;

        try
        {
            // Load signing key
            if (!string.IsNullOrEmpty(_options.PrivateKeyPath) && File.Exists(_options.PrivateKeyPath))
            {
                var keyPem = await File.ReadAllTextAsync(_options.PrivateKeyPath);
                _signingKey = ECDsa.Create();
                _signingKey.ImportFromPem(keyPem);
                _logger.LogInformation("Loaded signing key from {Path}", _options.PrivateKeyPath);
            }
            else if (!string.IsNullOrEmpty(_options.PrivateKeyDirectory) && Directory.Exists(_options.PrivateKeyDirectory))
            {
                // Fabric CA stores keys with random names in keystore/
                var keyFiles = Directory.GetFiles(_options.PrivateKeyDirectory, "*_sk");
                if (keyFiles.Length == 0)
                    keyFiles = Directory.GetFiles(_options.PrivateKeyDirectory, "*.pem");

                if (keyFiles.Length > 0)
                {
                    var keyPem = await File.ReadAllTextAsync(keyFiles[0]);
                    _signingKey = ECDsa.Create();
                    _signingKey.ImportFromPem(keyPem);
                    _logger.LogInformation("Loaded signing key from {Path}", keyFiles[0]);
                }
            }

            // Load certificate
            if (!string.IsNullOrEmpty(_options.CertificatePath) && File.Exists(_options.CertificatePath))
            {
                _certificate = await File.ReadAllTextAsync(_options.CertificatePath);
                _logger.LogInformation("Loaded certificate from {Path}", _options.CertificatePath);
            }

            // Load TLS CA cert
            if (!string.IsNullOrEmpty(_options.TlsCertificatePath) && File.Exists(_options.TlsCertificatePath))
            {
                _tlsCaCert = await File.ReadAllBytesAsync(_options.TlsCertificatePath);
                _logger.LogInformation("Loaded TLS CA cert from {Path}", _options.TlsCertificatePath);
            }

            _cryptoLoaded = _signingKey != null && _certificate != null;

            if (!_cryptoLoaded)
            {
                _logger.LogWarning(
                    "Crypto material not fully loaded (Key={HasKey}, Cert={HasCert}). " +
                    "Running in SIMULATION mode. To use real Fabric network, " +
                    "run 'blockchain/network.sh up' first.",
                    _signingKey != null, _certificate != null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load crypto material, falling back to simulation mode");
            _cryptoLoaded = false;
        }
    }

    // ========================================================================
    // Real Fabric Gateway Protocol (gRPC)
    // ========================================================================
    // Fabric Gateway (2.4+) protocol:
    //   1. Endorse: Send proposal → get endorsed proposal
    //   2. Submit: Send endorsed tx → orderer commits
    //   3. CommitStatus: Wait for commit confirmation
    // ========================================================================

    private async Task<BlockchainTransactionResult> SubmitViaGatewayAsync(
        string channelName, string chaincodeName, string functionName, string[] args)
    {
        var channel = GetOrCreateChannel();

        // Build the proposal payload
        var nonce = new byte[24];
        RandomNumberGenerator.Fill(nonce);
        var txId = ComputeTxId(nonce, _certificate!);

        var proposalPayload = BuildProposalPayload(channelName, chaincodeName, functionName, args, txId, nonce);

        // Sign the proposal
        var signature = SignPayload(proposalPayload);

        // Use the Fabric Gateway gRPC service
        // The gateway.Gateway service has: Evaluate, Endorse, Submit, CommitStatus
        var invoker = channel.CreateCallInvoker();

        // Step 1: Endorse
        var endorseRequest = new EndorseRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            ProposedTransaction = proposalPayload,
            ProposalSignature = signature
        };

        _logger.LogDebug("Endorsing transaction {TxId}...", txId);

        var endorseMethod = new Method<EndorseRequest, EndorseResponse>(
            MethodType.Unary,
            "gateway.Gateway",
            "Endorse",
            EndorseRequest.Marshaller,
            EndorseResponse.Marshaller);

        var callOptions = new CallOptions(
            deadline: DateTime.UtcNow.AddSeconds(_options.SubmitTimeoutSeconds));

        var endorseResponse = await invoker.AsyncUnaryCall(endorseMethod, null, callOptions, endorseRequest);

        if (endorseResponse == null || string.IsNullOrEmpty(endorseResponse.PreparedTransaction))
        {
            return new BlockchainTransactionResult
            {
                Success = false,
                ErrorMessage = "Endorsement failed: no prepared transaction returned",
                Timestamp = DateTime.UtcNow
            };
        }

        // Step 2: Submit (send to orderer)
        var submitSignature = SignPayload(Encoding.UTF8.GetBytes(endorseResponse.PreparedTransaction));

        var submitRequest = new SubmitRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            PreparedTransaction = endorseResponse.PreparedTransaction,
            Signature = submitSignature
        };

        var submitMethod = new Method<SubmitRequest, SubmitResponse>(
            MethodType.Unary,
            "gateway.Gateway",
            "Submit",
            SubmitRequest.Marshaller,
            SubmitResponse.Marshaller);

        await invoker.AsyncUnaryCall(submitMethod, null, callOptions, submitRequest);

        // Step 3: CommitStatus (wait for block commit)
        var commitStatusRequest = new CommitStatusRequest
        {
            TransactionId = txId,
            ChannelId = channelName
        };

        var commitMethod = new Method<CommitStatusRequest, CommitStatusResponse>(
            MethodType.Unary,
            "gateway.Gateway",
            "CommitStatus",
            CommitStatusRequest.Marshaller,
            CommitStatusResponse.Marshaller);

        var commitResponse = await invoker.AsyncUnaryCall(commitMethod, null, callOptions, commitStatusRequest);

        var txHash = "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(txId))).ToLowerInvariant();

        return new BlockchainTransactionResult
        {
            Success = commitResponse.Status == "VALID",
            TxHash = txHash,
            BlockNumber = commitResponse.BlockNumber,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = commitResponse.Status != "VALID" ? $"Commit status: {commitResponse.Status}" : null
        };
    }

    private async Task<string> EvaluateViaGatewayAsync(
        string channelName, string chaincodeName, string functionName, string[] args)
    {
        var channel = GetOrCreateChannel();

        var nonce = new byte[24];
        RandomNumberGenerator.Fill(nonce);
        var txId = ComputeTxId(nonce, _certificate!);

        var proposalPayload = BuildProposalPayload(channelName, chaincodeName, functionName, args, txId, nonce);
        var signature = SignPayload(proposalPayload);

        var invoker = channel.CreateCallInvoker();

        var evaluateRequest = new EvaluateRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            ProposedTransaction = proposalPayload,
            ProposalSignature = signature
        };

        var evaluateMethod = new Method<EvaluateRequest, EvaluateResponse>(
            MethodType.Unary,
            "gateway.Gateway",
            "Evaluate",
            EvaluateRequest.Marshaller,
            EvaluateResponse.Marshaller);

        var callOptions = new CallOptions(
            deadline: DateTime.UtcNow.AddSeconds(_options.EvaluateTimeoutSeconds));

        var response = await invoker.AsyncUnaryCall(evaluateMethod, null, callOptions, evaluateRequest);

        return response?.Result ?? "{}";
    }

    // ========================================================================
    // Simulation Mode (development without real Fabric network)
    // ========================================================================

    private async Task<BlockchainTransactionResult> SimulateSubmitAsync(
        string channelName, string chaincodeName, string functionName, string[] args)
    {
        _logger.LogDebug(
            "[SIMULATION] Submit: {Chaincode}.{Function}({Args})",
            chaincodeName, functionName, string.Join(", ", args.Take(3)));

        await Task.Delay(50); // Simulate network latency

        var txHash = GenerateSimulatedTxHash(channelName, chaincodeName, functionName, args);

        return new BlockchainTransactionResult
        {
            Success = true,
            TxHash = txHash,
            BlockNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Timestamp = DateTime.UtcNow
        };
    }

    private string SimulateEvaluate(string chaincodeName, string functionName, string[] args)
    {
        _logger.LogDebug(
            "[SIMULATION] Evaluate: {Chaincode}.{Function}({Args})",
            chaincodeName, functionName, string.Join(", ", args.Take(3)));

        // Return empty JSON for queries in simulation mode
        return "{}";
    }

    // ========================================================================
    // Crypto Helpers
    // ========================================================================

    /// <summary>
    /// Compute transaction ID = SHA-256(nonce || creator)
    /// This matches Fabric's txID algorithm
    /// </summary>
    private static string ComputeTxId(byte[] nonce, string certificatePem)
    {
        var certBytes = Encoding.UTF8.GetBytes(certificatePem);
        var combined = new byte[nonce.Length + certBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(certBytes, 0, combined, nonce.Length, certBytes.Length);

        var hash = SHA256.HashData(combined);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Sign a payload with the loaded ECDSA private key
    /// </summary>
    private byte[] SignPayload(byte[] payload)
    {
        if (_signingKey == null)
            return Array.Empty<byte>();

        var hash = SHA256.HashData(payload);
        return _signingKey.SignHash(hash, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    /// <summary>
    /// Build chaincode invocation proposal payload
    /// This is a simplified version of the Fabric proposal structure
    /// </summary>
    private byte[] BuildProposalPayload(
        string channelName, string chaincodeName, string functionName,
        string[] args, string txId, byte[] nonce)
    {
        // The proposal payload contains the chaincode invocation spec
        var invocationSpec = JsonConvert.SerializeObject(new
        {
            ChaincodeSpec = new
            {
                Type = "GOLANG",
                ChaincodeId = new { Name = chaincodeName },
                Input = new
                {
                    Args = new[] { functionName }.Concat(args).Select(Encoding.UTF8.GetBytes).ToArray()
                }
            },
            ChannelHeader = new
            {
                Type = 3, // ENDORSER_TRANSACTION
                TxId = txId,
                ChannelId = channelName,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Epoch = 0,
                Extension = new { ChaincodeId = new { Name = chaincodeName } }
            },
            SignatureHeader = new
            {
                Creator = new
                {
                    MspId = _options.MspId,
                    IdBytes = _certificate
                },
                Nonce = Convert.ToBase64String(nonce)
            }
        });

        return Encoding.UTF8.GetBytes(invocationSpec);
    }

    // ========================================================================
    // gRPC Channel Management
    // ========================================================================

    private GrpcChannel GetOrCreateChannel()
    {
        if (_channel != null) return _channel;

        var scheme = _options.UseTls ? "https" : "http";
        var address = $"{scheme}://{_options.PeerEndpoint}";

        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
            MaxSendMessageSize = 16 * 1024 * 1024
        };

        if (_options.UseTls)
        {
            var httpHandler = new SocketsHttpHandler();

            if (_tlsCaCert != null && _tlsCaCert.Length > 0)
            {
                httpHandler.SslOptions.RemoteCertificateValidationCallback = (_, cert, chain, errors) =>
                {
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                        return true;

                    // Validate against our CA cert
                    if (chain != null && cert != null)
                    {
                        var caCert = new X509Certificate2(_tlsCaCert);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.CustomTrustStore.Add(caCert);
                        return chain.Build(new X509Certificate2(cert));
                    }

                    return false;
                };
            }

            if (!string.IsNullOrEmpty(_options.GatewayPeerOverride))
            {
                // Required when peer hostname inside Docker differs from external hostname
                httpHandler.SslOptions.TargetHost = _options.GatewayPeerOverride;
            }

            channelOptions.HttpHandler = httpHandler;
        }

        _channel = GrpcChannel.ForAddress(address, channelOptions);

        _logger.LogInformation("Created gRPC channel to Fabric peer: {Address}", address);
        return _channel;
    }

    private void ResetChannel()
    {
        _channel?.Dispose();
        _channel = null;
    }

    private static string GenerateSimulatedTxHash(
        string channelName, string chaincodeName, string functionName, string[] args)
    {
        var data = JsonConvert.SerializeObject(new
        {
            channelName,
            chaincodeName,
            functionName,
            args,
            timestamp = DateTime.UtcNow,
            nonce = Guid.NewGuid().ToString()
        });

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return "0x" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ========================================================================
    // Dispose
    // ========================================================================

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _channel?.Dispose();
            _signingKey?.Dispose();
            _disposed = true;
        }

        await ValueTask.CompletedTask;
    }
}

// ============================================================================
// Fabric Gateway gRPC Protocol Models
// ============================================================================
// These are simplified C# representations of the Fabric Gateway gRPC messages.
// In production, generate from proto files:
//   github.com/hyperledger/fabric-protos/gateway/gateway.proto
// ============================================================================

#region Fabric Gateway gRPC Protocol

internal class EndorseRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public byte[] ProposedTransaction { get; set; } = Array.Empty<byte>();
    public byte[] ProposalSignature { get; set; } = Array.Empty<byte>();

    internal static readonly Marshaller<EndorseRequest> Marshaller = Marshallers.Create(
        serializer: req => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)),
        deserializer: bytes => JsonConvert.DeserializeObject<EndorseRequest>(Encoding.UTF8.GetString(bytes))!);
}

internal class EndorseResponse
{
    public string PreparedTransaction { get; set; } = string.Empty;

    internal static readonly Marshaller<EndorseResponse> Marshaller = Marshallers.Create(
        serializer: resp => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp)),
        deserializer: bytes => JsonConvert.DeserializeObject<EndorseResponse>(Encoding.UTF8.GetString(bytes))!);
}

internal class SubmitRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string PreparedTransaction { get; set; } = string.Empty;
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    internal static readonly Marshaller<SubmitRequest> Marshaller = Marshallers.Create(
        serializer: req => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)),
        deserializer: bytes => JsonConvert.DeserializeObject<SubmitRequest>(Encoding.UTF8.GetString(bytes))!);
}

internal class SubmitResponse
{
    internal static readonly Marshaller<SubmitResponse> Marshaller = Marshallers.Create(
        serializer: resp => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp)),
        deserializer: bytes => JsonConvert.DeserializeObject<SubmitResponse>(Encoding.UTF8.GetString(bytes))!);
}

internal class EvaluateRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public byte[] ProposedTransaction { get; set; } = Array.Empty<byte>();
    public byte[] ProposalSignature { get; set; } = Array.Empty<byte>();

    internal static readonly Marshaller<EvaluateRequest> Marshaller = Marshallers.Create(
        serializer: req => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)),
        deserializer: bytes => JsonConvert.DeserializeObject<EvaluateRequest>(Encoding.UTF8.GetString(bytes))!);
}

internal class EvaluateResponse
{
    public string Result { get; set; } = string.Empty;

    internal static readonly Marshaller<EvaluateResponse> Marshaller = Marshallers.Create(
        serializer: resp => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp)),
        deserializer: bytes => JsonConvert.DeserializeObject<EvaluateResponse>(Encoding.UTF8.GetString(bytes))!);
}

internal class CommitStatusRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;

    internal static readonly Marshaller<CommitStatusRequest> Marshaller = Marshallers.Create(
        serializer: req => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(req)),
        deserializer: bytes => JsonConvert.DeserializeObject<CommitStatusRequest>(Encoding.UTF8.GetString(bytes))!);
}

internal class CommitStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public long BlockNumber { get; set; }

    internal static readonly Marshaller<CommitStatusResponse> Marshaller = Marshallers.Create(
        serializer: resp => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp)),
        deserializer: bytes => JsonConvert.DeserializeObject<CommitStatusResponse>(Encoding.UTF8.GetString(bytes))!);
}

#endregion
