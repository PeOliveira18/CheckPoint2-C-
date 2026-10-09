using System;
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
/// I08 — pagamento simulado.
/// </summary>
[TestClass]
public sealed class ExpensePaymentTests
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

    /// <summary>Finance paga reembolso aprovado de outra pessoa: Approved → Paid, com registro e histórico.</summary>
    [TestMethod]
    public async Task PayApprovedCreatesPaymentRecordAndHistory()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestData.Dani, expense.Id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(ExpenseStatus.Paid, _repository.Stored(expense.Id).Status);
        PaymentRecord? payment = _repository.PaymentOf(expense.Id);
        Assert.IsNotNull(payment);
        Assert.AreEqual(TestData.DaniId, payment.PaidById);
        Assert.AreEqual(_clock.GetUtcNow().UtcDateTime, payment.PaidAtUtc);
        Assert.AreEqual(expense.Amount, payment.Amount);
        Assert.IsNotNull(result.Value.Payment);
        ExpenseHistory history = _repository.HistoryOf(expense.Id).Single();
        Assert.AreEqual(ExpenseAction.Paid, history.Action);
        Assert.AreEqual(ExpenseStatus.Approved, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, history.ToStatus);
    }

    /// <summary>Submitted, Rejected e Paid não são pagos (409) e nenhum pagamento é criado.</summary>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow("Submitted")]
    [DataRow("Rejected")]
    [DataRow("Paid")]
    public async Task PayOutsideApprovedReturnsConflict(string status)
    {
        Expense expense = Seed(TestData.AnaId, Enum.Parse<ExpenseStatus>(status));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestData.Dani, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.IsNull(_repository.PaymentOf(expense.Id));
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    /// <summary>Pagamento repetido retorna 409 e mantém um único registro e uma única entrada no histórico.</summary>
    [TestMethod]
    public async Task PayTwiceReturnsConflictKeepingSinglePayment()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Approved);
        await _service.PayAsync(TestData.Dani, expense.Id, CancellationToken.None);

        ServiceResult<ExpenseResponse> second = await _service.PayAsync(TestData.Dani, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, _repository.HistoryOf(expense.Id));
    }

    /// <summary>Finance proprietário (Employee + Finance) não paga o próprio reembolso.</summary>
    [TestMethod]
    public async Task FinanceOwnerCannotPayOwnExpense()
    {
        UserContext daniEmployee = TestData.User(TestData.DaniId, RoleNames.Employee, RoleNames.Finance);
        Expense expense = Seed(TestData.DaniId, ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(daniEmployee, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.IsNull(_repository.PaymentOf(expense.Id));
    }

    /// <summary>Somente Finance paga: Approver, Auditor, Employee e Admin recebem 403.</summary>
    /// <param name="roles">Roles do usuário.</param>
    [TestMethod]
    [DataRow("Approver")]
    [DataRow("Auditor")]
    [DataRow("Employee")]
    [DataRow("Admin")]
    public async Task PayWithoutFinanceRoleIsForbidden(string roles)
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result =
            await _service.PayAsync(TestData.User("x", roles.Split(',')), expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.AreEqual(ExpenseStatus.Approved, _repository.Stored(expense.Id).Status);
    }

    /// <summary>Falha de persistência no pagamento não deixa estado, pagamento e histórico divergentes.</summary>
    [TestMethod]
    public async Task PayWhenSaveFailsPersistsNothing()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Approved);
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestData.Dani, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.AreEqual(ExpenseStatus.Approved, _repository.Stored(expense.Id).Status);
        Assert.IsNull(_repository.PaymentOf(expense.Id));
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    private Expense Seed(string ownerId, ExpenseStatus status)
    {
        Expense expense = TestData.Expense(ownerId, status);
        _repository.Seed(expense);
        return expense;
    }
}
