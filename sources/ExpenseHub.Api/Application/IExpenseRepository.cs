using System;
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
    /// Persiste todas as alterações pendentes em uma única operação atômica.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="false"/> quando outra operação alterou o reembolso antes (conflito de concorrência).</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
