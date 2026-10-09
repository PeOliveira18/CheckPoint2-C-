using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Registro do banco relacional (Oracle) na injeção de dependência.
/// </summary>
internal static class DatabaseSetup
{
    /// <summary>Nome da connection string em <c>ConnectionStrings</c>.</summary>
    public const string ConnectionStringName = "ExpenseHub";

    /// <summary>
    /// Registra o <see cref="ExpenseHubDbContext"/> usando o provider Oracle.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddExpenseHubDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada. Configure-a com dotnet user-secrets (veja o README).");
        }

        services.AddDbContext<ExpenseHubDbContext>(options => options.UseOracle(connectionString));
        return services;
    }
}
