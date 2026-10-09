using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro de pagamento simulado de um reembolso aprovado.
/// </summary>
internal sealed class PaymentRecord
{
    /// <summary>Identificador do pagamento.</summary>
    public Guid Id { get; set; }

    /// <summary>Reembolso pago (no máximo um pagamento por reembolso).</summary>
    public Guid ExpenseId { get; set; }

    /// <summary>Usuário do Finance que registrou o pagamento.</summary>
    public string PaidById { get; set; } = string.Empty;

    /// <summary>Instante do pagamento (UTC).</summary>
    public DateTime PaidAtUtc { get; set; }

    /// <summary>Valor pago.</summary>
    public decimal Amount { get; set; }
}
