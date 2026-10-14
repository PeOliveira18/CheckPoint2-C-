using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseHub.Api.Auth;

internal static class Policies
{
    public const string Admin = "Admin";

    public const string ExpenseOwner = "ExpenseOwner";

    public const string ExpenseReader = "ExpenseReader";

    public const string ExpenseApprover = "ExpenseApprover";

    public const string ExpenseFinance = "ExpenseFinance";

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
