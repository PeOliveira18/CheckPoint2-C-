using ExpenseHub.Core.Domain;

namespace ExpenseHub.Core.Reports;

internal sealed record ReportStatusTotal(ExpenseStatus Status, int Count, decimal Amount);
