using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Implementação do <see cref="IExpenseRepository"/> com Entity Framework Core.
/// </summary>
internal sealed class EfExpenseRepository : IExpenseRepository
{
    private readonly ExpenseHubDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfExpenseRepository"/> class.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    public EfExpenseRepository(ExpenseHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        _dbContext.ExpenseCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    /// <inheritdoc />
    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Expenses
            .Include(expense => expense.Payment)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(Expense expense) => _dbContext.Expenses.Add(expense);

    /// <inheritdoc />
    public void AddHistory(ExpenseHistory history) => _dbContext.ExpenseHistory.Add(history);

    /// <inheritdoc />
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
