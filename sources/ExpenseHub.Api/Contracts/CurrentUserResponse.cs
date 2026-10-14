using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts;

internal sealed record CurrentUserResponse(string Id, string Email, IReadOnlyList<string> Roles);
