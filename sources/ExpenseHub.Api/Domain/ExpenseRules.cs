using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

internal static class ExpenseRules
{
    public const int MinTextLength = 10;

    public const int MaxTextLength = 500;

    public const decimal MinAmount = 0.01m;

    public const decimal MaxAmount = int.MaxValue;

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
