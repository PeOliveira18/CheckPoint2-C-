using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core do ExpenseHub.
/// </summary>
internal sealed class ExpenseHubDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseHubDbContext"/> class.
    /// </summary>
    /// <param name="options">Opções do contexto.</param>
    public ExpenseHubDbContext(DbContextOptions<ExpenseHubDbContext> options)
        : base(options)
    {
    }

    /// <summary>Reembolsos.</summary>
    public DbSet<Expense> Expenses => Set<Expense>();

    /// <summary>Categorias de despesa.</summary>
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    /// <summary>Histórico dos reembolsos.</summary>
    public DbSet<ExpenseHistory> ExpenseHistory => Set<ExpenseHistory>();

    /// <summary>Pagamentos registrados.</summary>
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseHubDbContext).Assembly);
    }
}
