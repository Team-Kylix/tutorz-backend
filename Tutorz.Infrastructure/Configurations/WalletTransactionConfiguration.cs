using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.HasKey(wt => wt.Id);

            builder.Property(wt => wt.Type)
                .HasConversion<string>()
                .HasMaxLength(50); // Store enum as string for readability

            builder.HasOne(wt => wt.Wallet)
                .WithMany()
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
