using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core do ExpenseHub, incluindo as tabelas do Identity.
/// </summary>
internal sealed class ExpenseHubDbContext : IdentityDbContext<ApplicationUser>
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

        modelBuilder.Entity<ApplicationUser>().ToTable("EH_USERS");
        modelBuilder.Entity<IdentityRole>().ToTable("EH_ROLES");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("EH_USER_ROLES");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("EH_USER_CLAIMS");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("EH_USER_LOGINS");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("EH_USER_TOKENS");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("EH_ROLE_CLAIMS");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseHubDbContext).Assembly);
    }
}
