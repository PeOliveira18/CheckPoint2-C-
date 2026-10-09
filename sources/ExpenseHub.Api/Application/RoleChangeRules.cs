using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Regras puras para a alteração de roles feita pelo Admin.
/// </summary>
internal static class RoleChangeRules
{
    /// <summary>
    /// Valida as roles solicitadas e devolve o conjunto normalizado (nomes canônicos, sem duplicatas).
    /// </summary>
    /// <param name="requestedRoles">Roles enviadas pelo Admin.</param>
    /// <param name="targetUserId">Usuário que terá as roles alteradas.</param>
    /// <param name="actingUserId">Admin autenticado que executa a operação.</param>
    /// <returns>Conjunto normalizado ou a falha correspondente.</returns>
    public static ServiceResult<IReadOnlyList<string>> Normalize(
        IEnumerable<string> requestedRoles,
        string targetUserId,
        string actingUserId)
    {
        List<string> normalized = [];
        List<string> unknown = [];
        foreach (string requested in requestedRoles)
        {
            string? canonical = RoleNames.All.FirstOrDefault(
                role => string.Equals(role, requested?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (canonical is null)
            {
                unknown.Add(requested ?? string.Empty);
            }
            else if (!normalized.Contains(canonical))
            {
                normalized.Add(canonical);
            }
        }

        if (unknown.Count > 0)
        {
            return ServiceResult<IReadOnlyList<string>>.Invalid(new Dictionary<string, string[]>
            {
                ["roles"] = [$"Roles desconhecidas: {string.Join(", ", unknown)}. Permitidas: {string.Join(", ", RoleNames.All)}."],
            });
        }

        if (string.Equals(targetUserId, actingUserId, StringComparison.Ordinal) && !normalized.Contains(RoleNames.Admin))
        {
            return ServiceResult<IReadOnlyList<string>>.Failure(
                ServiceError.Conflict,
                "O Admin não pode remover a própria role Admin.");
        }

        return ServiceResult<IReadOnlyList<string>>.Success(normalized);
    }
}
