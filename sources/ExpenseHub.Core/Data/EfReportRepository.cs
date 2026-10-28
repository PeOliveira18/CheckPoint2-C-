using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Core.Data;

internal sealed class EfReportRepository : IReportRepository
{
    private readonly ExpenseHubDbContext _dbContext;

    public EfReportRepository(ExpenseHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(ReportRequest report) => _dbContext.ReportRequests.Add(report);

    public Task<ReportRequest?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.ReportRequests.FirstOrDefaultAsync(report => report.Id == id, cancellationToken);

    public Task<ReportRequest?> FindOwnAsync(Guid id, string requesterId, CancellationToken cancellationToken) =>
        _dbContext.ReportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.Id == id && report.RequesterId == requesterId, cancellationToken);

    public async Task<IReadOnlyList<ReportRequest>> ListOwnAsync(string requesterId, CancellationToken cancellationToken) =>
        await _dbContext.ReportRequests
            .AsNoTracking()
            .Where(report => report.RequesterId == requesterId)
            .OrderByDescending(report => report.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> ListExpensesAsync(
        Expression<Func<Expense, bool>> criteria,
        CancellationToken cancellationToken) =>
        await _dbContext.Expenses
            .AsNoTracking()
            .Where(criteria)
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
