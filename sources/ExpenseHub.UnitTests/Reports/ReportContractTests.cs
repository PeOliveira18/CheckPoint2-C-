using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — formato JSON do envelope de relatório, com as mesmas opções de serialização da API.
/// </summary>
[TestClass]
public sealed class ReportContractTests
{
    private static readonly JsonSerializerOptions _apiOptions = CreateApiOptions();

    private static readonly string[] _envelopeFields =
        ["id", "scope", "fromDate", "toDate", "status", "createdAt", "startedAt", "completedAt", "result", "error"];

    /// <summary>Envelope Pending traz os campos do contrato, escopo e estado como texto e datas ano-mês-dia.</summary>
    [TestMethod]
    public void PendingEnvelopeMatchesContract()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);

        using JsonDocument json = Serialize(ReportResponse.From(report));
        JsonElement root = json.RootElement;

        CollectionAssert.AreEquivalent(_envelopeFields, root.EnumerateObject().Select(property => property.Name).ToList());
        Assert.AreEqual("Mine", root.GetProperty("scope").GetString());
        Assert.AreEqual("Pending", root.GetProperty("status").GetString());
        Assert.AreEqual("2026-09-01", root.GetProperty("fromDate").GetString());
        Assert.AreEqual("2026-09-30", root.GetProperty("toDate").GetString());
        Assert.EndsWith("Z", root.GetProperty("createdAt").GetString()!);
        Assert.AreEqual(JsonValueKind.Null, root.GetProperty("result").ValueKind);
        Assert.AreEqual(JsonValueKind.Null, root.GetProperty("error").ValueKind);
    }

    /// <summary>Resultado concluído traz generatedAt, totais e os cinco estados com quantidade e valor.</summary>
    [TestMethod]
    public void CompletedResultMatchesContract()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        ReportResult result = ReportCalculator.Calculate(report, ReportScenario.ApostilaExpenses(), FixedTimeProvider.DefaultNow.UtcDateTime);
        report.Status = ReportStatus.Completed;
        report.CompletedAtUtc = FixedTimeProvider.DefaultNow.UtcDateTime;
        report.ResultJson = ReportResultJson.Serialize(result);

        using JsonDocument json = Serialize(ReportResponse.From(report));
        JsonElement payload = json.RootElement.GetProperty("result");

        Assert.AreEqual(3, payload.GetProperty("totalCount").GetInt32());
        Assert.AreEqual(175.00m, payload.GetProperty("totalAmount").GetDecimal());
        Assert.EndsWith("Z", payload.GetProperty("generatedAt").GetString()!);
        JsonElement approved = payload.GetProperty("byStatus").EnumerateArray()
            .Single(item => item.GetProperty("status").GetString() == "Approved");
        Assert.AreEqual(2, approved.GetProperty("count").GetInt32());
        Assert.AreEqual(150.00m, approved.GetProperty("amount").GetDecimal());
        Assert.AreEqual(5, payload.GetProperty("byStatus").GetArrayLength());
    }

    private static JsonDocument Serialize(ReportResponse response) =>
        JsonDocument.Parse(JsonSerializer.Serialize(response, _apiOptions));

    private static JsonSerializerOptions CreateApiOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
