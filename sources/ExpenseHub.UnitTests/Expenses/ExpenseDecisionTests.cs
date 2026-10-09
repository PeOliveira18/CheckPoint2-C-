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
/// I07 — aprovação e reprovação com justificativa.
/// </summary>
[TestClass]
public sealed class ExpenseDecisionTests
{
    private const string ValidJustification = "Comprovante ilegível, reenvie em novo pedido.";

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

    /// <summary>Approver aprova reembolso enviado de outra pessoa; ator e horário vêm do servidor.</summary>
    [TestMethod]
    public async Task ApproveSubmittedMovesToApprovedWithServerActorAndTime()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestData.Carla, expense.Id, CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Expense stored = _repository.Stored(expense.Id);
        Assert.AreEqual(ExpenseStatus.Approved, stored.Status);
        Assert.AreEqual(TestData.CarlaId, stored.DecidedById);
        Assert.AreEqual(_clock.GetUtcNow().UtcDateTime, stored.DecidedAtUtc);
        ExpenseHistory history = _repository.HistoryOf(expense.Id).Single();
        Assert.AreEqual(ExpenseAction.Approved, history.Action);
        Assert.AreEqual(ExpenseStatus.Submitted, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Approved, history.ToStatus);
        Assert.AreEqual(TestData.CarlaId, history.ActorId);
    }

    /// <summary>Reprovação válida persiste a justificativa no reembolso e no histórico.</summary>
    [TestMethod]
    public async Task RejectSubmittedPersistsJustification()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result =
            await _service.RejectAsync(TestData.Carla, expense.Id, "  " + ValidJustification + "  ", CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
        Expense stored = _repository.Stored(expense.Id);
        Assert.AreEqual(ExpenseStatus.Rejected, stored.Status);
        Assert.AreEqual(ValidJustification, stored.RejectionReason);
        ExpenseHistory history = _repository.HistoryOf(expense.Id).Single();
        Assert.AreEqual(ExpenseAction.Rejected, history.Action);
        Assert.AreEqual(ValidJustification, history.Justification);
    }

    /// <summary>Justificativa ausente, curta ou longa é rejeitada sem alterar o reembolso.</summary>
    /// <param name="length">Tamanho da justificativa (-1 = nula).</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(501)]
    public async Task RejectWithInvalidJustificationIsInvalid(int length)
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);
        string? justification = length < 0 ? null : new string('j', length);

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(TestData.Carla, expense.Id, justification, CancellationToken.None);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsTrue(result.ValidationErrors!.ContainsKey("justification"));
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Stored(expense.Id).Status);
    }

    /// <summary>Justificativas com 10 e 500 caracteres são aceitas.</summary>
    /// <param name="length">Tamanho da justificativa.</param>
    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public async Task RejectWithJustificationAtLimitsSucceeds(int length)
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result =
            await _service.RejectAsync(TestData.Carla, expense.Id, new string('j', length), CancellationToken.None);

        Assert.IsTrue(result.Succeeded);
    }

    /// <summary>Employee, Finance, Auditor e Admin sem role Approver não decidem.</summary>
    /// <param name="roles">Roles do usuário.</param>
    [TestMethod]
    [DataRow("Employee")]
    [DataRow("Finance")]
    [DataRow("Auditor")]
    [DataRow("Admin")]
    [DataRow("Employee,Finance,Auditor,Admin")]
    public async Task DecisionWithoutApproverRoleIsForbidden(string roles)
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);
        UserContext user = TestData.User("x", roles.Split(','));

        Assert.AreEqual(ServiceError.Forbidden, (await _service.ApproveAsync(user, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(
            ServiceError.Forbidden,
            (await _service.RejectAsync(user, expense.Id, ValidJustification, CancellationToken.None)).Error);
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Stored(expense.Id).Status);
    }

    /// <summary>Proprietário com role Approver (acumulada com Employee) não decide sobre o próprio reembolso.</summary>
    [TestMethod]
    public async Task OwnerWithApproverRoleCannotDecideOwnExpense()
    {
        UserContext anaApprover = TestData.User(TestData.AnaId, RoleNames.Employee, RoleNames.Approver);
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);

        Assert.AreEqual(ServiceError.Forbidden, (await _service.ApproveAsync(anaApprover, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(
            ServiceError.Forbidden,
            (await _service.RejectAsync(anaApprover, expense.Id, ValidJustification, CancellationToken.None)).Error);
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    /// <summary>Approved, Rejected e Paid não recebem nova decisão (409, sem histórico).</summary>
    /// <param name="status">Estado atual.</param>
    [TestMethod]
    [DataRow("Approved")]
    [DataRow("Rejected")]
    [DataRow("Paid")]
    public async Task DecisionOutsideSubmittedReturnsConflict(string status)
    {
        Expense expense = Seed(TestData.AnaId, Enum.Parse<ExpenseStatus>(status));

        Assert.AreEqual(ServiceError.Conflict, (await _service.ApproveAsync(TestData.Carla, expense.Id, CancellationToken.None)).Error);
        Assert.AreEqual(
            ServiceError.Conflict,
            (await _service.RejectAsync(TestData.Carla, expense.Id, ValidJustification, CancellationToken.None)).Error);
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    /// <summary>Aprovar duas vezes: a segunda retorna 409 e o histórico tem uma única aprovação.</summary>
    [TestMethod]
    public async Task ApproveTwiceReturnsConflictWithoutDuplicatingHistory()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);
        await _service.ApproveAsync(TestData.Carla, expense.Id, CancellationToken.None);

        ServiceResult<ExpenseResponse> second = await _service.ApproveAsync(TestData.Carla, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, _repository.HistoryOf(expense.Id));
    }

    /// <summary>Rascunho de outra pessoa não é revelado ao Approver (404); inexistente também é 404.</summary>
    [TestMethod]
    public async Task DecisionOnForeignDraftOrUnknownReturnsNotFound()
    {
        Expense draft = Seed(TestData.AnaId, ExpenseStatus.Draft);

        Assert.AreEqual(ServiceError.NotFound, (await _service.ApproveAsync(TestData.Carla, draft.Id, CancellationToken.None)).Error);
        Assert.AreEqual(ServiceError.NotFound, (await _service.ApproveAsync(TestData.Carla, Guid.NewGuid(), CancellationToken.None)).Error);
    }

    /// <summary>Decisão concorrente perdida: nada é persistido e a resposta é 409.</summary>
    [TestMethod]
    public async Task DecisionWhenSaveConflictsPersistsNothing()
    {
        Expense expense = Seed(TestData.AnaId, ExpenseStatus.Submitted);
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestData.Carla, expense.Id, CancellationToken.None);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Stored(expense.Id).Status);
        Assert.IsNull(_repository.Stored(expense.Id).DecidedById);
        Assert.IsEmpty(_repository.HistoryOf(expense.Id));
    }

    private Expense Seed(string ownerId, ExpenseStatus status)
    {
        Expense expense = TestData.Expense(ownerId, status);
        _repository.Seed(expense);
        return expense;
    }
}
