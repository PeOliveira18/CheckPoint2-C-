using System;
using System.Linq;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Authorization;

/// <summary>
/// I06 — escopo de leitura por role (matriz de autorização).
/// </summary>
[TestClass]
public sealed class ExpenseAccessPolicyTests
{
    /// <summary>
    /// Matriz de visibilidade: cada linha descreve roles do leitor, se ele é o proprietário, o estado e o resultado esperado.
    /// Também garante que o filtro traduzido para SQL e a verificação em memória concordam.
    /// </summary>
    /// <param name="roles">Roles do leitor, separadas por vírgula (vazio = sem role).</param>
    /// <param name="isOwner">Se o leitor é o proprietário.</param>
    /// <param name="status">Estado do reembolso.</param>
    /// <param name="expected">Se o reembolso deve ser visível.</param>
    [TestMethod]
    [DataRow("Employee", true, "Draft", true)]
    [DataRow("Employee", true, "Paid", true)]
    [DataRow("Employee", false, "Draft", false)]
    [DataRow("Employee", false, "Submitted", false)]
    [DataRow("Employee", false, "Approved", false)]
    [DataRow("Approver", false, "Submitted", true)]
    [DataRow("Approver", false, "Draft", false)]
    [DataRow("Approver", false, "Approved", false)]
    [DataRow("Approver", true, "Draft", false)]
    [DataRow("Finance", false, "Approved", true)]
    [DataRow("Finance", false, "Paid", true)]
    [DataRow("Finance", false, "Submitted", false)]
    [DataRow("Finance", false, "Rejected", false)]
    [DataRow("Auditor", false, "Draft", true)]
    [DataRow("Auditor", false, "Rejected", true)]
    [DataRow("Admin", false, "Submitted", false)]
    [DataRow("Admin", true, "Draft", false)]
    [DataRow("", true, "Draft", false)]
    [DataRow("Employee,Approver", true, "Draft", true)]
    [DataRow("Employee,Approver", false, "Submitted", true)]
    [DataRow("Employee,Approver", false, "Draft", false)]
    [DataRow("Approver,Finance", false, "Rejected", false)]
    [DataRow("Admin,Employee", false, "Draft", false)]
    public void VisibilityFollowsRoleMatrix(string roles, bool isOwner, string status, bool expected)
    {
        UserContext reader = TestData.User(
            "leitor",
            roles.Split(',', StringSplitOptions.RemoveEmptyEntries));
        Expense expense = TestData.Expense(isOwner ? "leitor" : "outro", Enum.Parse<ExpenseStatus>(status));

        bool inMemory = ExpenseAccessPolicy.CanView(expense, reader);
        bool translated = new[] { expense }.AsQueryable().Any(ExpenseAccessPolicy.VisibleTo(reader));

        Assert.AreEqual(expected, inMemory, "CanView");
        Assert.AreEqual(expected, translated, "VisibleTo");
    }
}
