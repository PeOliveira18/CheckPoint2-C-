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
    private static readonly DateOnly _today = new(2026, 10, 9);

    private static readonly string[] _allExpenseFields = ["description", "amount", "expenseDate"];

    /// <summary>Entrada válida não gera erros.</summary>
    [TestMethod]
    public void ValidExpenseHasNoErrors()
    {
        Assert.IsEmpty(ExpenseRules.ValidateExpense("Estacionamento no cliente", 25m, _today, _today));
    }

    /// <summary>Todos os campos inválidos são reportados juntos.</summary>
    [TestMethod]
    public void InvalidExpenseReportsEveryField()
    {
        Dictionary<string, string[]> errors = ExpenseRules.ValidateExpense(null, 0m, _today.AddDays(1), _today);

        CollectionAssert.AreEquivalent(_allExpenseFields, new List<string>(errors.Keys));
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

        Assert.AreEqual(valid, ExpenseRules.ValidateExpense(text, 1m, _today, _today).Count == 0);
        Assert.AreEqual(valid, ExpenseRules.ValidateJustification(text).Count == 0);
    }

    /// <summary>Os limites de valor são inclusivos (R$ 0,01 e Int32.MaxValue).</summary>
    [TestMethod]
    public void AmountLimitsAreInclusive()
    {
        Assert.IsFalse(ExpenseRules.ValidateExpense("Descrição válida", 0.01m, _today, _today).ContainsKey("amount"));
        Assert.IsFalse(ExpenseRules.ValidateExpense("Descrição válida", int.MaxValue, _today, _today).ContainsKey("amount"));
        Assert.IsTrue(ExpenseRules.ValidateExpense("Descrição válida", int.MaxValue + 0.01m, _today, _today).ContainsKey("amount"));
    }

    /// <summary>Justificativa composta apenas de espaços é inválida.</summary>
    [TestMethod]
    public void WhitespaceJustificationIsInvalid()
    {
        Assert.IsTrue(ExpenseRules.ValidateJustification(new string(' ', 20)).ContainsKey("justification"));
    }
}
