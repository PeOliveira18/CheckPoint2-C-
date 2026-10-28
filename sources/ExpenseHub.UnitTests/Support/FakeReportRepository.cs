using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Domain;
using ExpenseHub.Core.Reports;

namespace ExpenseHub.UnitTests.Support;

internal sealed class FakeReportRepository : IReportRepository
{
    private readonly Dictionary<Guid, ReportRequest> _stored = [];
    private readonly Dictionary<Guid, ReportRequest> _tracked = [];
    private readonly List<Expense> _expenses = [];

    public bool FailNextSave { get; set; }

    public bool ThrowOnSave { get; set; }

    public int SaveCount { get; private set; }

    public int ExpenseQueryCount { get; private set; }

    public int StoredCount => _stored.Count;

    public void Seed(ReportRequest report) => _stored[report.Id] = Clone(report);

    public ReportRequest Stored(Guid id) => Clone(_stored[id]);

    public void SeedExpense(Expense expense) => _expenses.Add(expense);

    public void Add(ReportRequest report) => _tracked[report.Id] = report;

    public Task<ReportRequest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_stored.TryGetValue(id, out ReportRequest? stored))
        {
            return Task.FromResult<ReportRequest?>(null);
        }

        ReportRequest copy = Clone(stored);
        _tracked[id] = copy;
        return Task.FromResult<ReportRequest?>(copy);
    }

    public Task<ReportRequest?> FindOwnAsync(Guid id, string requesterId, CancellationToken cancellationToken)
    {
        ReportRequest? match = _stored.Values.FirstOrDefault(report =>
            report.Id == id && string.Equals(report.RequesterId, requesterId, StringComparison.Ordinal));
        return Task.FromResult(match is null ? null : Clone(match));
    }

    public Task<IReadOnlyList<ReportRequest>> ListOwnAsync(string requesterId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReportRequest> result = _stored.Values
            .Where(report => string.Equals(report.RequesterId, requesterId, StringComparison.Ordinal))
            .OrderByDescending(report => report.CreatedAtUtc)
            .Select(Clone)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Expense>> ListExpensesAsync(
        Expression<Func<Expense, bool>> criteria,
        CancellationToken cancellationToken)
    {
        ExpenseQueryCount++;
        IReadOnlyList<Expense> result = _expenses.AsQueryable().Where(criteria).ToList();
        return Task.FromResult(result);
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowOnSave)
        {
            throw new InvalidOperationException("Banco indisponível.");
        }

        if (FailNextSave)
        {
            FailNextSave = false;
            _tracked.Clear();
            return Task.FromResult(false);
        }

        foreach (ReportRequest report in _tracked.Values)
        {
            _stored[report.Id] = Clone(report);
        }

        SaveCount++;
        return Task.FromResult(true);
    }

    private static ReportRequest Clone(ReportRequest source) =>
        new()
        {
            Id = source.Id,
            RequesterId = source.RequesterId,
            Scope = source.Scope,
            FromDate = source.FromDate,
            ToDate = source.ToDate,
            Status = source.Status,
            CreatedAtUtc = source.CreatedAtUtc,
            StartedAtUtc = source.StartedAtUtc,
            CompletedAtUtc = source.CompletedAtUtc,
            ResultJson = source.ResultJson,
            ErrorCode = source.ErrorCode,
            ErrorMessage = source.ErrorMessage,
            Attempts = source.Attempts,
            ConcurrencyStamp = source.ConcurrencyStamp,
        };
}
