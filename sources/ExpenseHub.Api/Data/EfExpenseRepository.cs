using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Core.Data;
using ExpenseHub.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

internal sealed class EfExpenseRepository : IExpenseRepository
{
    private readonly ExpenseHubDbContext _dbContext;

    public EfExpenseRepository(ExpenseHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        _dbContext.ExpenseCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Expenses
            .Include(expense => expense.Payment)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

    public Task<Expense?> FindVisibleAsync(Guid id, Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken) =>
        _dbContext.Expenses
            .AsNoTracking()
            .Include(expense => expense.Payment)
            .Where(visibility)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken) =>
        await _dbContext.Expenses
            .AsNoTracking()
            .Include(expense => expense.Payment)
            .Where(visibility)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Expense expense) => _dbContext.Expenses.Add(expense);

    public void AddHistory(ExpenseHistory history) => _dbContext.ExpenseHistory.Add(history);

    public void AddPayment(PaymentRecord payment) => _dbContext.PaymentRecords.Add(payment);

    public async Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken) =>
        await _dbContext.ExpenseHistory
            .AsNoTracking()
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
