using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Pagamento registrado de um reembolso.
/// </summary>
/// <param name="Id">Identificador do pagamento.</param>
/// <param name="PaidById">Usuário do Finance que registrou.</param>
/// <param name="PaidAtUtc">Instante do pagamento (UTC).</param>
/// <param name="Amount">Valor pago.</param>
internal sealed record PaymentResponse(Guid Id, string PaidById, DateTime PaidAtUtc, decimal Amount)
{
    /// <summary>
    /// Converte a entidade para a resposta.
    /// </summary>
    /// <param name="payment">Pagamento.</param>
    /// <returns>Resposta ou nulo quando não há pagamento.</returns>
    public static PaymentResponse? From(PaymentRecord? payment) =>
        payment is null ? null : new(payment.Id, payment.PaidById, payment.PaidAtUtc, payment.Amount);
}
