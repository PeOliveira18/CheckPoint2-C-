using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Campos editáveis de um reembolso, usados na criação e na edição do rascunho.
/// Proprietário, estado, atores e horários não fazem parte do contrato e são definidos pelo servidor.
/// </summary>
internal sealed class ExpenseRequest
{
    /// <summary>Descrição da despesa (10 a 500 caracteres).</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Valor entre R$ 0,01 e R$ 2.147.483.647,00.</summary>
    [Range(typeof(decimal), "0.01", "2147483647", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    /// <summary>Data da despesa (não pode ser futura).</summary>
    [Required]
    public DateOnly? ExpenseDate { get; set; }

    /// <summary>Categoria da despesa.</summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
