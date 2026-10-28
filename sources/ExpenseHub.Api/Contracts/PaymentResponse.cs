using System;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Contracts;

internal sealed record PaymentResponse(Guid Id, string PaidById, DateTime PaidAtUtc, decimal Amount)
{
    public static PaymentResponse? From(PaymentRecord? payment) =>
        payment is null ? null : new(payment.Id, payment.PaidById, payment.PaidAtUtc, payment.Amount);
}
