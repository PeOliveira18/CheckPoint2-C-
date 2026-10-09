using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Roles conhecidas pela aplicação.
/// </summary>
internal static class RoleNames
{
    /// <summary>Administra usuários e roles.</summary>
    public const string Admin = "Admin";

    /// <summary>Cria, edita, envia e consulta os próprios reembolsos.</summary>
    public const string Employee = "Employee";

    /// <summary>Aprova ou reprova reembolsos enviados de outras pessoas.</summary>
    public const string Approver = "Approver";

    /// <summary>Registra pagamentos de reembolsos aprovados de outras pessoas.</summary>
    public const string Finance = "Finance";

    /// <summary>Consulta todos os reembolsos e históricos, sem escrita.</summary>
    public const string Auditor = "Auditor";

    /// <summary>Todas as roles que devem existir para a aplicação funcionar.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Employee, Approver, Finance, Auditor];
}
