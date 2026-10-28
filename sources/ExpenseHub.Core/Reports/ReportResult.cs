using System;
using System.Collections.Generic;

namespace ExpenseHub.Core.Reports;

internal sealed record ReportResult(
    DateTime GeneratedAt,
    int TotalCount,
    decimal TotalAmount,
    IReadOnlyList<ReportStatusTotal> ByStatus);
