using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Representação de um reembolso nas respostas da API.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="OwnerId">Proprietário.</param>
/// <param name="CategoryId">Categoria.</param>
/// <param name="Description">Descrição.</param>
/// <param name="Amount">Valor.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="Status">Estado atual.</param>
/// <param name="CreatedAtUtc">Criação (UTC).</param>
/// <param name="UpdatedAtUtc">Última alteração (UTC).</param>
/// <param name="DecidedById">Approver que decidiu.</param>
/// <param name="DecidedAtUtc">Instante da decisão (UTC).</param>
/// <param name="RejectionReason">Justificativa da reprovação.</param>
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
    string? RejectionReason)
{
    /// <summary>
    /// Converte a entidade para a resposta.
    /// </summary>
    /// <param name="expense">Reembolso.</param>
    /// <returns>Resposta.</returns>
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
            expense.RejectionReason);
}
