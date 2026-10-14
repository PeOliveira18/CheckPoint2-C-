using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ExpenseHub.Api.Auth;

namespace ExpenseHub.Api.Application;

internal sealed class UserContext
{
    public UserContext(string userId, IEnumerable<string> roles)
    {
        UserId = userId;
        Roles = new HashSet<string>(roles, StringComparer.Ordinal);
    }

    public string UserId { get; }

    public IReadOnlySet<string> Roles { get; }

    public static UserContext FromPrincipal(ClaimsPrincipal principal) =>
        new(
            principal.FindFirstValue(ClaimNames.Subject) ?? string.Empty,
            principal.FindAll(ClaimNames.Role).Select(claim => claim.Value));

    public bool IsInRole(string role) => Roles.Contains(role);
}
