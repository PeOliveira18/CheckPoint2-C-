using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Acesso a dados dos reembolsos usado pelo <see cref="ExpenseService"/>.
/// </summary>
internal interface IExpenseRepository
{
    /// <summary>
    /// Indica se a categoria existe.
    /// </summary>
    /// <param name="categoryId">Categoria procurada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> se existir.</returns>
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca um reembolso pelo identificador, sem filtro de visibilidade, para alteração.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso ou nulo.</returns>
    Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Busca um reembolso aplicando o filtro de visibilidade no banco.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="visibility">Escopo de leitura do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolso visível ou nulo.</returns>
    Task<Expense?> FindVisibleAsync(Guid id, Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken);

    /// <summary>
    /// Lista reembolsos aplicando o filtro de visibilidade no banco, do mais recente para o mais antigo.
    /// </summary>
    /// <param name="visibility">Escopo de leitura do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Reembolsos visíveis.</returns>
    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> visibility, CancellationToken cancellationToken);

    /// <summary>
    /// Adiciona um novo reembolso.
    /// </summary>
    /// <param name="expense">Reembolso criado.</param>
    void Add(Expense expense);

    /// <summary>
    /// Adiciona uma entrada de histórico.
    /// </summary>
    /// <param name="history">Entrada de histórico.</param>
    void AddHistory(ExpenseHistory history);

    /// <summary>
    /// Adiciona o registro de pagamento.
    /// </summary>
    /// <param name="payment">Pagamento.</param>
    void AddPayment(PaymentRecord payment);

    /// <summary>
    /// Lista o histórico de um reembolso em ordem cronológica.
    /// </summary>
    /// <param name="expenseId">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Entradas do histórico.</returns>
    Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste todas as alterações pendentes em uma única operação atômica.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="false"/> quando outra operação alterou o reembolso antes (conflito de concorrência).</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
