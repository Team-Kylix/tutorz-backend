using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class MonthlyUsageSummaryConfiguration : IEntityTypeConfiguration<MonthlyUsageSummary>
    {
        public void Configure(EntityTypeBuilder<MonthlyUsageSummary> builder)
        {
            builder.HasKey(m => m.Id);

            // Ensure a user has only one summary per month/year
            builder.HasIndex(m => new { m.UserId, m.Month, m.Year })
                .IsUnique();

            builder.Property(m => m.UserRole)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
