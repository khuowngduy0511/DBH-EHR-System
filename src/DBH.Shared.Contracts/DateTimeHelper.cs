namespace DBH.Shared.Contracts;

/// <summary>
/// Helper tập trung cho timezone Việt Nam (UTC+7).
/// Sử dụng VietnamTimeHelper.Now thay cho DateTime.UtcNow ở tất cả business timestamp.
/// </summary>
public static class VietnamTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    /// <summary>
    /// Thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    /// <summary>
    /// Ngày hiện tại theo múi giờ Việt Nam
    /// </summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
