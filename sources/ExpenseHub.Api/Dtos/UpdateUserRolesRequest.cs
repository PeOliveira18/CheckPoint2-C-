using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

internal sealed class UpdateUserRolesRequest
{
    [Required]
    [MaxLength(5)]
    public IList<string> Roles { get; init; } = [];
}
