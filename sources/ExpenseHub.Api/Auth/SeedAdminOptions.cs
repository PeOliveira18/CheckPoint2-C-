namespace ExpenseHub.Api.Auth;

/// <summary>
/// Dados da conta Admin inicial (seção <c>SeedAdmin</c>). A senha vem de User Secrets ou variável de ambiente.
/// </summary>
internal sealed class SeedAdminOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "SeedAdmin";

    /// <summary>E-mail (e nome de usuário) do Admin inicial.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha inicial do Admin; nunca versionada.</summary>
    public string Password { get; set; } = string.Empty;
}
