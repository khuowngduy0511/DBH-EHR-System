using System.Text.RegularExpressions;

namespace DBH.Notification.Service.Helpers;

/// <summary>
/// Parses User-Agent header to extract device information
/// </summary>
public static class UserAgentParser
{
    public static DeviceInfo Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return new DeviceInfo { DeviceType = "unknown", DeviceName = "Unknown" };

        var info = new DeviceInfo();

        // Detect device type and OS
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
        {
            info.DeviceType = "ios";
            info.DeviceName = ExtractMatch(userAgent, @"(iPhone|iPad|iPod)") ?? "iOS Device";
            info.OsVersion = ExtractMatch(userAgent, @"(?:iPhone|CPU) OS (\d+[_\.]\d+(?:[_\.]\d+)?)");
            info.OsVersion = info.OsVersion?.Replace('_', '.');
        }
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            info.DeviceType = "android";
            info.OsVersion = ExtractMatch(userAgent, @"Android (\d+[\.\d]*)");
            // Extract device model: typically after "Android x.x; " and before ")"
            info.DeviceName = ExtractMatch(userAgent, @"Android [\d\.]+;\s*([^;)]+)");
            info.DeviceName = info.DeviceName?.Trim() ?? "Android Device";
        }
        else
        {
            info.DeviceType = "web";
            info.DeviceName = DetectBrowser(userAgent);
            info.OsVersion = DetectOsVersion(userAgent);
        }

        // Extract app version from custom format: "AppName/x.x.x"
        info.AppVersion = ExtractMatch(userAgent, @"DBH[/-]?EHR[/-]?(?:App)?[/ ](\d+[\.\d]*)");

        return info;
    }

    private static string DetectBrowser(string userAgent)
    {
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) &&
            !userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            return "Firefox";
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) &&
            !userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            return "Safari";
        return "Web Browser";
    }

    private static string? DetectOsVersion(string userAgent)
    {
        // Windows
        var match = ExtractMatch(userAgent, @"Windows NT ([\d\.]+)");
        if (match != null)
        {
            return match switch
            {
                "10.0" => "Windows 10/11",
                "6.3" => "Windows 8.1",
                "6.2" => "Windows 8",
                "6.1" => "Windows 7",
                _ => $"Windows NT {match}"
            };
        }

        // macOS
        match = ExtractMatch(userAgent, @"Mac OS X (\d+[_\.]\d+(?:[_\.]\d+)?)");
        if (match != null) return "macOS " + match.Replace('_', '.');

        // Linux
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux";

        return null;
    }

    private static string? ExtractMatch(string input, string pattern)
    {
        var match = Regex.Match(input, pattern);
        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : null;
    }
}

public class DeviceInfo
{
    public string DeviceType { get; set; } = "unknown";
    public string? DeviceName { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
}
