using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Core.Reports;

internal sealed class ReportProcessor
{
    public const string InvalidPeriodCode = "invalid-period";

    private readonly IReportRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReportProcessor(IReportRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ReportProcessingOutcome> ProcessAsync(Guid reportId, CancellationToken cancellationToken)
    {
        ReportRequest? report = await _repository.FindAsync(reportId, cancellationToken);
        if (report is null)
        {
            return ReportProcessingOutcome.NotFound;
        }

        if (report.Status is ReportStatus.Completed or ReportStatus.Failed)
        {
            return ReportProcessingOutcome.AlreadyFinished;
        }

        report.Status = ReportStatus.Processing;
        report.StartedAtUtc ??= UtcNow();
        report.Attempts++;
        report.ConcurrencyStamp = Guid.NewGuid();
        if (!await _repository.SaveChangesAsync(cancellationToken))
        {
            return ReportProcessingOutcome.Conflict;
        }

        if (!ReportRules.IsValidPeriod(report.FromDate, report.ToDate))
        {
            Fail(report, InvalidPeriodCode, "O período registrado é inválido.");
            return await SaveAsync(ReportProcessingOutcome.Failed, cancellationToken);
        }

        IReadOnlyList<Expense> expenses = await _repository.ListExpensesAsync(ReportCalculator.Eligible(report), cancellationToken);
        DateTime now = UtcNow();
        ReportResult result = ReportCalculator.Summarize(expenses, now);

        report.Status = ReportStatus.Completed;
        report.CompletedAtUtc = now;
        report.ResultJson = ReportResultJson.Serialize(result);
        report.ErrorCode = null;
        report.ErrorMessage = null;
        report.ConcurrencyStamp = Guid.NewGuid();
        return await SaveAsync(ReportProcessingOutcome.Completed, cancellationToken);
    }

    private void Fail(ReportRequest report, string code, string message)
    {
        report.Status = ReportStatus.Failed;
        report.CompletedAtUtc = UtcNow();
        report.ResultJson = null;
        report.ErrorCode = code;
        report.ErrorMessage = message;
        report.ConcurrencyStamp = Guid.NewGuid();
    }

    private async Task<ReportProcessingOutcome> SaveAsync(ReportProcessingOutcome outcome, CancellationToken cancellationToken) =>
        await _repository.SaveChangesAsync(cancellationToken) ? outcome : ReportProcessingOutcome.Conflict;

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
