using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto completo de roles que o usuário deve possuir após <c>PUT /api/admin/users/{id}/roles</c>.
/// </summary>
internal sealed class UpdateUserRolesRequest
{
    /// <summary>Roles desejadas; roles ausentes da lista são removidas.</summary>
    [Required]
    [MaxLength(5)]
    public IList<string> Roles { get; init; } = [];
}
