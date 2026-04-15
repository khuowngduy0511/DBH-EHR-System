namespace DBH.Shared.Contracts.Blockchain;

/// <summary>
/// Shared UTC+7 timestamp helpers for blockchain record payloads.
/// </summary>
public static class BlockchainTime
{
    private static readonly TimeSpan UtcPlus7Offset = TimeSpan.FromHours(7);

    public static DateTimeOffset Now => DateTimeOffset.UtcNow.ToOffset(UtcPlus7Offset);

    public static DateTime NowDateTime => Now.DateTime;

    public static string NowIsoString => Now.ToString("o");

    public static string FromUtcIsoString(DateTime utcDateTime)
    {
        var normalizedUtc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return new DateTimeOffset(normalizedUtc).ToOffset(UtcPlus7Offset).ToString("o");
    }
}