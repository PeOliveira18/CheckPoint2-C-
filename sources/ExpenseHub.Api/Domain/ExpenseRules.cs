using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Regras de validação dos campos de um reembolso, independentes de HTTP e de banco.
/// </summary>
internal static class ExpenseRules
{
    /// <summary>Tamanho mínimo da descrição e da justificativa.</summary>
    public const int MinTextLength = 10;

    /// <summary>Tamanho máximo da descrição e da justificativa.</summary>
    public const int MaxTextLength = 500;

    /// <summary>Menor valor aceito (R$ 0,01).</summary>
    public const decimal MinAmount = 0.01m;

    /// <summary>Maior valor aceito (<see cref="int.MaxValue"/>).</summary>
    public const decimal MaxAmount = int.MaxValue;

    /// <summary>
    /// Valida descrição, valor e data de uma despesa.
    /// </summary>
    /// <param name="description">Descrição informada.</param>
    /// <param name="amount">Valor informado.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="today">Data atual do servidor (UTC).</param>
    /// <returns>Erros por campo; vazio quando válido.</returns>
    public static Dictionary<string, string[]> ValidateExpense(string? description, decimal amount, DateOnly expenseDate, DateOnly today)
    {
        Dictionary<string, string[]> errors = [];
        if (!IsValidText(description))
        {
            errors["description"] = [$"A descrição deve ter entre {MinTextLength} e {MaxTextLength} caracteres."];
        }

        if (amount < MinAmount || amount > MaxAmount)
        {
            errors["amount"] = ["O valor deve estar entre R$ 0,01 e R$ 2.147.483.647,00."];
        }

        if (expenseDate > today)
        {
            errors["expenseDate"] = ["A data da despesa não pode ser futura."];
        }

        return errors;
    }

    /// <summary>
    /// Valida a justificativa obrigatória da reprovação.
    /// </summary>
    /// <param name="justification">Justificativa informada.</param>
    /// <returns>Erros por campo; vazio quando válida.</returns>
    public static Dictionary<string, string[]> ValidateJustification(string? justification)
    {
        Dictionary<string, string[]> errors = [];
        if (!IsValidText(justification))
        {
            errors["justification"] = [$"A justificativa deve ter entre {MinTextLength} e {MaxTextLength} caracteres."];
        }

        return errors;
    }

    private static bool IsValidText(string? text)
    {
        int length = text?.Trim().Length ?? 0;
        return length is >= MinTextLength and <= MaxTextLength;
    }
}
