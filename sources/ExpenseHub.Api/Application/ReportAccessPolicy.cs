using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Application;

internal static class ReportAccessPolicy
{
    public static bool CanRequest(UserContext user, ReportScope scope) =>
        scope switch
        {
            ReportScope.Mine => user.IsInRole(RoleNames.Employee),
            ReportScope.Global => user.IsInRole(RoleNames.Auditor),
            _ => false,
        };
}
