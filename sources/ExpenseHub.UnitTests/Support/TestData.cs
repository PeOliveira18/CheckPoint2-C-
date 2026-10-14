using System;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Support;

internal static class TestData
{
    public const string AnaId = "ana";

    public const string BrunoId = "bruno";

    public const string CarlaId = "carla";

    public const string DaniId = "dani";

    public const string EduId = "edu";

    public const string FabioId = "fabio";

    public static UserContext Ana { get; } = new(AnaId, [RoleNames.Employee]);

    public static UserContext Bruno { get; } = new(BrunoId, [RoleNames.Employee]);

    public static UserContext Carla { get; } = new(CarlaId, [RoleNames.Approver]);

    public static UserContext Dani { get; } = new(DaniId, [RoleNames.Finance]);

    public static UserContext Edu { get; } = new(EduId, [RoleNames.Auditor]);

    public static UserContext Fabio { get; } = new(FabioId, [RoleNames.Admin]);

    public static UserContext NoRole { get; } = new("sem-role", []);

    public static UserContext User(string id, params string[] roles) => new(id, roles);

    public static ExpenseInput ValidInput(DateOnly today) =>
        new("Táxi do aeroporto até o cliente", 87.50m, today.AddDays(-1), 2);

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
