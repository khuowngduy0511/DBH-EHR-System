namespace DBH.Blockchain.Service.DTOs;

public class EhrVerifyRequestDto
{
    public string EhrId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string CurrentHash { get; set; } = string.Empty;
}

public class ConsentRevokeRequestDto
{
    public string ConsentId { get; set; } = string.Empty;
    public string? RevokedAt { get; set; }
    public string? Reason { get; set; }
}