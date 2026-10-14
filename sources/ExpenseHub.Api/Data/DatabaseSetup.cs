using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

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

        services.AddDbContext<ExpenseHubDbContext>(options => options.UseOracle(connectionString));
        return services;
    }
}
