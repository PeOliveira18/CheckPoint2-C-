using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Core.Reports;

internal interface IReportRepository
{
    void Add(ReportRequest report);

    Task<ReportRequest?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<ReportRequest?> FindOwnAsync(Guid id, string requesterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportRequest>> ListOwnAsync(string requesterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> ListExpensesAsync(Expression<Func<Expense, bool>> criteria, CancellationToken cancellationToken);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
