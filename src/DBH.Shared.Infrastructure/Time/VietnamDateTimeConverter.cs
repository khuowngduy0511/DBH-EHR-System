using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBH.Shared.Infrastructure.Time;

/// <summary>
/// Chuyển đổi DateTime sang giờ Việt Nam (UTC+7) khi serialize JSON.
/// DB lưu UTC, nhưng response API luôn trả về giờ VN dạng ISO 8601 với offset +07:00.
/// </summary>
public class VietnamDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        var vn = new DateTimeOffset(utc).ToOffset(VnOffset);
        writer.WriteStringValue(vn.ToString("yyyy-MM-ddTHH:mm:ss+07:00"));
    }
}

/// <summary>
/// Nullable wrapper — System.Text.Json tự xử lý null, class này để tường minh.
/// </summary>
public class VietnamNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly VietnamDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }
        Inner.Write(writer, value.Value, options);
    }
}
