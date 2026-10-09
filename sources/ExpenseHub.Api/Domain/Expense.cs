using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso com um único valor.
/// </summary>
internal sealed class Expense
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário proprietário, obtido do token.</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Categoria da despesa.</summary>
    public int CategoryId { get; set; }

    /// <summary>Navegação para a categoria.</summary>
    public ExpenseCategory? Category { get; set; }

    /// <summary>Descrição da despesa (10 a 500 caracteres).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Valor solicitado.</summary>
    public decimal Amount { get; set; }

    /// <summary>Data em que a despesa ocorreu.</summary>
    public DateOnly ExpenseDate { get; set; }

    /// <summary>Estado atual, definido exclusivamente pelo servidor.</summary>
    public ExpenseStatus Status { get; set; }

    /// <summary>Instante de criação (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Instante da última alteração (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Approver que aprovou ou reprovou.</summary>
    public string? DecidedById { get; set; }

    /// <summary>Instante da decisão (UTC).</summary>
    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Justificativa da reprovação.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Token de concorrência renovado a cada alteração.</summary>
    public Guid ConcurrencyStamp { get; set; }

    /// <summary>Histórico de ações do reembolso.</summary>
    public ICollection<ExpenseHistory> History { get; } = new List<ExpenseHistory>();

    /// <summary>Pagamento registrado, quando existir.</summary>
    public PaymentRecord? Payment { get; set; }
}
