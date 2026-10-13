using System;
using System.Linq;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Tabela de transições do fluxo de reembolso.
/// </summary>
[TestClass]
public sealed class ExpenseStateMachineTests
{
    /// <summary>As transições do contrato levam ao próximo estado esperado.</summary>
    /// <param name="from">Estado atual.</param>
    /// <param name="action">Ação.</param>
    /// <param name="to">Próximo estado esperado.</param>
    [TestMethod]
    [DataRow("Draft", "Updated", "Draft")]
    [DataRow("Draft", "Submitted", "Submitted")]
    [DataRow("Submitted", "Approved", "Approved")]
    [DataRow("Submitted", "Rejected", "Rejected")]
    [DataRow("Approved", "Paid", "Paid")]
    public void AllowedTransitionsReachExpectedState(string from, string action, string to)
    {
        ExpenseStatus? next = ExpenseStateMachine.Next(Enum.Parse<ExpenseStatus>(from), Enum.Parse<ExpenseAction>(action));

        Assert.AreEqual(Enum.Parse<ExpenseStatus>(to), next);
    }

    /// <summary>Qualquer combinação fora do contrato é negada (sem reabertura, reenvio ou atalhos).</summary>
    [TestMethod]
    public void EveryOtherCombinationIsDenied()
    {
        (ExpenseStatus From, ExpenseAction Action)[] allowed =
        [
            (ExpenseStatus.Draft, ExpenseAction.Updated),
            (ExpenseStatus.Draft, ExpenseAction.Submitted),
            (ExpenseStatus.Submitted, ExpenseAction.Approved),
            (ExpenseStatus.Submitted, ExpenseAction.Rejected),
            (ExpenseStatus.Approved, ExpenseAction.Paid),
        ];

        foreach (ExpenseStatus from in Enum.GetValues<ExpenseStatus>())
        {
            foreach (ExpenseAction action in Enum.GetValues<ExpenseAction>().Where(action => !allowed.Contains((from, action))))
            {
                Assert.IsNull(ExpenseStateMachine.Next(from, action), $"{from} + {action} deveria ser negado.");
            }
        }
    }

    /// <summary>Somente Rejected e Paid são finais e não aceitam nenhuma ação.</summary>
    [TestMethod]
    public void RejectedAndPaidAreFinal()
    {
        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            bool expectedFinal = status is ExpenseStatus.Rejected or ExpenseStatus.Paid;
            Assert.AreEqual(expectedFinal, ExpenseStateMachine.IsFinal(status), status.ToString());
            if (expectedFinal)
            {
                Assert.IsTrue(Enum.GetValues<ExpenseAction>().All(action => ExpenseStateMachine.Next(status, action) is null));
            }
        }
    }
}
