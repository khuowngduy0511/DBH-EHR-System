namespace DBH.Shared.Infrastructure.Blockchain;

/// <summary>
/// Cấu hình kết nối Hyperledger Fabric Network
/// </summary>
public class FabricOptions
{
    public const string SectionName = "HyperledgerFabric";

    /// <summary>Bật/tắt blockchain integration</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Fabric Gateway peer endpoint (e.g., localhost:7051)</summary>
    public string PeerEndpoint { get; set; } = "localhost:7051";

    /// <summary>Override TLS hostname cho peer</summary>
    public string? GatewayPeerOverride { get; set; }

    /// <summary>MSP ID của organization (e.g., Org1MSP)</summary>
    public string MspId { get; set; } = "Org1MSP";

    /// <summary>Path đến certificate PEM file</summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>Path đến private key PEM file</summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>Path đến TLS CA certificate</summary>
    public string TlsCertificatePath { get; set; } = string.Empty;

    /// <summary>Channel mặc định</summary>
    public string DefaultChannel { get; set; } = "ehr-channel";

    /// <summary>Timeout cho submit transaction (seconds)</summary>
    public int SubmitTimeoutSeconds { get; set; } = 30;

    /// <summary>Timeout cho evaluate transaction (seconds)</summary>
    public int EvaluateTimeoutSeconds { get; set; } = 10;

    /// <summary>Số lần retry khi transaction fail</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Delay giữa các retry (milliseconds)</summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>Sử dụng TLS</summary>
    public bool UseTls { get; set; } = true;
}
