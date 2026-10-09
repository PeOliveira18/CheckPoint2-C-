using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Dados do usuário autenticado conforme o token.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Email">E-mail do usuário.</param>
/// <param name="Roles">Roles presentes no token.</param>
internal sealed record CurrentUserResponse(string Id, string Email, IReadOnlyList<string> Roles);
