using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento de <see cref="Expense"/>.
/// </summary>
internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("EH_EXPENSES");
        builder.HasKey(expense => expense.Id);
        builder.Property(expense => expense.Id).ValueGeneratedNever();

        builder.Property(expense => expense.OwnerId).HasMaxLength(450).IsRequired();
        builder.Property(expense => expense.Description).HasMaxLength(500).IsRequired();
        builder.Property(expense => expense.Amount).HasPrecision(18, 2);
        builder.Property(expense => expense.ExpenseDate)
            .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
            .HasColumnType("DATE");
        builder.Property(expense => expense.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(expense => expense.DecidedById).HasMaxLength(450);
        builder.Property(expense => expense.RejectionReason).HasMaxLength(500);
        builder.Property(expense => expense.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(expense => expense.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(expense => expense.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(expense => expense.Category)
            .WithMany()
            .HasForeignKey(expense => expense.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(expense => expense.History)
            .WithOne()
            .HasForeignKey(history => history.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(expense => expense.Payment)
            .WithOne()
            .HasForeignKey<PaymentRecord>(payment => payment.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(expense => expense.OwnerId);
        builder.HasIndex(expense => expense.Status);
    }
}
