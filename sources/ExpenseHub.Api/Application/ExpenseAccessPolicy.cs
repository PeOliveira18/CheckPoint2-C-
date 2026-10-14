using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application;

internal static class ExpenseAccessPolicy
{
    public static Expression<Func<Expense, bool>> VisibleTo(UserContext user)
    {
        string userId = user.UserId;
        bool isAuditor = user.IsInRole(RoleNames.Auditor);
        bool isEmployee = user.IsInRole(RoleNames.Employee);
        bool isApprover = user.IsInRole(RoleNames.Approver);
        bool isFinance = user.IsInRole(RoleNames.Finance);

        return expense =>
            isAuditor ||
            (isEmployee && expense.OwnerId == userId) ||
            (isApprover && expense.Status == ExpenseStatus.Submitted) ||
            (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }

    public static bool CanView(Expense expense, UserContext user) =>
        user.IsInRole(RoleNames.Auditor) ||
        (user.IsInRole(RoleNames.Employee) && expense.OwnerId == user.UserId) ||
        (user.IsInRole(RoleNames.Approver) && expense.Status == ExpenseStatus.Submitted) ||
        (user.IsInRole(RoleNames.Finance) && expense.Status is ExpenseStatus.Approved or ExpenseStatus.Paid);
}
