using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Contracts;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// I04 — a descrição é persistida sem espaços nas pontas.
/// </summary>
[TestClass]
public sealed class ExpenseDescriptionTests
{
    /// <summary>Criação e edição gravam a descrição sem espaços no início e no fim.</summary>
    [TestMethod]
    public async Task CreateAndUpdateStoreTrimmedDescription()
    {
        FakeExpenseRepository repository = new();
        FixedTimeProvider clock = new();
        ExpenseService service = new(repository, clock);
        ExpenseInput input = TestData.ValidInput(clock.Today) with { Description = "   Táxi até o cliente   " };

        ServiceResult<ExpenseResponse> created = await service.CreateAsync(TestData.Ana, input, CancellationToken.None);

        Assert.IsTrue(created.Succeeded);
        Assert.AreEqual("Táxi até o cliente", repository.Stored(created.Value.Id).Description);

        ExpenseInput edited = input with { Description = "  Táxi de volta ao escritório  " };
        ServiceResult<ExpenseResponse> updated = await service.UpdateAsync(TestData.Ana, created.Value.Id, edited, CancellationToken.None);

        Assert.IsTrue(updated.Succeeded);
        Assert.AreEqual("Táxi de volta ao escritório", repository.Stored(created.Value.Id).Description);
    }
}
