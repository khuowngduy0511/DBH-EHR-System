namespace DBH.Blockchain.Service.DTOs;

/// <summary>
/// DTO for logging errors to blockchain
/// </summary>
public class ErrorLogDto
{
    /// <summary>
    /// Error ID (unique identifier)
    /// </summary>
    public string ErrorId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Service name where error occurred
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error details/stack trace
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// User ID associated with the error (if applicable)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Error severity level (Info, Warning, Error, Critical)
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Additional context data (JSON)
    /// </summary>
    public Dictionary<string, string>? Context { get; set; }
}

/// <summary>
/// Response DTO for error log creation
/// </summary>
public class ErrorLogResponseDto
{
    public bool Success { get; set; }
    public string ErrorId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
