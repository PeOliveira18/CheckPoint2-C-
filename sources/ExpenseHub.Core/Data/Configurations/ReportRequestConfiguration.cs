using System;
using ExpenseHub.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Core.Data.Configurations;

internal sealed class ReportRequestConfiguration : IEntityTypeConfiguration<ReportRequest>
{
    public void Configure(EntityTypeBuilder<ReportRequest> builder)
    {
        builder.ToTable("EH_REPORT_REQUESTS");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Id).ValueGeneratedNever();

        builder.Property(report => report.RequesterId).HasMaxLength(450).IsRequired();
        builder.Property(report => report.Scope).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.FromDate)
            .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
            .HasColumnType("DATE");
        builder.Property(report => report.ToDate)
            .HasConversion(date => date.ToDateTime(TimeOnly.MinValue), value => DateOnly.FromDateTime(value))
            .HasColumnType("DATE");
        builder.Property(report => report.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(report => report.ResultJson).HasMaxLength(2000);
        builder.Property(report => report.ErrorCode).HasMaxLength(50);
        builder.Property(report => report.ErrorMessage).HasMaxLength(500);
        builder.Property(report => report.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(report => report.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(report => new { report.RequesterId, report.CreatedAtUtc });
    }
}
