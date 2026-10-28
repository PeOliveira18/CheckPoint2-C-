using ExpenseHub.Api.Contracts;

namespace ExpenseHub.Api.Application;

internal sealed record ReportSubmission(ReportResponse Report, bool Published);
