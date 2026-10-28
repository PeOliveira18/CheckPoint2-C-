using System;
using System.Collections.Generic;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.UnitTests.Support;

internal static class ReportScenario
{
    public static DateOnly From { get; } = new(2026, 9, 1);

    public static DateOnly To { get; } = new(2026, 9, 30);

    public static List<Expense> ApostilaExpenses() =>
    [
        TestData.Expense(TestData.AnaId, ExpenseStatus.Approved, 100.00m, new DateOnly(2026, 9, 5)),
        TestData.Expense(TestData.AnaId, ExpenseStatus.Approved, 50.00m, new DateOnly(2026, 9, 10)),
        TestData.Expense(TestData.AnaId, ExpenseStatus.Paid, 25.00m, new DateOnly(2026, 9, 20)),
        TestData.Expense(TestData.BrunoId, ExpenseStatus.Approved, 200.00m, new DateOnly(2026, 9, 15)),
        TestData.Expense(TestData.AnaId, ExpenseStatus.Paid, 500.00m, new DateOnly(2026, 10, 2)),
    ];
}
