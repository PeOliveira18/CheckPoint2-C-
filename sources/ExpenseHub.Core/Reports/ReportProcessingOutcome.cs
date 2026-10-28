namespace ExpenseHub.Core.Reports;

internal enum ReportProcessingOutcome
{
    Completed,

    Failed,

    AlreadyFinished,

    NotFound,

    Conflict,
}
