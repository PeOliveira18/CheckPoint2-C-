using ExpenseHub.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Core.Data.Configurations;

internal sealed class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    public void Configure(EntityTypeBuilder<ExpenseHistory> builder)
    {
        builder.ToTable("EH_EXPENSE_HISTORY");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.ActorId).HasMaxLength(450).IsRequired();
        builder.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.Justification).HasMaxLength(500);
        builder.Property(history => history.Changes).HasMaxLength(2000);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(history => history.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.ExpenseId, history.OccurredAtUtc });
    }
}
