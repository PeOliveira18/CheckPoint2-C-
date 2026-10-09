using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Entrada imutável do histórico de um reembolso.
/// </summary>
internal sealed class ExpenseHistory
{
    /// <summary>Identificador da entrada.</summary>
    public Guid Id { get; set; }

    /// <summary>Reembolso relacionado.</summary>
    public Guid ExpenseId { get; set; }

    /// <summary>Ação executada.</summary>
    public ExpenseAction Action { get; set; }

    /// <summary>Usuário que executou a ação.</summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>Instante da ação (UTC).</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Estado anterior; nulo na criação.</summary>
    public ExpenseStatus? FromStatus { get; set; }

    /// <summary>Estado posterior.</summary>
    public ExpenseStatus ToStatus { get; set; }

    /// <summary>Justificativa da reprovação.</summary>
    public string? Justification { get; set; }

    /// <summary>Descrição das alterações feitas em Draft.</summary>
    public string? Changes { get; set; }
}
