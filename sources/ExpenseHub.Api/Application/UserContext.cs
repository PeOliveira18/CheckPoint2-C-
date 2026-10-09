using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ExpenseHub.Api.Auth;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Identidade autenticada usada pelas regras de serviço (derivada do token, nunca do corpo da requisição).
/// </summary>
internal sealed class UserContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserContext"/> class.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="roles">Roles do usuário.</param>
    public UserContext(string userId, IEnumerable<string> roles)
    {
        UserId = userId;
        Roles = new HashSet<string>(roles, StringComparer.Ordinal);
    }

    /// <summary>Identificador do usuário autenticado.</summary>
    public string UserId { get; }

    /// <summary>Roles do usuário autenticado.</summary>
    public IReadOnlySet<string> Roles { get; }

    /// <summary>
    /// Cria o contexto a partir das claims do token validado.
    /// </summary>
    /// <param name="principal">Usuário autenticado.</param>
    /// <returns>Contexto do usuário.</returns>
    public static UserContext FromPrincipal(ClaimsPrincipal principal) =>
        new(
            principal.FindFirstValue(ClaimNames.Subject) ?? string.Empty,
            principal.FindAll(ClaimNames.Role).Select(claim => claim.Value));

    /// <summary>
    /// Indica se o usuário possui a role.
    /// </summary>
    /// <param name="role">Role avaliada.</param>
    /// <returns><see langword="true"/> se possuir.</returns>
    public bool IsInRole(string role) => Roles.Contains(role);
}
