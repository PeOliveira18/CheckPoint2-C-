using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Regras do fluxo de reembolsos: combina role, ownership e estado em cada operação.
/// </summary>
internal sealed class ExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseService"/> class.
    /// </summary>
    /// <param name="repository">Repositório de reembolsos.</param>
    /// <param name="timeProvider">Relógio do servidor.</param>
    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria um rascunho cujo proprietário é o usuário autenticado.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="input">Campos do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso criado em <see cref="ExpenseStatus.Draft"/>.</returns>
    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(UserContext user, ExpenseInput input, CancellationToken cancellationToken)
    {
        if (!user.IsInRole(RoleNames.Employee))
        {
            return Forbidden("Somente Employee cria reembolsos.");
        }

        Dictionary<string, string[]> errors = await ValidateInputAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<ExpenseResponse>.Invalid(errors);
        }

        DateTime now = UtcNow();
        Expense expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = user.UserId,
            CategoryId = input.CategoryId,
            Description = input.Description.Trim(),
            Amount = input.Amount,
            ExpenseDate = input.ExpenseDate,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid(),
        };

        _repository.Add(expense);
        _repository.AddHistory(NewHistory(expense, ExpenseAction.Created, user, null, now));
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Edita um rascunho próprio e registra as alterações no histórico.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="input">Novos valores.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso atualizado ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(UserContext user, Guid id, ExpenseInput input, CancellationToken cancellationToken)
    {
        if (!user.IsInRole(RoleNames.Employee))
        {
            return Forbidden("Somente Employee edita reembolsos.");
        }

        Dictionary<string, string[]> errors = await ValidateInputAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return ServiceResult<ExpenseResponse>.Invalid(errors);
        }

        Expense? expense = await _repository.FindAsync(id, cancellationToken);
        if (IsOwnedTransitionDenied(expense, user, ExpenseAction.Updated, out ServiceResult<ExpenseResponse>? denied))
        {
            return denied;
        }

        string changes = DescribeChanges(expense, input);
        if (changes.Length == 0)
        {
            return ServiceResult<ExpenseResponse>.Success(ExpenseResponse.From(expense));
        }

        DateTime now = UtcNow();
        expense.Description = input.Description.Trim();
        expense.Amount = input.Amount;
        expense.ExpenseDate = input.ExpenseDate;
        expense.CategoryId = input.CategoryId;
        expense.UpdatedAtUtc = now;
        expense.ConcurrencyStamp = Guid.NewGuid();

        ExpenseHistory history = NewHistory(expense, ExpenseAction.Updated, user, ExpenseStatus.Draft, now);
        history.Changes = changes;
        _repository.AddHistory(history);
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Envia um rascunho próprio para aprovação (<see cref="ExpenseStatus.Draft"/> → <see cref="ExpenseStatus.Submitted"/>).
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso enviado ou a falha correspondente.</returns>
    public async Task<ServiceResult<ExpenseResponse>> SubmitAsync(UserContext user, Guid id, CancellationToken cancellationToken)
    {
        if (!user.IsInRole(RoleNames.Employee))
        {
            return Forbidden("Somente Employee envia reembolsos.");
        }

        Expense? expense = await _repository.FindAsync(id, cancellationToken);
        if (IsOwnedTransitionDenied(expense, user, ExpenseAction.Submitted, out ServiceResult<ExpenseResponse>? denied))
        {
            return denied;
        }

        return await TransitionAsync(expense, user, ExpenseAction.Submitted, cancellationToken);
    }

    /// <summary>
    /// Lista os reembolsos no escopo de leitura do usuário; o filtro é aplicado no banco.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolsos visíveis.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(UserContext user, CancellationToken cancellationToken)
    {
        if (!HasFunctionalRole(user))
        {
            return ServiceResult<IReadOnlyList<ExpenseResponse>>.Failure(
                ServiceError.Forbidden,
                "O usuário não possui role com acesso a reembolsos.");
        }

        IReadOnlyList<Expense> expenses = await _repository.ListAsync(ExpenseAccessPolicy.VisibleTo(user), cancellationToken);
        List<ExpenseResponse> response = expenses.Select(ExpenseResponse.From).ToList();
        return ServiceResult<IReadOnlyList<ExpenseResponse>>.Success(response);
    }

    /// <summary>
    /// Consulta um reembolso; fora do escopo de leitura é tratado como inexistente.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso ou 404.</returns>
    public async Task<ServiceResult<ExpenseResponse>> GetAsync(UserContext user, Guid id, CancellationToken cancellationToken)
    {
        if (!HasFunctionalRole(user))
        {
            return Forbidden("O usuário não possui role com acesso a reembolsos.");
        }

        Expense? expense = await _repository.FindVisibleAsync(id, ExpenseAccessPolicy.VisibleTo(user), cancellationToken);
        return expense is null ? NotFound() : ServiceResult<ExpenseResponse>.Success(ExpenseResponse.From(expense));
    }

    private static bool HasFunctionalRole(UserContext user) =>
        user.IsInRole(RoleNames.Employee) ||
        user.IsInRole(RoleNames.Approver) ||
        user.IsInRole(RoleNames.Finance) ||
        user.IsInRole(RoleNames.Auditor);

    private static ServiceResult<ExpenseResponse> Forbidden(string message) =>
        ServiceResult<ExpenseResponse>.Failure(ServiceError.Forbidden, message);

    private static ServiceResult<ExpenseResponse> NotFound() =>
        ServiceResult<ExpenseResponse>.Failure(ServiceError.NotFound, "Reembolso não encontrado.");

    private static ServiceResult<ExpenseResponse> Conflict(string message) =>
        ServiceResult<ExpenseResponse>.Failure(ServiceError.Conflict, message);

    /// <summary>
    /// Regras das ações do proprietário (editar e enviar): fora do escopo de leitura → 404,
    /// visível mas de outra pessoa → 403, estado incompatível → 409.
    /// </summary>
    private static bool IsOwnedTransitionDenied(
        [NotNullWhen(false)] Expense? expense,
        UserContext user,
        ExpenseAction action,
        [NotNullWhen(true)] out ServiceResult<ExpenseResponse>? denied)
    {
        if (expense is null || !ExpenseAccessPolicy.CanView(expense, user))
        {
            denied = NotFound();
        }
        else if (!string.Equals(expense.OwnerId, user.UserId, StringComparison.Ordinal))
        {
            denied = Forbidden("Somente o proprietário pode alterar este reembolso.");
        }
        else if (ExpenseStateMachine.Next(expense.Status, action) is null)
        {
            denied = Conflict($"Reembolso em {expense.Status} não aceita a ação {action}.");
        }
        else
        {
            denied = null;
        }

        return denied is not null;
    }

    private static ExpenseHistory NewHistory(Expense expense, ExpenseAction action, UserContext user, ExpenseStatus? from, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExpenseId = expense.Id,
            Action = action,
            ActorId = user.UserId,
            OccurredAtUtc = now,
            FromStatus = from,
            ToStatus = expense.Status,
        };

    private static string DescribeChanges(Expense expense, ExpenseInput input)
    {
        List<string> changes = [];
        string description = input.Description.Trim();
        if (!string.Equals(expense.Description, description, StringComparison.Ordinal))
        {
            changes.Add($"description: \"{expense.Description}\" -> \"{description}\"");
        }

        if (expense.Amount != input.Amount)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"amount: {expense.Amount:0.00} -> {input.Amount:0.00}"));
        }

        if (expense.ExpenseDate != input.ExpenseDate)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"expenseDate: {expense.ExpenseDate:yyyy-MM-dd} -> {input.ExpenseDate:yyyy-MM-dd}"));
        }

        if (expense.CategoryId != input.CategoryId)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"categoryId: {expense.CategoryId} -> {input.CategoryId}"));
        }

        string text = string.Join("; ", changes);
        return text.Length > 2000 ? text[..2000] : text;
    }

    /// <summary>
    /// Aplica a transição de estado e grava a alteração e o histórico juntos.
    /// </summary>
    private async Task<ServiceResult<ExpenseResponse>> TransitionAsync(
        Expense expense,
        UserContext user,
        ExpenseAction action,
        CancellationToken cancellationToken,
        string? justification = null)
    {
        ExpenseStatus from = expense.Status;
        ExpenseStatus? to = ExpenseStateMachine.Next(from, action);
        if (to is null)
        {
            return Conflict($"Reembolso em {from} não aceita a ação {action}.");
        }

        DateTime now = UtcNow();
        expense.Status = to.Value;
        expense.UpdatedAtUtc = now;
        expense.ConcurrencyStamp = Guid.NewGuid();

        ExpenseHistory history = NewHistory(expense, action, user, from, now);
        history.Justification = justification;
        _repository.AddHistory(history);
        return await SaveAsync(expense, cancellationToken);
    }

    private async Task<Dictionary<string, string[]>> ValidateInputAsync(ExpenseInput input, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(UtcNow());
        Dictionary<string, string[]> errors = ExpenseRules.ValidateExpense(input.Description, input.Amount, input.ExpenseDate, today);
        if (!await _repository.CategoryExistsAsync(input.CategoryId, cancellationToken))
        {
            errors["categoryId"] = ["Categoria inexistente."];
        }

        return errors;
    }

    private async Task<ServiceResult<ExpenseResponse>> SaveAsync(Expense expense, CancellationToken cancellationToken)
    {
        if (!await _repository.SaveChangesAsync(cancellationToken))
        {
            return Conflict("O reembolso foi alterado por outra operação. Consulte o estado atual e tente novamente.");
        }

        return ServiceResult<ExpenseResponse>.Success(ExpenseResponse.From(expense));
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
