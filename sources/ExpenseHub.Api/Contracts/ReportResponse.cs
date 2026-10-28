using System;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;

namespace ExpenseHub.Api.Contracts;

internal sealed record ReportResponse(
    Guid Id,
    ReportScope Scope,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    ReportResult? Result,
    ReportErrorResponse? Error)
{
    public static ReportResponse From(ReportRequest report) =>
        new(
            report.Id,
            report.Scope,
            report.FromDate,
            report.ToDate,
            report.Status,
            AsUtc(report.CreatedAtUtc),
            report.StartedAtUtc is { } startedAt ? AsUtc(startedAt) : null,
            report.CompletedAtUtc is { } completedAt ? AsUtc(completedAt) : null,
            report.Status == ReportStatus.Completed && report.ResultJson is not null
                ? ReportResultJson.Deserialize(report.ResultJson)
                : null,
            report.Status == ReportStatus.Failed
                ? new ReportErrorResponse(report.ErrorCode ?? "unknown", report.ErrorMessage ?? "Falha no processamento.")
                : null);

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
