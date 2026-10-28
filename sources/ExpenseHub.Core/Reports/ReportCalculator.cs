using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Core.Reports;

internal static class ReportCalculator
{
    public static Expression<Func<Expense, bool>> Eligible(ReportRequest report)
    {
        DateOnly fromDate = report.FromDate;
        DateOnly toDate = report.ToDate;
        bool isGlobal = report.Scope == ReportScope.Global;
        string requesterId = report.RequesterId;

        return expense =>
            expense.ExpenseDate >= fromDate &&
            expense.ExpenseDate <= toDate &&
            (isGlobal || expense.OwnerId == requesterId);
    }

    public static ReportResult Calculate(ReportRequest report, IEnumerable<Expense> expenses, DateTime generatedAtUtc) =>
        Summarize(expenses.AsQueryable().Where(Eligible(report)), generatedAtUtc);

    public static ReportResult Summarize(IEnumerable<Expense> expenses, DateTime generatedAtUtc)
    {
        Dictionary<ExpenseStatus, int> counts = [];
        Dictionary<ExpenseStatus, decimal> amounts = [];
        foreach (Expense expense in expenses)
        {
            counts[expense.Status] = counts.GetValueOrDefault(expense.Status) + 1;
            amounts[expense.Status] = amounts.GetValueOrDefault(expense.Status) + expense.Amount;
        }

        List<ReportStatusTotal> byStatus = Enum.GetValues<ExpenseStatus>()
            .Select(status => new ReportStatusTotal(status, counts.GetValueOrDefault(status), amounts.GetValueOrDefault(status)))
            .ToList();

        return new ReportResult(
            generatedAtUtc,
            byStatus.Sum(total => total.Count),
            byStatus.Sum(total => total.Amount),
            byStatus);
    }
}
