using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Corpo de <c>POST /api/expenses/{id}/reject</c>.
/// </summary>
internal sealed class RejectExpenseRequest
{
    /// <summary>Justificativa obrigatória da reprovação (10 a 500 caracteres).</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Justification { get; set; } = string.Empty;
}
