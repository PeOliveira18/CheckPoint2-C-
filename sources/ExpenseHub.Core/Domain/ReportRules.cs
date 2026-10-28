using System;
using System.Collections.Generic;

namespace ExpenseHub.Core.Domain;

internal static class ReportRules
{
    public static Dictionary<string, string[]> ValidateRequest(DateOnly? fromDate, DateOnly? toDate, ReportScope? scope)
    {
        Dictionary<string, string[]> errors = [];
        if (fromDate is null)
        {
            errors["fromDate"] = ["A data inicial é obrigatória (ano-mês-dia)."];
        }

        if (toDate is null)
        {
            errors["toDate"] = ["A data final é obrigatória (ano-mês-dia)."];
        }

        if (fromDate is not null && toDate is not null && !IsValidPeriod(fromDate.Value, toDate.Value))
        {
            errors["toDate"] = ["A data final deve ser igual ou posterior à data inicial."];
        }

        if (scope is null || !Enum.IsDefined(scope.Value))
        {
            errors["scope"] = ["O escopo deve ser Mine ou Global."];
        }

        return errors;
    }

    public static bool IsValidPeriod(DateOnly fromDate, DateOnly toDate) => fromDate <= toDate;
}
