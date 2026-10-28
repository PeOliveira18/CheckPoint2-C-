using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseHub.Core.Reports;

internal static class ReportResultJson
{
    private static readonly JsonSerializerOptions _options = CreateOptions();

    public static string Serialize(ReportResult result) => JsonSerializer.Serialize(result, _options);

    public static ReportResult Deserialize(string json) =>
        JsonSerializer.Deserialize<ReportResult>(json, _options) ?? throw new JsonException("Resultado do relatório vazio.");

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
