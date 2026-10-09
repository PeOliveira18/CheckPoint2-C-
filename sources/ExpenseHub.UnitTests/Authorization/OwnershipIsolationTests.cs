using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Authorization;

/// <summary>
/// I06 — isolamento entre Employees e ausência de acesso funcional implícito.
/// </summary>
[TestClass]
public sealed class OwnershipIsolationTests
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

    /// <summary>Trocar o identificador na URL não expõe nem altera o reembolso de outro Employee em nenhum estado.</summary>
    /// <param name="status">Estado do reembolso da Ana.</param>
    [TestMethod]
    [DataRow("Draft")]
    [DataRow("Submitted")]
    [DataRow("Approved")]
    [DataRow("Rejected")]
    [DataRow("Paid")]
    public async Task AnotherEmployeeCannotReadOrChangeExpense(string status)
    {
        Expense expense = TestData.Expense(TestData.AnaId, System.Enum.Parse<ExpenseStatus>(status));
        _repository.Seed(expense);
        ExpenseInput input = TestData.ValidInput(new FixedTimeProvider().Today);

        Assert.AreEqual(ServiceError.NotFound, (await _service.GetAsync(TestData.Bruno, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.UpdateAsync(TestData.Bruno, expense.Id, input, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.SubmitAsync(TestData.Bruno, expense.Id, CancellationToken.None)).Error);
        ServiceResult<IReadOnlyList<ExpenseResponse>> list = await _service.ListAsync(TestData.Bruno, CancellationToken.None);
        Assert.IsEmpty(list.Value!);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    /// <summary>Admin não recebe acesso funcional apenas por ser Admin.</summary>
    [TestMethod]
    public async Task AdminWithoutFunctionalRolesHasNoExpenseAccess()
    {
        Expense expense = TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted);
        _repository.Seed(expense);

        Assert.AreEqual(ServiceError.Forbidden, (await _service.GetAsync(TestData.Fabio, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.Forbidden, (await _service.SubmitAsync(TestData.Fabio, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(
            ServiceError.Forbidden,
            (await _service.CreateAsync(TestData.Fabio, TestData.ValidInput(new FixedTimeProvider().Today), CancellationToken.None)).Error);
    }

    /// <summary>Auditor consulta todos os reembolsos, mas não escreve.</summary>
    [TestMethod]
    public async Task AuditorReadsEverythingButCannotWrite()
    {
        Expense draft = TestData.Expense(TestData.AnaId, ExpenseStatus.Draft);
        Expense paid = TestData.Expense(TestData.BrunoId, ExpenseStatus.Paid);
        _repository.Seed(draft);
        _repository.Seed(paid);

        ServiceResult<IReadOnlyList<ExpenseResponse>> list = await _service.ListAsync(TestData.Edu, CancellationToken.None);
        ServiceResult<ExpenseResponse> update =
            await _service.UpdateAsync(TestData.Edu, draft.Id, TestData.ValidInput(new FixedTimeProvider().Today), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { draft.Id, paid.Id }, list.Value!.Select(expense => expense.Id).ToList());
        Assert.AreEqual(ServiceError.Forbidden, update.Error);
        Assert.AreEqual(0, _repository.SaveCount);
    }
}
