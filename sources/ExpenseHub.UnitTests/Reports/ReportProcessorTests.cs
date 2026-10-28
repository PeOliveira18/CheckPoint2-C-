using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — processamento da solicitação: estados, resultado salvo e instante de geração.
/// </summary>
[TestClass]
public sealed class ReportProcessorTests
{
    private FakeReportRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private ReportProcessor _processor = null!;

    /// <summary>Cria um processador novo com os reembolsos do exemplo da apostila.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeReportRepository();
        _clock = new FixedTimeProvider();
        _processor = new ReportProcessor(_repository, _clock);
        foreach (Expense expense in ReportScenario.ApostilaExpenses())
        {
            _repository.SeedExpense(expense);
        }
    }

    /// <summary>Pending vira Completed com resultado, generatedAt, startedAt e completedAt salvos.</summary>
    [TestMethod]
    public async Task PendingReportIsCompletedWithSavedResult()
    {
        ReportRequest report = Seed(ReportScope.Mine);

        ReportProcessingOutcome outcome = await _processor.ProcessAsync(report.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.Completed, outcome);
        ReportRequest stored = _repository.Stored(report.Id);
        DateTime now = _clock.GetUtcNow().UtcDateTime;
        Assert.AreEqual(ReportStatus.Completed, stored.Status);
        Assert.AreEqual(now, stored.StartedAtUtc);
        Assert.AreEqual(now, stored.CompletedAtUtc);
        Assert.AreEqual(1, stored.Attempts);
        Assert.IsNull(stored.ErrorCode);

        Assert.IsNotNull(stored.ResultJson);
        ReportResult result = ReportResultJson.Deserialize(stored.ResultJson);
        Assert.AreEqual(now, result.GeneratedAt);
        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(175.00m, result.TotalAmount);
    }

    /// <summary>Global usa todos os funcionários do período (exemplo da apostila: 4 / R$ 375,00).</summary>
    [TestMethod]
    public async Task GlobalReportUsesAllEmployees()
    {
        ReportRequest report = Seed(ReportScope.Global);

        await _processor.ProcessAsync(report.Id, CancellationToken.None);

        ReportResult result = ReportResultJson.Deserialize(_repository.Stored(report.Id).ResultJson!);
        Assert.AreEqual(4, result.TotalCount);
        Assert.AreEqual(375.00m, result.TotalAmount);
    }

    /// <summary>Uma solicitação em Processing (tentativa anterior interrompida) é retomada e concluída.</summary>
    [TestMethod]
    public async Task ProcessingReportIsResumed()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        DateTime firstStart = new(2026, 10, 9, 14, 0, 0, DateTimeKind.Utc);
        report.Status = ReportStatus.Processing;
        report.StartedAtUtc = firstStart;
        report.Attempts = 1;
        _repository.Seed(report);

        ReportProcessingOutcome outcome = await _processor.ProcessAsync(report.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.Completed, outcome);
        ReportRequest stored = _repository.Stored(report.Id);
        Assert.AreEqual(ReportStatus.Completed, stored.Status);
        Assert.AreEqual(firstStart, stored.StartedAtUtc);
        Assert.AreEqual(2, stored.Attempts);
    }

    /// <summary>Solicitação já concluída não é recalculada nem salva de novo.</summary>
    [TestMethod]
    public async Task CompletedReportIsNotRecalculated()
    {
        ReportRequest report = Seed(ReportScope.Mine);
        await _processor.ProcessAsync(report.Id, CancellationToken.None);
        string? savedJson = _repository.Stored(report.Id).ResultJson;
        int saves = _repository.SaveCount;
        _clock.Advance(TimeSpan.FromHours(1));

        ReportProcessingOutcome outcome = await _processor.ProcessAsync(report.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.AlreadyFinished, outcome);
        Assert.AreEqual(savedJson, _repository.Stored(report.Id).ResultJson);
        Assert.AreEqual(saves, _repository.SaveCount);
        Assert.AreEqual(1, _repository.ExpenseQueryCount);
    }

    /// <summary>Período inválido registrado vira Failed com código estável, sem resultado.</summary>
    [TestMethod]
    public async Task InvalidStoredPeriodIsMarkedFailed()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.To, ReportScenario.From);
        _repository.Seed(report);

        ReportProcessingOutcome outcome = await _processor.ProcessAsync(report.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.Failed, outcome);
        ReportRequest stored = _repository.Stored(report.Id);
        Assert.AreEqual(ReportStatus.Failed, stored.Status);
        Assert.AreEqual(ReportProcessor.InvalidPeriodCode, stored.ErrorCode);
        Assert.IsNotNull(stored.CompletedAtUtc);
        Assert.IsNull(stored.ResultJson);
        Assert.AreEqual(0, _repository.ExpenseQueryCount);
    }

    /// <summary>Solicitação inexistente não gera efeito.</summary>
    [TestMethod]
    public async Task UnknownReportReturnsNotFound()
    {
        ReportProcessingOutcome outcome = await _processor.ProcessAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.NotFound, outcome);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Conflito de concorrência ao marcar Processing não calcula nem conclui.</summary>
    [TestMethod]
    public async Task ConcurrencyConflictDoesNotComplete()
    {
        ReportRequest report = Seed(ReportScope.Mine);
        _repository.FailNextSave = true;

        ReportProcessingOutcome outcome = await _processor.ProcessAsync(report.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.Conflict, outcome);
        Assert.AreEqual(ReportStatus.Pending, _repository.Stored(report.Id).Status);
        Assert.AreEqual(0, _repository.ExpenseQueryCount);
    }

    private ReportRequest Seed(ReportScope scope)
    {
        ReportRequest report = TestData.Report(TestData.AnaId, scope, ReportScenario.From, ReportScenario.To);
        _repository.Seed(report);
        return report;
    }
}
