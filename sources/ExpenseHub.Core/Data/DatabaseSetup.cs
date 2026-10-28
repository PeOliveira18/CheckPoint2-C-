using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Core.Data;

internal static class DatabaseSetup
{
    public const string ConnectionStringName = "ExpenseHub";

    public static IServiceCollection AddExpenseHubDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada. Configure-a com dotnet user-secrets (veja o README).");
        }

        services.AddDbContext<ExpenseHubDbContext>(options => UseOracle(options, connectionString));
        return services;
    }

    public static DbContextOptionsBuilder UseOracle(DbContextOptionsBuilder options, string connectionString) =>
        options.UseOracle(
            connectionString,
            oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
}
