using System;

namespace ExpenseHub.Core.Domain;

internal sealed class ReportRequest
{
    public Guid Id { get; set; }

    public string RequesterId { get; set; } = string.Empty;

    public ReportScope Scope { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public ReportStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? ResultJson { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int Attempts { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
