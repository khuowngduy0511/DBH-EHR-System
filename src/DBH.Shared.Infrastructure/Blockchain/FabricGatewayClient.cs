using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DBH.Shared.Contracts.Blockchain;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ProtoBuf;

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
    private readonly IFabricRuntimeIdentityResolver _identityResolver;
    private readonly ILogger<FabricGatewayClient> _logger;
    private GrpcChannel? _channel;
    private ECDsa? _signingKey;
    private string? _certificate;
    private byte[]? _tlsCaCert;
    private string _activeMspId;
    private string _activePeerEndpoint;
    private string? _activeGatewayPeerOverride;
    private bool _activeUseTls;
    private string? _activeIdentityKey;
    private bool _cryptoLoaded;
    private bool _disposed;

    public FabricGatewayClient(
        IOptions<FabricOptions> options,
        IFabricRuntimeIdentityResolver identityResolver,
        ILogger<FabricGatewayClient> logger)
    {
        _options = options.Value;
        _identityResolver = identityResolver;
        _logger = logger;
        _activeMspId = _options.MspId;
        _activePeerEndpoint = _options.PeerEndpoint;
        _activeGatewayPeerOverride = _options.GatewayPeerOverride;
        _activeUseTls = _options.UseTls;
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

                var runtimeIdentity = await _identityResolver.ResolveForCurrentContextAsync();
                await EnsureCryptoLoadedAsync(runtimeIdentity);

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

            var runtimeIdentity = await _identityResolver.ResolveForCurrentContextAsync();
            await EnsureCryptoLoadedAsync(runtimeIdentity);

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

            var runtimeIdentity = await _identityResolver.ResolveForCurrentContextAsync();
            await EnsureCryptoLoadedAsync(runtimeIdentity);
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

    private async Task EnsureCryptoLoadedAsync(FabricRuntimeIdentity runtimeIdentity)
    {
        if (_activeIdentityKey != runtimeIdentity.IdentityKey)
        {
            _signingKey?.Dispose();
            _signingKey = null;
            _certificate = null;
            _tlsCaCert = null;
            _cryptoLoaded = false;
            _activeIdentityKey = runtimeIdentity.IdentityKey;
            _activeMspId = runtimeIdentity.MspId;
            _activePeerEndpoint = runtimeIdentity.PeerEndpoint;
            _activeGatewayPeerOverride = runtimeIdentity.GatewayPeerOverride;
            _activeUseTls = runtimeIdentity.UseTls;
            ResetChannel();
        }

        if (_cryptoLoaded) return;

        try
        {
            // Load signing key
            if (!string.IsNullOrEmpty(runtimeIdentity.PrivateKeyPath) && File.Exists(runtimeIdentity.PrivateKeyPath))
            {
                var keyPem = await File.ReadAllTextAsync(runtimeIdentity.PrivateKeyPath);
                _signingKey = ECDsa.Create();
                _signingKey.ImportFromPem(keyPem);
                _logger.LogInformation("Loaded signing key from {Path}", runtimeIdentity.PrivateKeyPath);
            }
            else if (!string.IsNullOrEmpty(runtimeIdentity.PrivateKeyDirectory) && Directory.Exists(runtimeIdentity.PrivateKeyDirectory))
            {
                // Fabric CA stores keys with random names in keystore/
                var keyFiles = Directory.GetFiles(runtimeIdentity.PrivateKeyDirectory, "*_sk");
                if (keyFiles.Length == 0)
                    keyFiles = Directory.GetFiles(runtimeIdentity.PrivateKeyDirectory, "*.pem");

                if (keyFiles.Length > 0)
                {
                    var keyPem = await File.ReadAllTextAsync(keyFiles[0]);
                    _signingKey = ECDsa.Create();
                    _signingKey.ImportFromPem(keyPem);
                    _logger.LogInformation("Loaded signing key from {Path}", keyFiles[0]);
                }
            }

            // Load certificate
            if (!string.IsNullOrEmpty(runtimeIdentity.CertificatePath) && File.Exists(runtimeIdentity.CertificatePath))
            {
                _certificate = await File.ReadAllTextAsync(runtimeIdentity.CertificatePath);
                _logger.LogInformation("Loaded certificate from {Path}", runtimeIdentity.CertificatePath);
            }

            // Load TLS CA cert
            if (!string.IsNullOrEmpty(runtimeIdentity.GatewayTlsCertificatePath) && File.Exists(runtimeIdentity.GatewayTlsCertificatePath))
            {
                _tlsCaCert = await File.ReadAllBytesAsync(runtimeIdentity.GatewayTlsCertificatePath);
                _logger.LogInformation("Loaded TLS CA cert from {Path}", runtimeIdentity.GatewayTlsCertificatePath);
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

        var nonce = new byte[24];
        RandomNumberGenerator.Fill(nonce);
        var creatorIdentity = GetSerializedIdentity();
        var txId = ComputeTxId(nonce, creatorIdentity);
        var signedProposal = BuildSignedProposal(channelName, chaincodeName, functionName, args, txId, nonce, creatorIdentity);

        // Use the Fabric Gateway gRPC service
        // The gateway.Gateway service has: Evaluate, Endorse, Submit, CommitStatus
        var invoker = channel.CreateCallInvoker();

        // Step 1: Endorse
        var endorseRequest = new EndorseRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            ProposedTransaction = signedProposal
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

        if (endorseResponse == null || endorseResponse.PreparedTransaction == null)
        {
            return new BlockchainTransactionResult
            {
                Success = false,
                ErrorMessage = "Endorsement failed: no prepared transaction returned",
                Timestamp = DateTime.UtcNow
            };
        }

        // Step 2: Submit (send to orderer)
        var submitSignature = SignPayload(endorseResponse.PreparedTransaction.Payload);
        endorseResponse.PreparedTransaction.Signature = submitSignature;

        var submitRequest = new SubmitRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            PreparedTransaction = endorseResponse.PreparedTransaction
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
            ChannelId = channelName,
            Identity = creatorIdentity
        };

        var commitStatusRequestBytes = SerializeProto(commitStatusRequest);
        var signedCommitStatusRequest = new SignedCommitStatusRequest
        {
            Request = commitStatusRequestBytes,
            Signature = SignPayload(commitStatusRequestBytes)
        };

        var commitMethod = new Method<SignedCommitStatusRequest, CommitStatusResponse>(
            MethodType.Unary,
            "gateway.Gateway",
            "CommitStatus",
            SignedCommitStatusRequest.Marshaller,
            CommitStatusResponse.Marshaller);

        var commitResponse = await invoker.AsyncUnaryCall(commitMethod, null, callOptions, signedCommitStatusRequest);

        var txHash = "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(txId))).ToLowerInvariant();

        return new BlockchainTransactionResult
        {
            Success = commitResponse.Result == TxValidationCode.Valid,
            TxHash = txHash,
            BlockNumber = (long)commitResponse.BlockNumber,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = commitResponse.Result != TxValidationCode.Valid ? $"Commit status: {commitResponse.Result}" : null
        };
    }

    private async Task<string> EvaluateViaGatewayAsync(
        string channelName, string chaincodeName, string functionName, string[] args)
    {
        var channel = GetOrCreateChannel();

        var nonce = new byte[24];
        RandomNumberGenerator.Fill(nonce);
        var creatorIdentity = GetSerializedIdentity();
        var txId = ComputeTxId(nonce, creatorIdentity);
        var signedProposal = BuildSignedProposal(channelName, chaincodeName, functionName, args, txId, nonce, creatorIdentity);

        var invoker = channel.CreateCallInvoker();

        var evaluateRequest = new EvaluateRequest
        {
            TransactionId = txId,
            ChannelId = channelName,
            ProposedTransaction = signedProposal
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

        if (response?.Result?.Payload == null || response.Result.Payload.Length == 0)
        {
            return "{}";
        }

        return Encoding.UTF8.GetString(response.Result.Payload);
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
    /// where creator is the serialized msp.SerializedIdentity bytes.
    /// </summary>
    private static string ComputeTxId(byte[] nonce, byte[] serializedIdentity)
    {
        var combined = new byte[nonce.Length + serializedIdentity.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(serializedIdentity, 0, combined, nonce.Length, serializedIdentity.Length);

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
        var signature = _signingKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);
        return NormalizeEcdsaSignatureLowS(signature, _signingKey.KeySize);
    }

    private static byte[] NormalizeEcdsaSignatureLowS(byte[] derSignature, int keySizeBits)
    {
        var curveOrder = GetCurveOrder(keySizeBits);
        if (curveOrder <= BigInteger.Zero)
            return derSignature;

        try
        {
            var reader = new AsnReader(derSignature, AsnEncodingRules.DER);
            var sequence = reader.ReadSequence();
            var rBytes = sequence.ReadIntegerBytes().ToArray();
            var sBytes = sequence.ReadIntegerBytes().ToArray();
            sequence.ThrowIfNotEmpty();
            reader.ThrowIfNotEmpty();

            var r = new BigInteger(rBytes, isUnsigned: true, isBigEndian: true);
            var s = new BigInteger(sBytes, isUnsigned: true, isBigEndian: true);

            var halfOrder = curveOrder >> 1;
            if (s <= halfOrder)
                return derSignature;

            var lowS = curveOrder - s;

            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteInteger(r);
            writer.WriteInteger(lowS);
            writer.PopSequence();
            return writer.Encode();
        }
        catch
        {
            // If signature parsing fails unexpectedly, keep original signature.
            return derSignature;
        }
    }

    private static BigInteger GetCurveOrder(int keySizeBits)
    {
        // NIST P-256
        if (keySizeBits <= 256)
            return ParseUnsignedHex("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551");

        // NIST P-384
        if (keySizeBits <= 384)
            return ParseUnsignedHex("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC7634D81F4372DDF581A0DB248B0A77AECEC196ACCC52973");

        // NIST P-521
        return ParseUnsignedHex("01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
    }

    private static BigInteger ParseUnsignedHex(string hex)
    {
        return new BigInteger(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
    }

    private byte[] GetSerializedIdentity()
    {
        if (string.IsNullOrWhiteSpace(_certificate))
        {
            throw new InvalidOperationException("Certificate is not loaded.");
        }

        var identity = new SerializedIdentity
        {
            MspId = _activeMspId,
            IdBytes = Encoding.UTF8.GetBytes(_certificate)
        };

        return SerializeProto(identity);
    }

    private SignedProposal BuildSignedProposal(
        string channelName,
        string chaincodeName,
        string functionName,
        string[] args,
        string txId,
        byte[] nonce,
        byte[] serializedIdentity)
    {
        var signatureHeader = new SignatureHeader
        {
            Creator = serializedIdentity,
            Nonce = nonce
        };

        var channelHeader = new ChannelHeader
        {
            Type = (int)HeaderType.EndorserTransaction,
            Version = 0,
            Timestamp = ProtoTimestamp.FromDateTime(DateTime.UtcNow),
            ChannelId = channelName,
            TxId = txId,
            Epoch = 0,
            Extension = SerializeProto(new ChaincodeHeaderExtension
            {
                ChaincodeId = new ChaincodeId { Name = chaincodeName }
            })
        };

        var header = new Header
        {
            ChannelHeader = SerializeProto(channelHeader),
            SignatureHeader = SerializeProto(signatureHeader)
        };

        var invocationSpec = new ChaincodeInvocationSpec
        {
            ChaincodeSpec = new ChaincodeSpec
            {
                Type = ChaincodeSpecType.Node,
                ChaincodeId = new ChaincodeId { Name = chaincodeName },
                Input = new ChaincodeInput
                {
                    Args = new[] { functionName }.Concat(args).Select(Encoding.UTF8.GetBytes).ToList()
                }
            }
        };

        var payload = new ChaincodeProposalPayload
        {
            Input = SerializeProto(invocationSpec)
        };

        var proposal = new Proposal
        {
            Header = SerializeProto(header),
            Payload = SerializeProto(payload)
        };

        var proposalBytes = SerializeProto(proposal);

        return new SignedProposal
        {
            ProposalBytes = proposalBytes,
            Signature = SignPayload(proposalBytes)
        };
    }

    private static byte[] SerializeProto<T>(T value)
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, value);
        return ms.ToArray();
    }

    // ========================================================================
    // gRPC Channel Management
    // ========================================================================

    private GrpcChannel GetOrCreateChannel()
    {
        if (_channel != null) return _channel;

        var scheme = _activeUseTls ? "https" : "http";
        var address = $"{scheme}://{_activePeerEndpoint}";

        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
            MaxSendMessageSize = 16 * 1024 * 1024
        };

        if (_activeUseTls)
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

            if (!string.IsNullOrEmpty(_activeGatewayPeerOverride))
            {
                // Required when peer hostname inside Docker differs from external hostname
                httpHandler.SslOptions.TargetHost = _activeGatewayPeerOverride;
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
// Protobuf-net contracts aligned with:
//   github.com/hyperledger/fabric-protos
// ============================================================================

#region Fabric Gateway gRPC Protocol

internal static class ProtobufMarshaller
{
    public static Marshaller<T> Create<T>() where T : class, new() =>
        Marshallers.Create(
            serializer: value =>
            {
                using var ms = new MemoryStream();
                Serializer.Serialize(ms, value);
                return ms.ToArray();
            },
            deserializer: data =>
            {
                using var ms = new MemoryStream(data);
                return Serializer.Deserialize<T>(ms);
            });
}

[ProtoContract]
internal class EndorseRequest
{
    [ProtoMember(1)] public string TransactionId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ChannelId { get; set; } = string.Empty;
    [ProtoMember(3)] public SignedProposal ProposedTransaction { get; set; } = new();
    [ProtoMember(4)] public List<string> EndorsingOrganizations { get; set; } = [];

    internal static readonly Marshaller<EndorseRequest> Marshaller = ProtobufMarshaller.Create<EndorseRequest>();
}

[ProtoContract]
internal class EndorseResponse
{
    [ProtoMember(1)] public Envelope PreparedTransaction { get; set; } = new();

    internal static readonly Marshaller<EndorseResponse> Marshaller = ProtobufMarshaller.Create<EndorseResponse>();
}

[ProtoContract]
internal class SubmitRequest
{
    [ProtoMember(1)] public string TransactionId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ChannelId { get; set; } = string.Empty;
    [ProtoMember(3)] public Envelope PreparedTransaction { get; set; } = new();

    internal static readonly Marshaller<SubmitRequest> Marshaller = ProtobufMarshaller.Create<SubmitRequest>();
}

[ProtoContract]
internal class SubmitResponse
{
    internal static readonly Marshaller<SubmitResponse> Marshaller = ProtobufMarshaller.Create<SubmitResponse>();
}

[ProtoContract]
internal class EvaluateRequest
{
    [ProtoMember(1)] public string TransactionId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ChannelId { get; set; } = string.Empty;
    [ProtoMember(3)] public SignedProposal ProposedTransaction { get; set; } = new();
    [ProtoMember(4)] public List<string> TargetOrganizations { get; set; } = [];

    internal static readonly Marshaller<EvaluateRequest> Marshaller = ProtobufMarshaller.Create<EvaluateRequest>();
}

[ProtoContract]
internal class EvaluateResponse
{
    [ProtoMember(1)] public PeerResponse Result { get; set; } = new();

    internal static readonly Marshaller<EvaluateResponse> Marshaller = ProtobufMarshaller.Create<EvaluateResponse>();
}

[ProtoContract]
internal class CommitStatusRequest
{
    [ProtoMember(1)] public string TransactionId { get; set; } = string.Empty;
    [ProtoMember(2)] public string ChannelId { get; set; } = string.Empty;
    [ProtoMember(3)] public byte[] Identity { get; set; } = Array.Empty<byte>();

    internal static readonly Marshaller<CommitStatusRequest> Marshaller = ProtobufMarshaller.Create<CommitStatusRequest>();
}

[ProtoContract]
internal class SignedCommitStatusRequest
{
    [ProtoMember(1)] public byte[] Request { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] Signature { get; set; } = Array.Empty<byte>();

    internal static readonly Marshaller<SignedCommitStatusRequest> Marshaller = ProtobufMarshaller.Create<SignedCommitStatusRequest>();
}

[ProtoContract]
internal class CommitStatusResponse
{
    [ProtoMember(1)] public TxValidationCode Result { get; set; }
    [ProtoMember(2)] public ulong BlockNumber { get; set; }

    internal static readonly Marshaller<CommitStatusResponse> Marshaller = ProtobufMarshaller.Create<CommitStatusResponse>();
}

[ProtoContract]
internal class SignedProposal
{
    [ProtoMember(1)] public byte[] ProposalBytes { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] Signature { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class Proposal
{
    [ProtoMember(1)] public byte[] Header { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] Payload { get; set; } = Array.Empty<byte>();
    [ProtoMember(3)] public byte[] Extension { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class Header
{
    [ProtoMember(1)] public byte[] ChannelHeader { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] SignatureHeader { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class ChannelHeader
{
    [ProtoMember(1)] public int Type { get; set; }
    [ProtoMember(2)] public int Version { get; set; }
    [ProtoMember(3)] public ProtoTimestamp Timestamp { get; set; } = new();
    [ProtoMember(4)] public string ChannelId { get; set; } = string.Empty;
    [ProtoMember(5)] public string TxId { get; set; } = string.Empty;
    [ProtoMember(6)] public ulong Epoch { get; set; }
    [ProtoMember(7)] public byte[] Extension { get; set; } = Array.Empty<byte>();
    [ProtoMember(8)] public byte[] TlsCertHash { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class SignatureHeader
{
    [ProtoMember(1)] public byte[] Creator { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] Nonce { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class ChaincodeHeaderExtension
{
    [ProtoMember(2)] public ChaincodeId ChaincodeId { get; set; } = new();
}

[ProtoContract]
internal class ChaincodeProposalPayload
{
    [ProtoMember(1)] public byte[] Input { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class ChaincodeInvocationSpec
{
    [ProtoMember(1)] public ChaincodeSpec ChaincodeSpec { get; set; } = new();
}

[ProtoContract]
internal class ChaincodeSpec
{
    [ProtoMember(1)] public ChaincodeSpecType Type { get; set; }
    [ProtoMember(2)] public ChaincodeId ChaincodeId { get; set; } = new();
    [ProtoMember(3)] public ChaincodeInput Input { get; set; } = new();
    [ProtoMember(4)] public int Timeout { get; set; }
}

[ProtoContract]
internal class ChaincodeId
{
    [ProtoMember(1)] public string Path { get; set; } = string.Empty;
    [ProtoMember(2)] public string Name { get; set; } = string.Empty;
    [ProtoMember(3)] public string Version { get; set; } = string.Empty;
}

[ProtoContract]
internal class ChaincodeInput
{
    [ProtoMember(1)] public List<byte[]> Args { get; set; } = [];
    [ProtoMember(3)] public bool IsInit { get; set; }
}

[ProtoContract]
internal class SerializedIdentity
{
    [ProtoMember(1)] public string MspId { get; set; } = string.Empty;
    [ProtoMember(2)] public byte[] IdBytes { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class Envelope
{
    [ProtoMember(1)] public byte[] Payload { get; set; } = Array.Empty<byte>();
    [ProtoMember(2)] public byte[] Signature { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class PeerResponse
{
    [ProtoMember(1)] public int Status { get; set; }
    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
    [ProtoMember(3)] public byte[] Payload { get; set; } = Array.Empty<byte>();
}

[ProtoContract]
internal class ProtoTimestamp
{
    [ProtoMember(1)] public long Seconds { get; set; }
    [ProtoMember(2)] public int Nanos { get; set; }

    public static ProtoTimestamp FromDateTime(DateTime utc)
    {
        var dto = new DateTimeOffset(utc.ToUniversalTime());
        var seconds = dto.ToUnixTimeSeconds();
        var nanos = (int)((dto - DateTimeOffset.FromUnixTimeSeconds(seconds)).Ticks * 100);
        return new ProtoTimestamp
        {
            Seconds = seconds,
            Nanos = nanos
        };
    }
}

internal enum HeaderType
{
    EndorserTransaction = 3
}

internal enum ChaincodeSpecType
{
    Undefined = 0,
    Golang = 1,
    Node = 2,
    Car = 3,
    Java = 4
}

internal enum TxValidationCode
{
    Valid = 0,
    NotValidated = 254,
    InvalidOtherReason = 255
}

#endregion
