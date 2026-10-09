namespace ExpenseHub.Api.Domain;

/// <summary>
/// Transições permitidas do fluxo de reembolso.
/// </summary>
internal static class ExpenseStateMachine
{
    /// <summary>
    /// Devolve o próximo estado para a ação a partir do estado atual, ou nulo se a transição não for permitida.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="action">Ação solicitada.</param>
    /// <returns>Próximo estado ou nulo.</returns>
    public static ExpenseStatus? Next(ExpenseStatus current, ExpenseAction action) => (current, action) switch
    {
        (ExpenseStatus.Draft, ExpenseAction.Updated) => ExpenseStatus.Draft,
        (ExpenseStatus.Draft, ExpenseAction.Submitted) => ExpenseStatus.Submitted,
        (ExpenseStatus.Submitted, ExpenseAction.Approved) => ExpenseStatus.Approved,
        (ExpenseStatus.Submitted, ExpenseAction.Rejected) => ExpenseStatus.Rejected,
        (ExpenseStatus.Approved, ExpenseAction.Paid) => ExpenseStatus.Paid,
        _ => null,
    };

    /// <summary>
    /// Indica se o estado é final (sem transições posteriores).
    /// </summary>
    /// <param name="status">Estado avaliado.</param>
    /// <returns><see langword="true"/> para <see cref="ExpenseStatus.Rejected"/> e <see cref="ExpenseStatus.Paid"/>.</returns>
    public static bool IsFinal(ExpenseStatus status) => status is ExpenseStatus.Rejected or ExpenseStatus.Paid;
}
