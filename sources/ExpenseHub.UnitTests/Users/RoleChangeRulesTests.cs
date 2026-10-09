using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Users;

/// <summary>
/// Regras de alteração de roles pelo Admin.
/// </summary>
[TestClass]
public sealed class RoleChangeRulesTests
{
    private const string AdminId = "admin-1";
    private const string OtherUserId = "user-2";

    /// <summary>Roles conhecidas são aceitas e normalizadas para o nome canônico, sem duplicatas.</summary>
    [TestMethod]
    public void NormalizeKnownRolesReturnsCanonicalDistinctNames()
    {
        ServiceResult<IReadOnlyList<string>> result =
            RoleChangeRules.Normalize(["employee", " APPROVER ", "Employee"], OtherUserId, AdminId);

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new List<string> { RoleNames.Employee, RoleNames.Approver }, result.Value.ToList());
    }

    /// <summary>Role desconhecida é rejeitada como entrada inválida e nunca criada implicitamente.</summary>
    [TestMethod]
    public void NormalizeUnknownRoleReturnsValidationError()
    {
        ServiceResult<IReadOnlyList<string>> result =
            RoleChangeRules.Normalize([RoleNames.Employee, "SuperUser"], OtherUserId, AdminId);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsNotNull(result.ValidationErrors);
        StringAssert.Contains(result.ValidationErrors["roles"][0], "SuperUser");
    }

    /// <summary>O Admin não pode remover a própria role Admin.</summary>
    [TestMethod]
    public void NormalizeAdminRemovingOwnAdminRoleReturnsConflict()
    {
        ServiceResult<IReadOnlyList<string>> result =
            RoleChangeRules.Normalize([RoleNames.Employee], AdminId, AdminId);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>O Admin pode alterar as próprias roles desde que mantenha Admin.</summary>
    [TestMethod]
    public void NormalizeAdminKeepingOwnAdminRoleSucceeds()
    {
        ServiceResult<IReadOnlyList<string>> result =
            RoleChangeRules.Normalize([RoleNames.Admin, RoleNames.Auditor], AdminId, AdminId);

        Assert.IsTrue(result.Succeeded);
        Assert.HasCount(2, result.Value);
    }

    /// <summary>O Admin pode remover a role Admin de outro usuário.</summary>
    [TestMethod]
    public void NormalizeRemovingAdminFromAnotherUserSucceeds()
    {
        ServiceResult<IReadOnlyList<string>> result =
            RoleChangeRules.Normalize([], OtherUserId, AdminId);

        Assert.IsTrue(result.Succeeded);
        Assert.IsEmpty(result.Value);
    }
}
