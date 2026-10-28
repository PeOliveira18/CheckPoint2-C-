using System;
using System.Text.Json;

namespace ExpenseHub.Core.Messaging;

internal sealed record ReportMessage(Guid ReportId)
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, _options);

    public static ReportMessage? TryParse(ReadOnlyMemory<byte> body)
    {
        try
        {
            ReportMessage? message = JsonSerializer.Deserialize<ReportMessage>(body.Span, _options);
            return message is null || message.ReportId == Guid.Empty ? null : message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
