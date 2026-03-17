namespace DBH.Shared.Infrastructure.Blockchain;

/// <summary>
/// Configuration for Hyperledger Fabric Certificate Authority (CA) enrollment.
/// Used to register and enroll new user identities at registration time.
/// </summary>
public class FabricCaOptions
{
    public const string SectionName = "FabricCA";

    /// <summary>Enables CA enrollment on user registration. When false, enrollment is skipped silently.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Fabric CA URL, e.g. https://localhost:7054</summary>
    public string CaUrl { get; set; } = "https://localhost:7054";

    /// <summary>CA name as registered in the network, e.g. ca-hospital1</summary>
    public string CaName { get; set; } = "ca-hospital1";

    /// <summary>Admin enrollment certificate path (PEM file). Used to authenticate register requests.</summary>
    public string AdminCertPath { get; set; } = string.Empty;

    /// <summary>Admin private key path (PEM file). Used to sign register request tokens.</summary>
    public string AdminKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Admin private key directory (keystore/).
    /// Fabric CA stores keys with random names; if AdminKeyPath is empty, searches this directory for *_sk or *.pem.
    /// </summary>
    public string? AdminKeyDirectory { get; set; }

    /// <summary>Path to the TLS CA certificate for verifying the CA HTTPS connection.</summary>
    public string TlsCertPath { get; set; } = string.Empty;

    /// <summary>Default affiliation assigned to newly registered users, e.g. org1.department1</summary>
    public string DefaultAffiliation { get; set; } = "org1.department1";
}
