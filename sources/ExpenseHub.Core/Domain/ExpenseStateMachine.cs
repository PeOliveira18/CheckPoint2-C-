namespace ExpenseHub.Core.Domain;

internal static class ExpenseStateMachine
{
    public static ExpenseStatus? Next(ExpenseStatus current, ExpenseAction action) => (current, action) switch
    {
        (ExpenseStatus.Draft, ExpenseAction.Updated) => ExpenseStatus.Draft,
        (ExpenseStatus.Draft, ExpenseAction.Submitted) => ExpenseStatus.Submitted,
        (ExpenseStatus.Submitted, ExpenseAction.Approved) => ExpenseStatus.Approved,
        (ExpenseStatus.Submitted, ExpenseAction.Rejected) => ExpenseStatus.Rejected,
        (ExpenseStatus.Approved, ExpenseAction.Paid) => ExpenseStatus.Paid,
        _ => null,
    };

    public static bool IsFinal(ExpenseStatus status) => status is ExpenseStatus.Rejected or ExpenseStatus.Paid;
}
