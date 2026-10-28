using ExpenseHub.Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Core.Data;

internal sealed class ExpenseHubDbContext : IdentityDbContext<ApplicationUser>
{
    public ExpenseHubDbContext(DbContextOptions<ExpenseHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public DbSet<ExpenseHistory> ExpenseHistory => Set<ExpenseHistory>();

    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    public DbSet<ReportRequest> ReportRequests => Set<ReportRequest>();

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
