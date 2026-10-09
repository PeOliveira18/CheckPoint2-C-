using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Cria o contexto para as ferramentas do EF Core (migrations) sem iniciar a aplicação.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ExpenseHubDbContext>
{
    /// <summary>
    /// Connection string fictícia usada apenas para gerar migrations sem acesso ao banco.
    /// </summary>
    private const string OfflineConnectionString = "Data Source=design-time-only";

    /// <inheritdoc />
    public ExpenseHubDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        string? connectionString = configuration.GetConnectionString(DatabaseSetup.ConnectionStringName);
        DbContextOptionsBuilder<ExpenseHubDbContext> optionsBuilder = new();
        optionsBuilder.UseOracle(string.IsNullOrWhiteSpace(connectionString) ? OfflineConnectionString : connectionString);

        return new ExpenseHubDbContext(optionsBuilder.Options);
    }
}
