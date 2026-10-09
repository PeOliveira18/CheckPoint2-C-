using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento de <see cref="PaymentRecord"/>.
/// </summary>
internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("EH_PAYMENT_RECORDS");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.PaidById).HasMaxLength(450).IsRequired();
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.HasIndex(payment => payment.ExpenseId).IsUnique();
    }
}
