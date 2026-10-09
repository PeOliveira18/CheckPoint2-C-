using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Policies de role aplicadas nas rotas. São a primeira barreira; ownership e estado são verificados no serviço.
/// </summary>
internal static class Policies
{
    /// <summary>Somente Admin.</summary>
    public const string Admin = "Admin";

    /// <summary>Employee: criar, editar e enviar.</summary>
    public const string ExpenseOwner = "ExpenseOwner";

    /// <summary>Qualquer role com escopo de leitura de reembolsos (Admin não incluído).</summary>
    public const string ExpenseReader = "ExpenseReader";

    /// <summary>Approver: aprovar e reprovar.</summary>
    public const string ExpenseApprover = "ExpenseApprover";

    /// <summary>Finance: registrar pagamento.</summary>
    public const string ExpenseFinance = "ExpenseFinance";

    /// <summary>
    /// Registra as policies.
    /// </summary>
    /// <param name="options">Opções de autorização.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, policy => policy.RequireRole(RoleNames.Admin));
        options.AddPolicy(ExpenseOwner, policy => policy.RequireRole(RoleNames.Employee));
        options.AddPolicy(
            ExpenseReader,
            policy => policy.RequireRole(RoleNames.Employee, RoleNames.Approver, RoleNames.Finance, RoleNames.Auditor));
        options.AddPolicy(ExpenseApprover, policy => policy.RequireRole(RoleNames.Approver));
        options.AddPolicy(ExpenseFinance, policy => policy.RequireRole(RoleNames.Finance));
    }
}
