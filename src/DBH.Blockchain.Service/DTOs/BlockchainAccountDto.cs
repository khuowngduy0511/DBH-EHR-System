namespace DBH.Blockchain.Service.DTOs;

/// <summary>
/// DTO for creating blockchain account
/// </summary>
public class CreateBlockchainAccountDto
{
    /// <summary>
    /// Enrollment ID (unique account identifier)
    /// </summary>
    public string EnrollmentId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Role or organizational unit
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Organization affiliation (optional)
    /// </summary>
    public string? Organization { get; set; }
}

/// <summary>
/// DTO for login to blockchain account
/// </summary>
public class BlockchainAccountLoginDto
{
    /// <summary>
    /// Enrollment ID
    /// </summary>
    public string EnrollmentId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Previously saved enrollment secret (for re-enrollment)
    /// </summary>
    public string? EnrollmentSecret { get; set; }
}

/// <summary>
/// Response DTO for account creation
/// </summary>
public class BlockchainAccountResponseDto
{
    public bool Success { get; set; }
    public string EnrollmentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Secret for re-enrollment - SAVE THIS SECURELY!
    /// </summary>
    public string? EnrollmentSecret { get; set; }

    /// <summary>
    /// MSP directory path where account credentials are stored (on Fabric peer)
    /// </summary>
    public string? AccountStoragePath { get; set; }

    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
