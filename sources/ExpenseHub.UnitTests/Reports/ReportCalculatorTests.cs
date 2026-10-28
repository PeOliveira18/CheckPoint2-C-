using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — cálculo do resumo de reembolsos por estado e totais.
/// </summary>
[TestClass]
public sealed class ReportCalculatorTests
{
    private static readonly DateTime _generatedAt = new(2026, 10, 1, 12, 0, 2, DateTimeKind.Utc);

    /// <summary>Exemplo da apostila em Mine: 3 reembolsos, R$ 175,00 (2 Approved = 150, 1 Paid = 25).</summary>
    [TestMethod]
    public void MineMatchesApostilaExample()
    {
        ReportResult result = Calculate(ReportScope.Mine, ReportScenario.ApostilaExpenses());

        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(175.00m, result.TotalAmount);
        AssertStatus(result, ExpenseStatus.Approved, 2, 150.00m);
        AssertStatus(result, ExpenseStatus.Paid, 1, 25.00m);
        AssertStatus(result, ExpenseStatus.Draft, 0, 0m);
        AssertStatus(result, ExpenseStatus.Submitted, 0, 0m);
        AssertStatus(result, ExpenseStatus.Rejected, 0, 0m);
    }

    /// <summary>Exemplo da apostila em Global: 4 reembolsos, R$ 375,00, mantendo a exclusão fora do período.</summary>
    [TestMethod]
    public void GlobalMatchesApostilaExample()
    {
        ReportResult result = Calculate(ReportScope.Global, ReportScenario.ApostilaExpenses());

        Assert.AreEqual(4, result.TotalCount);
        Assert.AreEqual(375.00m, result.TotalAmount);
        AssertStatus(result, ExpenseStatus.Approved, 3, 350.00m);
        AssertStatus(result, ExpenseStatus.Paid, 1, 25.00m);
    }

    /// <summary>As duas datas do período são inclusivas; o dia anterior e o posterior ficam de fora.</summary>
    [TestMethod]
    public void PeriodLimitsAreInclusive()
    {
        List<Expense> expenses =
        [
            TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted, 10.00m, new DateOnly(2026, 8, 31)),
            TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted, 20.00m, new DateOnly(2026, 9, 1)),
            TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted, 30.00m, new DateOnly(2026, 9, 30)),
            TestData.Expense(TestData.AnaId, ExpenseStatus.Submitted, 40.00m, new DateOnly(2026, 10, 1)),
        ];

        ReportResult result = Calculate(ReportScope.Mine, expenses);

        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(50.00m, result.TotalAmount);
    }

    /// <summary>O filtro usa a data da despesa, não a data de criação do registro.</summary>
    [TestMethod]
    public void FilterUsesExpenseDateInsteadOfCreationDate()
    {
        Expense createdInPeriod = TestData.Expense(TestData.AnaId, ExpenseStatus.Approved, 70.00m, new DateOnly(2026, 8, 20));
        createdInPeriod.CreatedAtUtc = new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc);
        Expense datedInPeriod = TestData.Expense(TestData.AnaId, ExpenseStatus.Approved, 30.00m, new DateOnly(2026, 9, 25));
        datedInPeriod.CreatedAtUtc = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);

        ReportResult result = Calculate(ReportScope.Mine, [createdInPeriod, datedInPeriod]);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(30.00m, result.TotalAmount);
    }

    /// <summary>Sem ocorrências: totais zero e os cinco estados presentes com zero.</summary>
    [TestMethod]
    public void EmptySetReturnsZerosForAllFiveStatuses()
    {
        ReportResult result = Calculate(ReportScope.Global, []);

        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0m, result.TotalAmount);
        CollectionAssert.AreEquivalent(Enum.GetValues<ExpenseStatus>(), result.ByStatus.Select(total => total.Status).ToList());
        Assert.IsTrue(result.ByStatus.All(total => total.Count == 0 && total.Amount == 0m));
    }

    /// <summary>Valores decimais somam sem erro de ponto flutuante.</summary>
    [TestMethod]
    public void DecimalAmountsSumWithoutFloatingPointError()
    {
        List<Expense> expenses =
        [
            TestData.Expense(TestData.AnaId, ExpenseStatus.Paid, 0.10m, new DateOnly(2026, 9, 2)),
            TestData.Expense(TestData.AnaId, ExpenseStatus.Paid, 0.20m, new DateOnly(2026, 9, 3)),
        ];

        ReportResult result = Calculate(ReportScope.Mine, expenses);

        Assert.AreEqual(0.30m, result.TotalAmount);
        AssertStatus(result, ExpenseStatus.Paid, 2, 0.30m);
    }

    /// <summary>O instante de geração informado é registrado no resultado.</summary>
    [TestMethod]
    public void ResultCarriesGenerationInstant()
    {
        Assert.AreEqual(_generatedAt, Calculate(ReportScope.Mine, []).GeneratedAt);
    }

    /// <summary>O resultado sobrevive à serialização usada para persistência sem perder valores.</summary>
    [TestMethod]
    public void ResultRoundTripsThroughPersistedJson()
    {
        ReportResult result = Calculate(ReportScope.Global, ReportScenario.ApostilaExpenses());

        ReportResult restored = ReportResultJson.Deserialize(ReportResultJson.Serialize(result));

        Assert.AreEqual(result.GeneratedAt, restored.GeneratedAt);
        Assert.AreEqual(result.TotalCount, restored.TotalCount);
        Assert.AreEqual(result.TotalAmount, restored.TotalAmount);
        CollectionAssert.AreEqual(result.ByStatus.ToList(), restored.ByStatus.ToList());
    }

    private static ReportResult Calculate(ReportScope scope, IEnumerable<Expense> expenses) =>
        ReportCalculator.Calculate(
            TestData.Report(TestData.AnaId, scope, ReportScenario.From, ReportScenario.To),
            expenses,
            _generatedAt);

    private static void AssertStatus(ReportResult result, ExpenseStatus status, int count, decimal amount)
    {
        ReportStatusTotal total = result.ByStatus.Single(item => item.Status == status);
        Assert.AreEqual(count, total.Count, $"Quantidade de {status}");
        Assert.AreEqual(amount, total.Amount, $"Valor de {status}");
    }
}
