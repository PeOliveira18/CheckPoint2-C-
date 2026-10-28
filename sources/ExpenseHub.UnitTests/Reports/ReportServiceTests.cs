using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — solicitação, listagem e detalhe de relatórios.
/// </summary>
[TestClass]
public sealed class ReportServiceTests
{
    private FakeReportRepository _repository = null!;
    private FakeReportPublisher _publisher = null!;
    private FixedTimeProvider _clock = null!;
    private ReportService _service = null!;

    /// <summary>Cria um serviço novo para cada teste.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeReportRepository();
        _publisher = new FakeReportPublisher();
        _clock = new FixedTimeProvider();
        _service = new ReportService(_repository, _publisher, _clock, NullLogger<ReportService>.Instance);
    }

    /// <summary>Pedido válido persiste Pending com solicitante da identidade e publica o mesmo id.</summary>
    [TestMethod]
    public async Task ValidRequestIsPersistedAsPendingAndPublished()
    {
        ServiceResult<ReportSubmission> result = await Request(TestData.Ana, ReportScope.Mine);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Value.Published);
        ReportResponse response = result.Value.Report;
        ReportRequest stored = _repository.Stored(response.Id);
        Assert.AreEqual(TestData.AnaId, stored.RequesterId);
        Assert.AreEqual(ReportStatus.Pending, stored.Status);
        Assert.AreEqual(ReportScope.Mine, stored.Scope);
        Assert.AreEqual(ReportScenario.From, stored.FromDate);
        Assert.AreEqual(ReportScenario.To, stored.ToDate);
        Assert.AreEqual(_clock.GetUtcNow().UtcDateTime, response.CreatedAt);
        Assert.IsNull(response.StartedAt);
        Assert.IsNull(response.Result);
        CollectionAssert.AreEqual(new[] { response.Id }, _publisher.PublishedIds.ToList());
    }

    /// <summary>Datas invertidas não criam trabalho: nada persistido nem publicado.</summary>
    [TestMethod]
    public async Task InvertedPeriodIsRejectedWithoutCreatingWork()
    {
        ServiceResult<ReportSubmission> result = await _service.RequestAsync(
            TestData.Ana,
            new ReportInput(ReportScenario.To, ReportScenario.From, ReportScope.Mine),
            CancellationToken.None);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey("toDate"));
        Assert.AreEqual(0, _repository.StoredCount);
        Assert.IsEmpty(_publisher.PublishedIds);
    }

    /// <summary>Datas ausentes são rejeitadas sem publicação.</summary>
    [TestMethod]
    public async Task MissingDatesAreRejectedWithoutPublishing()
    {
        ServiceResult<ReportSubmission> result = await _service.RequestAsync(
            TestData.Ana,
            new ReportInput(null, null, ReportScope.Mine),
            CancellationToken.None);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsEmpty(_publisher.PublishedIds);
    }

    /// <summary>Publicação sem confirmação mantém o registro e devolve o id persistido.</summary>
    [TestMethod]
    public async Task UnconfirmedPublicationKeepsPersistedRequest()
    {
        _publisher.Confirm = false;

        ServiceResult<ReportSubmission> result = await Request(TestData.Ana, ReportScope.Mine);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Value.Published);
        Assert.AreEqual(ReportStatus.Pending, _repository.Stored(result.Value.Report.Id).Status);
    }

    /// <summary>Banco indisponível antes de registrar: erro explícito e nenhuma publicação.</summary>
    [TestMethod]
    public async Task DatabaseFailureBeforeSavingDoesNotPublish()
    {
        _repository.ThrowOnSave = true;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Request(TestData.Ana, ReportScope.Mine));

        Assert.AreEqual(0, _repository.StoredCount);
        Assert.IsEmpty(_publisher.PublishedIds);
    }

    /// <summary>Cada POST válido cria uma nova solicitação com outro id.</summary>
    [TestMethod]
    public async Task EachValidRequestCreatesANewReport()
    {
        ServiceResult<ReportSubmission> first = await Request(TestData.Ana, ReportScope.Mine);
        ServiceResult<ReportSubmission> second = await Request(TestData.Ana, ReportScope.Mine);

        Assert.AreNotEqual(first.Value!.Report.Id, second.Value!.Report.Id);
        Assert.AreEqual(2, _repository.StoredCount);
    }

    /// <summary>A listagem traz somente as solicitações do próprio usuário, mais recentes primeiro.</summary>
    [TestMethod]
    public async Task ListReturnsOnlyOwnReportsNewestFirst()
    {
        ServiceResult<ReportSubmission> older = await Request(TestData.Ana, ReportScope.Mine);
        _clock.Advance(TimeSpan.FromMinutes(5));
        ServiceResult<ReportSubmission> newer = await Request(TestData.Ana, ReportScope.Global);
        await Request(TestData.Bruno, ReportScope.Mine);

        ServiceResult<IReadOnlyList<ReportResponse>> result = await _service.ListAsync(TestData.Ana, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(
            new[] { newer.Value!.Report.Id, older.Value!.Report.Id },
            result.Value.Select(report => report.Id).ToList());
    }

    /// <summary>Detalhe de solicitação alheia ou inexistente retorna 404.</summary>
    [TestMethod]
    public async Task GetOfOtherUserOrUnknownReportReturnsNotFound()
    {
        ServiceResult<ReportSubmission> anas = await Request(TestData.Ana, ReportScope.Mine);

        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Bruno, anas.Value!.Report.Id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Ana, Guid.NewGuid(), CancellationToken.None)).Error);
    }

    /// <summary>Consulta após concluir devolve o resultado salvo, mesmo depois de alterar um reembolso.</summary>
    [TestMethod]
    public async Task GetCompletedReportReturnsSavedResultWithoutRecalculating()
    {
        List<Expense> expenses = ReportScenario.ApostilaExpenses();
        expenses.ForEach(_repository.SeedExpense);
        ServiceResult<ReportSubmission> submitted = await Request(TestData.Ana, ReportScope.Mine);
        Guid id = submitted.Value!.Report.Id;
        await new ReportProcessor(_repository, _clock).ProcessAsync(id, CancellationToken.None);

        expenses[0].Status = ExpenseStatus.Paid;
        expenses[0].Amount = 999.00m;
        ServiceResult<ReportResponse> result = await _service.GetAsync(TestData.Ana, id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ReportStatus.Completed, result.Value.Status);
        Assert.IsNotNull(result.Value.CompletedAt);
        Assert.IsNull(result.Value.Error);
        ReportResult? saved = result.Value.Result;
        Assert.IsNotNull(saved);
        Assert.AreEqual(3, saved.TotalCount);
        Assert.AreEqual(175.00m, saved.TotalAmount);
        Assert.AreEqual(150.00m, saved.ByStatus.Single(total => total.Status == ExpenseStatus.Approved).Amount);
        Assert.AreEqual(1, _repository.ExpenseQueryCount);
    }

    /// <summary>Solicitação Failed expõe código estável e mensagem, sem resultado.</summary>
    [TestMethod]
    public async Task FailedReportExposesErrorWithoutResult()
    {
        ReportRequest report = TestData.Report(TestData.AnaId, ReportScope.Mine, ReportScenario.From, ReportScenario.To);
        report.Status = ReportStatus.Failed;
        report.CompletedAtUtc = new DateTime(2026, 10, 1, 12, 0, 5, DateTimeKind.Utc);
        report.ErrorCode = "invalid-period";
        report.ErrorMessage = "O período registrado é inválido.";
        _repository.Seed(report);

        ServiceResult<ReportResponse> result = await _service.GetAsync(TestData.Ana, report.Id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNull(result.Value.Result);
        Assert.IsNotNull(result.Value.Error);
        Assert.AreEqual("invalid-period", result.Value.Error.Code);
        Assert.AreEqual(DateTimeKind.Utc, result.Value.CompletedAt!.Value.Kind);
    }

    private Task<ServiceResult<ReportSubmission>> Request(UserContext user, ReportScope scope) =>
        _service.RequestAsync(user, new ReportInput(ReportScenario.From, ReportScenario.To, scope), CancellationToken.None);
}
