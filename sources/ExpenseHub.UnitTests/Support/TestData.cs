using System;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Support;

/// <summary>
/// Usuários e reembolsos padrão dos cenários de teste.
/// </summary>
internal static class TestData
{
    /// <summary>Employee proprietário dos reembolsos de teste.</summary>
    public const string AnaId = "ana";

    /// <summary>Outro Employee.</summary>
    public const string BrunoId = "bruno";

    /// <summary>Approver.</summary>
    public const string CarlaId = "carla";

    /// <summary>Finance.</summary>
    public const string DaniId = "dani";

    /// <summary>Auditor.</summary>
    public const string EduId = "edu";

    /// <summary>Admin.</summary>
    public const string FabioId = "fabio";

    /// <summary>Ana (Employee).</summary>
    public static UserContext Ana { get; } = new(AnaId, [RoleNames.Employee]);

    /// <summary>Bruno (Employee).</summary>
    public static UserContext Bruno { get; } = new(BrunoId, [RoleNames.Employee]);

    /// <summary>Carla (Approver).</summary>
    public static UserContext Carla { get; } = new(CarlaId, [RoleNames.Approver]);

    /// <summary>Dani (Finance).</summary>
    public static UserContext Dani { get; } = new(DaniId, [RoleNames.Finance]);

    /// <summary>Edu (Auditor).</summary>
    public static UserContext Edu { get; } = new(EduId, [RoleNames.Auditor]);

    /// <summary>Fabio (Admin, sem roles funcionais).</summary>
    public static UserContext Fabio { get; } = new(FabioId, [RoleNames.Admin]);

    /// <summary>Usuário autenticado sem nenhuma role.</summary>
    public static UserContext NoRole { get; } = new("sem-role", []);

    /// <summary>
    /// Cria um usuário com as roles informadas.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="roles">Roles.</param>
    /// <returns>Usuário.</returns>
    public static UserContext User(string id, params string[] roles) => new(id, roles);

    /// <summary>
    /// Entrada válida para criação/edição.
    /// </summary>
    /// <param name="today">Data atual.</param>
    /// <returns>Entrada válida.</returns>
    public static ExpenseInput ValidInput(DateOnly today) =>
        new("Táxi do aeroporto até o cliente", 87.50m, today.AddDays(-1), 2);

    /// <summary>
    /// Cria um reembolso persistido no estado informado.
    /// </summary>
    /// <param name="ownerId">Proprietário.</param>
    /// <param name="status">Estado.</param>
    /// <returns>Reembolso.</returns>
    public static Expense Expense(string ownerId, ExpenseStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CategoryId = 1,
            Description = "Almoço com cliente na visita técnica",
            Amount = 120.00m,
            ExpenseDate = new DateOnly(2026, 10, 1),
            Status = status,
            CreatedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            ConcurrencyStamp = Guid.NewGuid(),
        };
}
