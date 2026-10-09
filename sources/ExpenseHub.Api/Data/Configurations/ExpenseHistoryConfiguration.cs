using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento de <see cref="ExpenseHistory"/>.
/// </summary>
internal sealed class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    /// <inheritdoc />
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

        builder.HasIndex(history => new { history.ExpenseId, history.OccurredAtUtc });
    }
}
