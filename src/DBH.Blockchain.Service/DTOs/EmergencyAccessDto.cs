namespace DBH.Blockchain.Service.DTOs;

/// <summary>
/// DTO for emergency access request
/// </summary>
public class EmergencyAccessRequestDto
{
    /// <summary>
    /// Target EHR record DID
    /// </summary>
    public string TargetRecordDid { get; set; } = string.Empty;

    /// <summary>
    /// Accessor DID (person performing emergency access)
    /// </summary>
    public string AccessorDid { get; set; } = string.Empty;

    /// <summary>
    /// Accessor organization
    /// </summary>
    public string AccessorOrg { get; set; } = string.Empty;

    /// <summary>
    /// Reason for emergency access
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for emergency access
/// </summary>
public class EmergencyAccessResponseDto
{
    public bool Success { get; set; }
    public string LogId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for querying emergency access logs
/// </summary>
public class EmergencyAccessLogDto
{
    public string LogId { get; set; } = string.Empty;
    public string TargetRecordDid { get; set; } = string.Empty;
    public string AccessorDid { get; set; } = string.Empty;
    public string AccessorOrg { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Query filters for emergency access logs
/// </summary>
public class EmergencyAccessQueryFilterDto
{
    public string? TargetRecordDid { get; set; }
    public string? AccessorDid { get; set; }
    public int PageNo { get; set; } = 0;
    public int PageSize { get; set; } = 10;
}
