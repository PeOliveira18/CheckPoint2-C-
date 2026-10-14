using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Support;

internal sealed class FakeExpenseRepository : IExpenseRepository
{
    private readonly Dictionary<Guid, Expense> _stored = [];
    private readonly List<ExpenseHistory> _storedHistory = [];
    private readonly Dictionary<Guid, Expense> _tracked = [];
    private readonly List<ExpenseHistory> _pendingHistory = [];
    private readonly Dictionary<Guid, PaymentRecord> _storedPayments = [];
    private readonly List<PaymentRecord> _pendingPayments = [];

    public HashSet<int> Categories { get; } = [1, 2, 3];

    public bool FailNextSave { get; set; }

    public int SaveCount { get; private set; }

    public IReadOnlyList<ExpenseHistory> StoredHistory => _storedHistory;

    public void Seed(Expense expense) => _stored[expense.Id] = Clone(expense);

    public Expense Stored(Guid id) => Clone(_stored[id]);

    public IReadOnlyList<ExpenseHistory> HistoryOf(Guid id) => _storedHistory.Where(history => history.ExpenseId == id).ToList();

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.Contains(categoryId));

    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_stored.TryGetValue(id, out Expense? stored))
        {
            return Task.FromResult<Expense?>(null);
        }

        Expense copy = Clone(stored);
        _tracked[id] = copy;
        return Task.FromResult<Expense?>(copy);
    }

    public Expression<Func<Expense, bool>>? LastVisibilityFilter { get; private set; }

    public Task<Expense?> FindVisibleAsync(Guid id, Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken)
    {
        LastVisibilityFilter = visibility;
        Expense? match = _stored.Values.AsQueryable().Where(visibility).FirstOrDefault(expense => expense.Id == id);
        return Task.FromResult(match is null ? null : Clone(match));
    }

    public Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken)
    {
        LastVisibilityFilter = visibility;
        IReadOnlyList<Expense> result = _stored.Values.AsQueryable()
            .Where(visibility)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .Select(expense => Clone(expense))
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(Expense expense) => _tracked[expense.Id] = expense;

    public void AddHistory(ExpenseHistory history) => _pendingHistory.Add(history);

    public PaymentRecord? PaymentOf(Guid expenseId) => _storedPayments.GetValueOrDefault(expenseId);

    public void AddPayment(PaymentRecord payment) => _pendingPayments.Add(payment);

    public Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ExpenseHistory>>(_storedHistory
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ToList());

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            _tracked.Clear();
            _pendingHistory.Clear();
            _pendingPayments.Clear();
            return Task.FromResult(false);
        }

        foreach (PaymentRecord payment in _pendingPayments)
        {
            _storedPayments.Add(payment.ExpenseId, payment);
        }

        foreach (Expense expense in _tracked.Values)
        {
            _stored[expense.Id] = Clone(expense);
        }

        _storedHistory.AddRange(_pendingHistory);
        _tracked.Clear();
        _pendingHistory.Clear();
        _pendingPayments.Clear();
        SaveCount++;
        return Task.FromResult(true);
    }

    private Expense Clone(Expense source) =>
        new()
        {
            Id = source.Id,
            OwnerId = source.OwnerId,
            CategoryId = source.CategoryId,
            Description = source.Description,
            Amount = source.Amount,
            ExpenseDate = source.ExpenseDate,
            Status = source.Status,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
            DecidedById = source.DecidedById,
            DecidedAtUtc = source.DecidedAtUtc,
            RejectionReason = source.RejectionReason,
            ConcurrencyStamp = source.ConcurrencyStamp,
            Payment = _storedPayments.GetValueOrDefault(source.Id),
        };
}
