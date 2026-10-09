using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Entrada do histórico de um reembolso.
/// </summary>
/// <param name="Id">Identificador da entrada.</param>
/// <param name="ExpenseId">Reembolso.</param>
/// <param name="Action">Ação executada.</param>
/// <param name="ActorId">Usuário que executou.</param>
/// <param name="OccurredAtUtc">Instante (UTC).</param>
/// <param name="FromStatus">Estado anterior.</param>
/// <param name="ToStatus">Estado posterior.</param>
/// <param name="Justification">Justificativa da reprovação.</param>
/// <param name="Changes">Alterações feitas em Draft.</param>
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
    /// <summary>
    /// Converte a entidade para a resposta.
    /// </summary>
    /// <param name="history">Entrada de histórico.</param>
    /// <returns>Resposta.</returns>
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
