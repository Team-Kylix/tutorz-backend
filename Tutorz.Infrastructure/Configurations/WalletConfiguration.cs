using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.HasKey(w => w.Id);

            // One wallet per user + institute + individual flag combination
            builder.HasIndex(w => new { w.UserId, w.InstituteId, w.IsIndividual }).IsUnique();

            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
