using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Regras de validação de campos do reembolso.
/// </summary>
[TestClass]
public sealed class ExpenseRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static readonly string[] AllExpenseFields = ["description", "amount", "expenseDate"];

    /// <summary>Entrada válida não gera erros.</summary>
    [TestMethod]
    public void ValidExpenseHasNoErrors()
    {
        Assert.IsEmpty(ExpenseRules.ValidateExpense("Estacionamento no cliente", 25m, Today, Today));
    }

    /// <summary>Todos os campos inválidos são reportados juntos.</summary>
    [TestMethod]
    public void InvalidExpenseReportsEveryField()
    {
        Dictionary<string, string[]> errors = ExpenseRules.ValidateExpense(null, 0m, Today.AddDays(1), Today);

        CollectionAssert.AreEquivalent(AllExpenseFields, new List<string>(errors.Keys));
    }

    /// <summary>Os limites de tamanho de texto são inclusivos (10 e 500).</summary>
    /// <param name="length">Tamanho do texto.</param>
    /// <param name="valid">Se deve ser aceito.</param>
    [TestMethod]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void TextLengthLimitsAreInclusive(int length, bool valid)
    {
        string text = new('x', length);

        Assert.AreEqual(valid, ExpenseRules.ValidateExpense(text, 1m, Today, Today).Count == 0);
        Assert.AreEqual(valid, ExpenseRules.ValidateJustification(text).Count == 0);
    }

    /// <summary>Os limites de valor são inclusivos (R$ 0,01 e Int32.MaxValue).</summary>
    [TestMethod]
    public void AmountLimitsAreInclusive()
    {
        Assert.IsFalse(ExpenseRules.ValidateExpense("Descrição válida", 0.01m, Today, Today).ContainsKey("amount"));
        Assert.IsFalse(ExpenseRules.ValidateExpense("Descrição válida", int.MaxValue, Today, Today).ContainsKey("amount"));
        Assert.IsTrue(ExpenseRules.ValidateExpense("Descrição válida", int.MaxValue + 0.01m, Today, Today).ContainsKey("amount"));
    }

    /// <summary>Justificativa composta apenas de espaços é inválida.</summary>
    [TestMethod]
    public void WhitespaceJustificationIsInvalid()
    {
        Assert.IsTrue(ExpenseRules.ValidateJustification(new string(' ', 20)).ContainsKey("justification"));
    }
}
