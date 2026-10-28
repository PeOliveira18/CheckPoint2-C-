using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace ExpenseHub.UnitTests.Expenses;
/// <summary>
/// I07 — a reprovação registra quem decidiu e quando, a partir do servidor.
/// </summary>
[TestClass]
public sealed class ExpenseRejectionAuditTests
{
    /// <summary>Ator e horário da reprovação vêm do token e do relógio do servidor, e o histórico registra a transição.</summary>
    [TestMethod]
    public async Task RejectRecordsServerActorTimeAndTransition()
    {
        FakeExpenseRepository repository = new();
        FixedTimeProvider clock = new();
        ExpenseService service = new(repository, clock);
        Expense expense = TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted);
        repository.Seed(expense);
        ServiceResult<ExpenseResponse> result = await service.RejectAsync(
            TestData.Carla,
            expense.Id,
            "Nota fiscal não corresponde ao valor solicitado.",
            CancellationToken.None);
        Assert.IsTrue(result.Succeeded);
        Expense stored = repository.Stored(expense.Id);
        Assert.AreEqual(TestData.CarlaId, stored.DecidedById);
        Assert.AreEqual(clock.GetUtcNow().UtcDateTime, stored.DecidedAtUtc);
        ExpenseHistory history = repository.HistoryOf(expense.Id).Single();
        Assert.AreEqual(ExpenseStatus.Submitted, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Rejected, history.ToStatus);
    }
}
