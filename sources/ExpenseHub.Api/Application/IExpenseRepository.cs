using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Application;

internal interface IExpenseRepository
{
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Expense?> FindVisibleAsync(Guid id, Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken);

    void Add(Expense expense);

    void AddHistory(ExpenseHistory history);

    void AddPayment(PaymentRecord payment);

    Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
