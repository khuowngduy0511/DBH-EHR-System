namespace DBH.Shared.Contracts;

/// <summary>
/// Helper tập trung cho timezone Việt Nam (UTC+7).
/// </summary>
public static class VietnamTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    /// <summary>
    /// UTC thực sự — dùng để lưu timestamp vào DB (timestamptz).
    /// FE nhận UTC và tự convert sang giờ Việt Nam (UTC+7) khi hiển thị.
    /// </summary>
    public static DateTime Now => DateTime.UtcNow;

    /// <summary>
    /// Ngày hiện tại theo múi giờ Việt Nam (UTC+7).
    /// </summary>
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone));
}
