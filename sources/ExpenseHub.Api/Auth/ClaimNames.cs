namespace ExpenseHub.Api.Auth;

/// <summary>
/// Nomes das claims emitidas no token de acesso.
/// </summary>
internal static class ClaimNames
{
    /// <summary>Identificador do usuário.</summary>
    public const string Subject = "sub";

    /// <summary>E-mail do usuário.</summary>
    public const string Email = "email";

    /// <summary>Role do usuário (uma claim por role).</summary>
    public const string Role = "role";

    /// <summary>Security stamp do Identity no momento do login.</summary>
    public const string SecurityStamp = "sstamp";
}
