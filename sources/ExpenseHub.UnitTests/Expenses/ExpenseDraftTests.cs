using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// I04 — criação e edição de rascunhos.
/// </summary>
[TestClass]
public sealed class ExpenseDraftTests
{
    private FakeExpenseRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private ExpenseService _service = null!;

    /// <summary>Cria um serviço novo para cada teste.</summary>
    [TestInitialize]
    public void Setup()
    {
        _repository = new FakeExpenseRepository();
        _clock = new FixedTimeProvider();
        _service = new ExpenseService(_repository, _clock);
    }

    /// <summary>Employee cria rascunho em Draft com proprietário e horários do servidor.</summary>
    [TestMethod]
    public async Task CreateAsEmployeeStoresDraftOwnedByCaller()
    {
        ServiceResult<ExpenseResponse> result =
            await _service.CreateAsync(TestData.Ana, TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Expense stored = _repository.Stored(result.Value.Id);
        Assert.AreEqual(ExpenseStatus.Draft, stored.Status);
        Assert.AreEqual(TestData.AnaId, stored.OwnerId);
        Assert.AreEqual(FixedTimeProvider.DefaultNow.UtcDateTime, stored.CreatedAtUtc);
    }

    /// <summary>A criação gera uma entrada de histórico Created atribuída ao proprietário.</summary>
    [TestMethod]
    public async Task CreateRecordsCreatedHistory()
    {
        ServiceResult<ExpenseResponse> result =
            await _service.CreateAsync(TestData.Ana, TestData.ValidInput(_clock.Today), CancellationToken.None);

        IReadOnlyList<ExpenseHistory> history = _repository.HistoryOf(result.Value!.Id);
        Assert.HasCount(1, history);
        Assert.AreEqual(ExpenseAction.Created, history[0].Action);
        Assert.IsNull(history[0].FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, history[0].ToStatus);
        Assert.AreEqual(TestData.AnaId, history[0].ActorId);
    }

    /// <summary>Usuários sem a role Employee não criam reembolsos, mesmo acumulando outras roles.</summary>
    /// <param name="roles">Roles do usuário, separadas por vírgula.</param>
    [TestMethod]
    [DataRow("Approver")]
    [DataRow("Finance")]
    [DataRow("Auditor")]
    [DataRow("Admin")]
    [DataRow("Approver,Finance,Auditor,Admin")]
    public async Task CreateWithoutEmployeeRoleIsForbidden(string roles)
    {
        UserContext user = TestData.User("x", roles.Split(','));

        ServiceResult<ExpenseResponse> result =
            await _service.CreateAsync(user, TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Valores fora do intervalo R$ 0,01–Int32.MaxValue são rejeitados.</summary>
    /// <param name="amount">Valor informado.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("0.009")]
    [DataRow("-1")]
    [DataRow("2147483647.01")]
    public async Task CreateWithAmountOutOfRangeIsInvalid(string amount)
    {
        ExpenseInput input = TestData.ValidInput(_clock.Today) with
        {
            Amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
        };

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestData.Ana, input, CancellationToken.None);

        AssertInvalid(result, "amount");
    }

    /// <summary>Os limites exatos de valor são aceitos.</summary>
    /// <param name="amount">Valor informado.</param>
    [TestMethod]
    [DataRow("0.01")]
    [DataRow("2147483647")]
    public async Task CreateWithAmountAtLimitsSucceeds(string amount)
    {
        ExpenseInput input = TestData.ValidInput(_clock.Today) with
        {
            Amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture),
        };

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestData.Ana, input, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
    }

    /// <summary>Data futura é rejeitada; a data de hoje é aceita.</summary>
    [TestMethod]
    public async Task CreateWithFutureDateIsInvalidButTodayIsAccepted()
    {
        ExpenseInput future = TestData.ValidInput(_clock.Today) with { ExpenseDate = _clock.Today.AddDays(1) };
        ExpenseInput today = TestData.ValidInput(_clock.Today) with { ExpenseDate = _clock.Today };

        AssertInvalid(await _service.CreateAsync(TestData.Ana, future, CancellationToken.None), "expenseDate");
        Assert.IsTrue((await _service.CreateAsync(TestData.Ana, today, CancellationToken.None)).Succeeded);
    }

    /// <summary>Descrição com menos de 10 ou mais de 500 caracteres (desconsiderando espaços nas pontas) é rejeitada.</summary>
    /// <param name="length">Tamanho da descrição.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(501)]
    public async Task CreateWithDescriptionOutOfBoundsIsInvalid(int length)
    {
        ExpenseInput input = TestData.ValidInput(_clock.Today) with { Description = new string('a', length) };

        AssertInvalid(await _service.CreateAsync(TestData.Ana, input, CancellationToken.None), "description");
    }

    /// <summary>Espaços não contam para o tamanho mínimo da descrição.</summary>
    [TestMethod]
    public async Task CreateWithPaddedShortDescriptionIsInvalid()
    {
        ExpenseInput input = TestData.ValidInput(_clock.Today) with { Description = "   curta   " };

        AssertInvalid(await _service.CreateAsync(TestData.Ana, input, CancellationToken.None), "description");
    }

    /// <summary>Categoria inexistente é rejeitada.</summary>
    [TestMethod]
    public async Task CreateWithUnknownCategoryIsInvalid()
    {
        ExpenseInput input = TestData.ValidInput(_clock.Today) with { CategoryId = 99 };

        AssertInvalid(await _service.CreateAsync(TestData.Ana, input, CancellationToken.None), "categoryId");
    }

    /// <summary>O proprietário edita o próprio rascunho e as alterações ficam no histórico.</summary>
    [TestMethod]
    public async Task UpdateOwnDraftPersistsChangesAndHistory()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);
        ExpenseInput input = TestData.ValidInput(_clock.Today) with { Amount = 99.90m };

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestData.Ana, draft.Id, input, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Expense stored = _repository.Stored(draft.Id);
        Assert.AreEqual(99.90m, stored.Amount);
        Assert.AreEqual(ExpenseStatus.Draft, stored.Status);
        Assert.AreEqual(TestData.AnaId, stored.OwnerId);
        IReadOnlyList<ExpenseHistory> history = _repository.HistoryOf(draft.Id);
        Assert.HasCount(1, history);
        Assert.AreEqual(ExpenseAction.Updated, history[0].Action);
        StringAssert.Contains(history[0].Changes, "amount: 120.00 -> 99.90");
    }

    /// <summary>Edição sem nenhuma diferença não gera histórico.</summary>
    [TestMethod]
    public async Task UpdateWithoutChangesDoesNotRecordHistory()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);
        ExpenseInput same = new(draft.Description, draft.Amount, draft.ExpenseDate, draft.CategoryId);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestData.Ana, draft.Id, same, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.IsEmpty(_repository.HistoryOf(draft.Id));
    }

    /// <summary>Outro Employee não enxerga nem edita o rascunho (404) e nada muda.</summary>
    [TestMethod]
    public async Task UpdateDraftOfAnotherEmployeeReturnsNotFound()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result =
            await _service.UpdateAsync(TestData.Bruno, draft.Id, TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
        Assert.AreEqual(draft.Amount, _repository.Stored(draft.Id).Amount);
    }

    /// <summary>Employee que também é Auditor vê o rascunho alheio, mas não pode editá-lo (403).</summary>
    [TestMethod]
    public async Task UpdateVisibleDraftOfAnotherUserIsForbidden()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);
        UserContext employeeAuditor = TestData.User(TestData.BrunoId, RoleNames.Employee, RoleNames.Auditor);

        ServiceResult<ExpenseResponse> result =
            await _service.UpdateAsync(employeeAuditor, draft.Id, TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>Reembolso fora de Draft não é editado (409) e o histórico não muda.</summary>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow("Submitted")]
    [DataRow("Approved")]
    [DataRow("Rejected")]
    [DataRow("Paid")]
    public async Task UpdateOutsideDraftReturnsConflict(string status)
    {
        Expense expense = Seed(TestData.AnaId, Enum.Parse<ExpenseStatus>(status));

        ServiceResult<ExpenseResponse> result =
            await _service.UpdateAsync(TestData.Ana, expense.Id, TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    /// <summary>Reembolso inexistente retorna 404.</summary>
    [TestMethod]
    public async Task UpdateUnknownExpenseReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result =
            await _service.UpdateAsync(TestData.Ana, Guid.NewGuid(), TestData.ValidInput(_clock.Today), CancellationToken.None);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>Falha de persistência não deixa alteração sem histórico nem histórico sem alteração.</summary>
    [TestMethod]
    public async Task UpdateWhenSaveFailsKeepsStateAndHistoryConsistent()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestData.Ana,
            draft.Id,
            TestData.ValidInput(_clock.Today) with { Amount = 1m },
            CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.AreEqual(draft.Amount, _repository.Stored(draft.Id).Amount);
        Assert.IsEmpty(_repository.HistoryOf(draft.Id));
    }

    private static void AssertInvalid(ServiceResult<ExpenseResponse> result, string field)
    {
        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsNotNull(result.ValidationErrors);
        Assert.IsTrue(result.ValidationErrors.ContainsKey(field), $"Esperado erro no campo '{field}'.");
    }

    private Expense Seed(string ownerId, ExpenseStatus status)
    {
        Expense expense = TestData.Expense(ownerId, status);
        _repository.Seed(expense);
        return expense;
    }
}
