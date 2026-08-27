using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class MonthlyPlatformCommissionSummaryConfiguration : IEntityTypeConfiguration<MonthlyPlatformCommissionSummary>
    {
        public void Configure(EntityTypeBuilder<MonthlyPlatformCommissionSummary> builder)
        {
            builder.HasKey(m => m.Id);

            // Ensure only one record exists per class per month/year
            builder.HasIndex(m => new { m.ClassId, m.Month, m.Year })
                .IsUnique();

            builder.HasOne(m => m.Class)
                .WithMany()
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Tutor)
                .WithMany()
                .HasForeignKey(m => m.TutorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(m => m.Institute)
                .WithMany()
                .HasForeignKey(m => m.InstituteId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
