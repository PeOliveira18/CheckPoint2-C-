using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// I05 — envio, listagem e consulta de reembolsos.
/// </summary>
[TestClass]
public sealed class ExpenseSubmitAndQueryTests
{
    private FakeExpenseRepository _repository = null!;
    private ExpenseService _service = null!;

    /// <summary>Cria um serviço novo para cada teste.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeExpenseRepository();
        _service = new ExpenseService(_repository, new FixedTimeProvider());
    }

    /// <summary>O proprietário envia o rascunho: Draft → Submitted com histórico.</summary>
    [TestMethod]
    public async Task SubmitOwnDraftMovesToSubmittedAndRecordsHistory()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestData.Ana, draft.Id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Stored(draft.Id).Status);
        ExpenseHistory history = _repository.HistoryOf(draft.Id).Single();
        Assert.AreEqual(ExpenseAction.Submitted, history.Action);
        Assert.AreEqual(ExpenseStatus.Draft, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, history.ToStatus);
        Assert.AreEqual(TestData.AnaId, history.ActorId);
    }

    /// <summary>Reenviar um reembolso já enviado retorna conflito e não duplica o histórico.</summary>
    [TestMethod]
    public async Task SubmitTwiceReturnsConflictWithoutDuplicatingHistory()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);
        await _service.SubmitAsync(TestData.Ana, draft.Id, CancellationToken.None);

        ServiceResult<ExpenseResponse> second = await _service.SubmitAsync(TestData.Ana, draft.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, _repository.HistoryOf(draft.Id));
    }

    /// <summary>Outro Employee não envia o rascunho (404, sem revelar existência).</summary>
    [TestMethod]
    public async Task SubmitDraftOfAnotherEmployeeReturnsNotFound()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestData.Bruno, draft.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
        Assert.AreEqual(ExpenseStatus.Draft, _repository.Stored(draft.Id).Status);
    }

    /// <summary>Auditor não ganha escrita: não envia reembolsos.</summary>
    [TestMethod]
    public async Task SubmitAsAuditorIsForbidden()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestData.Edu, draft.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.IsEmpty(_repository.HistoryOf(draft.Id));
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    [TestMethod]
    public async Task SubmitUnknownExpenseReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestData.Ana, Guid.NewGuid(), CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>Employee lista somente os próprios reembolsos.</summary>
    [TestMethod]
    public async Task ListAsEmployeeReturnsOnlyOwnExpenses()
    {
        Expense own = Seed(TestData.AnaId, ExpenseStatus.Draft);
        Seed(TestData.BrunoId, ExpenseStatus.Draft);
        Seed(TestData.BrunoId, ExpenseStatus.Submitted);

        CollectionAssert.AreEquivalent(new[] { own.Id }, await ListIds(TestData.Ana));
    }

    /// <summary>A listagem envia o filtro de visibilidade ao repositório (aplicado antes de materializar).</summary>
    [TestMethod]
    public async Task ListPassesVisibilityFilterToRepository()
    {
        await _service.ListAsync(TestData.Ana, CancellationToken.None);

        Assert.IsNotNull(_repository.LastVisibilityFilter);
    }

    /// <summary>Admin sem roles funcionais e usuário sem role não listam reembolsos (403).</summary>
    [TestMethod]
    public async Task ListWithoutFunctionalRoleIsForbidden()
    {
        Seed(TestData.AnaId, ExpenseStatus.Submitted);

        Assert.AreEqual(ServiceError.Forbidden, (await _service.ListAsync(TestData.Fabio, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.Forbidden, (await _service.ListAsync(TestData.NoRole, CancellationToken.None)).Error);
    }

    /// <summary>Detalhe fora do escopo é 404; o proprietário consulta o próprio.</summary>
    [TestMethod]
    public async Task GetOutsideScopeReturnsNotFoundAndOwnerSeesOwn()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Bruno, draft.Id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Carla, draft.Id, CancellationToken.None)).Error);
        Assert.IsTrue((await _service.GetAsync(TestData.Ana, draft.Id, CancellationToken.None)).Succeeded);
    }

    /// <summary>Reembolso inexistente retorna 404 no detalhe.</summary>
    [TestMethod]
    public async Task GetUnknownExpenseReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(TestData.Edu, Guid.NewGuid(), CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    private async Task<List<Guid>> ListIds(UserContext user)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(user, CancellationToken.None);
        Assert.IsTrue(result.Succeeded);
        return result.Value.Select(expense => expense.Id).ToList();
    }

    private Expense Seed(string ownerId, ExpenseStatus status)
    {
        Expense expense = TestData.Expense(ownerId, status);
        _repository.Seed(expense);
        return expense;
    }
}
