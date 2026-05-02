namespace DBH.Shared.Infrastructure.Time;

public static class VietnamTime
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    /// <summary>
    /// Thời gian hiện tại theo múi giờ Việt Nam (UTC+7) — dùng cho business logic (hiển thị, so sánh, expiry).
    /// KHÔNG dùng để lưu vào DB.
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    /// <summary>
    /// UTC thực sự — dùng để lưu timestamp vào DB (timestamptz).
    /// FE nhận UTC và tự convert sang giờ Việt Nam khi hiển thị.
    /// </summary>
    public static DateTime DatabaseNow => DateTime.UtcNow;

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}