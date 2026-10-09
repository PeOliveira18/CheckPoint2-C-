using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Escopo de leitura dos reembolsos por role. Roles se acumulam: o usuário enxerga a união dos escopos.
/// </summary>
/// <remarks>
/// Employee: próprios; Approver: <see cref="ExpenseStatus.Submitted"/>; Finance: <see cref="ExpenseStatus.Approved"/>
/// e <see cref="ExpenseStatus.Paid"/>; Auditor: todos; Admin: nenhum (sem acesso funcional implícito).
/// </remarks>
internal static class ExpenseAccessPolicy
{
    /// <summary>
    /// Filtro traduzível para SQL, aplicado no banco antes de materializar os dados.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns>Expressão de visibilidade.</returns>
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

    /// <summary>
    /// Mesma regra de <see cref="VisibleTo"/> avaliada sobre um reembolso já carregado.
    /// </summary>
    /// <param name="expense">Reembolso.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns><see langword="true"/> se o reembolso está no escopo de leitura do usuário.</returns>
    public static bool CanView(Expense expense, UserContext user) =>
        user.IsInRole(RoleNames.Auditor) ||
        (user.IsInRole(RoleNames.Employee) && expense.OwnerId == user.UserId) ||
        (user.IsInRole(RoleNames.Approver) && expense.Status == ExpenseStatus.Submitted) ||
        (user.IsInRole(RoleNames.Finance) && expense.Status is ExpenseStatus.Approved or ExpenseStatus.Paid);
}
