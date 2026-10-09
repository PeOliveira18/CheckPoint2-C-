using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento de <see cref="ExpenseCategory"/> com as categorias iniciais.
/// </summary>
internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("EH_EXPENSE_CATEGORIES");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(category => category.Name).IsUnique();

        builder.HasData(
            new ExpenseCategory { Id = 1, Name = "Alimentação" },
            new ExpenseCategory { Id = 2, Name = "Transporte" },
            new ExpenseCategory { Id = 3, Name = "Hospedagem" },
            new ExpenseCategory { Id = 4, Name = "Material de escritório" },
            new ExpenseCategory { Id = 5, Name = "Outros" });
    }
}
