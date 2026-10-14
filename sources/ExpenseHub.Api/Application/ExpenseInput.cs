using System;

namespace ExpenseHub.Api.Application;

internal sealed record ExpenseInput(string Description, decimal Amount, DateOnly ExpenseDate, int CategoryId);
