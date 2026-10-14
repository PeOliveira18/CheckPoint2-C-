using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

internal sealed record ExpenseResponse(
    Guid Id,
    string OwnerId,
    int CategoryId,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpenseStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? DecidedById,
    DateTime? DecidedAtUtc,
    string? RejectionReason,
    PaymentResponse? Payment)
{
    public static ExpenseResponse From(Expense expense) =>
        new(
            expense.Id,
            expense.OwnerId,
            expense.CategoryId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.Status,
            expense.CreatedAtUtc,
            expense.UpdatedAtUtc,
            expense.DecidedById,
            expense.DecidedAtUtc,
            expense.RejectionReason,
            PaymentResponse.From(expense.Payment));
}
