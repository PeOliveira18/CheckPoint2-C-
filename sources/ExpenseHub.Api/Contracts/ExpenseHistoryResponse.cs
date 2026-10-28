using System;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Contracts;

internal sealed record ExpenseHistoryResponse(
    Guid Id,
    Guid ExpenseId,
    ExpenseAction Action,
    string ActorId,
    DateTime OccurredAtUtc,
    ExpenseStatus? FromStatus,
    ExpenseStatus ToStatus,
    string? Justification,
    string? Changes)
{
    public static ExpenseHistoryResponse From(ExpenseHistory history) =>
        new(
            history.Id,
            history.ExpenseId,
            history.Action,
            history.ActorId,
            history.OccurredAtUtc,
            history.FromStatus,
            history.ToStatus,
            history.Justification,
            history.Changes);
}
