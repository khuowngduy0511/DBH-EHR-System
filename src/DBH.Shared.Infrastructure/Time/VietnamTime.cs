namespace DBH.Shared.Infrastructure.Time;

public static class VietnamTime
{
    public static readonly TimeZoneInfo TimeZone = ResolveVietnamTimeZone();

    // Keep private alias for internal compat
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZone;

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    public static DateTime DatabaseNow => DateTime.SpecifyKind(Now, DateTimeKind.Utc);

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