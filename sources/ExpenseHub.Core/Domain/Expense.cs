using System;
using System.Collections.Generic;

namespace ExpenseHub.Core.Domain;

internal sealed class Expense
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public ExpenseCategory? Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public ExpenseStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? DecidedById { get; set; }

    public DateTime? DecidedAtUtc { get; set; }

    public string? RejectionReason { get; set; }

    public Guid ConcurrencyStamp { get; set; }

    public ICollection<ExpenseHistory> History { get; } = new List<ExpenseHistory>();

    public PaymentRecord? Payment { get; set; }
}
