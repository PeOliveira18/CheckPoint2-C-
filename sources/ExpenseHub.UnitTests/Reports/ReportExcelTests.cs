using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ExpenseHub.Api.Application;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — download Excel montado a partir do resultado salvo.
/// </summary>
[TestClass]
public sealed class ReportExcelTests
{
    private FakeReportRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private ReportService _service = null!;

    /// <summary>Cria um serviço novo para cada teste.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeReportRepository();
        _clock = new FixedTimeProvider();
        _service = new ReportService(_repository, new FakeReportPublisher(), _clock, NullLogger<ReportService>.Instance);
    }

    /// <summary>Relatório concluído gera planilha válida com cabeçalho, estados e total numéricos.</summary>
    [TestMethod]
    public async Task CompletedReportExportsSavedTotals()
    {
        ReportScenario.ApostilaExpenses().ForEach(_repository.SeedExpense);
        Guid id = await RequestAndProcess(TestData.Ana);

        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        using XLWorkbook workbook = Open(result.Value.Content);
        IXLWorksheet sheet = workbook.Worksheet(1);
        Assert.AreEqual(id.ToString(), sheet.Cell("B1").GetString());
        Assert.AreEqual("Mine", sheet.Cell("B2").GetString());
        Assert.AreEqual("2026-09-01", sheet.Cell("B3").GetString());
        Assert.AreEqual("2026-09-30", sheet.Cell("B4").GetString());
        StringAssert.EndsWith(sheet.Cell("B5").GetString(), "Z");
        Assert.AreEqual("Estado", sheet.Cell("A7").GetString());
        Assert.AreEqual("Quantidade", sheet.Cell("B7").GetString());
        Assert.AreEqual("Valor", sheet.Cell("C7").GetString());
        AssertLine(sheet, "Approved", 2, 150.00m);
        AssertLine(sheet, "Paid", 1, 25.00m);
        AssertLine(sheet, "Total", 3, 175.00m);
    }

    /// <summary>Alterar um reembolso depois da conclusão não muda a planilha.</summary>
    [TestMethod]
    public async Task ChangingExpenseAfterCompletionKeepsSavedTotals()
    {
        List<Expense> expenses = ReportScenario.ApostilaExpenses();
        expenses.ForEach(_repository.SeedExpense);
        Guid id = await RequestAndProcess(TestData.Ana);

        expenses[0].Amount = 999.00m;
        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, id, CancellationToken.None);

        using XLWorkbook workbook = Open(result.Value!.Content);
        AssertLine(workbook.Worksheet(1), "Total", 3, 175.00m);
        Assert.AreEqual(1, _repository.ExpenseQueryCount);
    }

    /// <summary>Período sem reembolsos gera os cinco estados zerados e total zero.</summary>
    [TestMethod]
    public async Task EmptyReportExportsFiveZeroedStatuses()
    {
        Guid id = await RequestAndProcess(TestData.Ana);

        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, id, CancellationToken.None);

        using XLWorkbook workbook = Open(result.Value!.Content);
        IXLWorksheet sheet = workbook.Worksheet(1);
        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            AssertLine(sheet, status.ToString(), 0, 0m);
        }

        AssertLine(sheet, "Total", 0, 0m);
    }

    /// <summary>Relatório ainda Pending responde conflito.</summary>
    [TestMethod]
    public async Task PendingReportReturnsConflict()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        _repository.Seed(report);

        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, report.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>Relatório Failed responde conflito.</summary>
    [TestMethod]
    public async Task FailedReportReturnsConflict()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        report.Status = ReportStatus.Failed;
        report.ErrorCode = "invalid-period";
        _repository.Seed(report);

        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, report.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>Relatório de outro usuário responde 404, inclusive para Auditor.</summary>
    [TestMethod]
    public async Task OtherUsersReportReturnsNotFoundEvenForAuditor()
    {
        Guid id = await RequestAndProcess(TestData.Ana);

        Assert.AreEqual(ServiceError.NotFound, (await _service.GetExcelAsync(TestData.Bruno, id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.GetExcelAsync(TestData.Edu, id, CancellationToken.None)).Error);
    }

    /// <summary>O arquivo leva o id do relatório no nome.</summary>
    [TestMethod]
    public async Task FileNameUsesReportId()
    {
        Guid id = await RequestAndProcess(TestData.Ana);

        ServiceResult<ReportExcelFile> result = await _service.GetExcelAsync(TestData.Ana, id, CancellationToken.None);

        Assert.AreEqual($"report-{id}.xlsx", result.Value!.FileName);
    }

    private async Task<Guid> RequestAndProcess(UserContext user)
    {
        ServiceResult<ReportSubmission> submitted = await _service.RequestAsync(
            user,
            new ReportInput(ReportScenario.From, ReportScenario.To, ReportScope.Mine),
            CancellationToken.None);
        Guid id = submitted.Value!.Report.Id;
        await new ReportProcessor(_repository, _clock).ProcessAsync(id, CancellationToken.None);
        return id;
    }

    private static XLWorkbook Open(byte[] content) => new(new MemoryStream(content));

    private static void AssertLine(IXLWorksheet sheet, string label, int count, decimal amount)
    {
        IXLRow row = sheet.Column(1).CellsUsed(cell => cell.GetString() == label).Single().WorksheetRow();
        IXLCell countCell = row.Cell(2);
        IXLCell amountCell = row.Cell(3);
        Assert.AreEqual(XLDataType.Number, countCell.DataType);
        Assert.AreEqual(XLDataType.Number, amountCell.DataType);
        Assert.AreEqual(count, countCell.GetValue<int>());
        Assert.AreEqual(amount, amountCell.GetValue<decimal>());
    }
}
