using System;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Campos editáveis de um reembolso recebidos pelo serviço.
/// </summary>
/// <param name="Description">Descrição.</param>
/// <param name="Amount">Valor.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="CategoryId">Categoria.</param>
internal sealed record ExpenseInput(string Description, decimal Amount, DateOnly ExpenseDate, int CategoryId);
