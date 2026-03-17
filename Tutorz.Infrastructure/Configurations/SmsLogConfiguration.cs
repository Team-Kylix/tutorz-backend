using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class SmsLogConfiguration : IEntityTypeConfiguration<SmsLog>
    {
        public void Configure(EntityTypeBuilder<SmsLog> builder)
        {
            builder.HasKey(s => s.SmsLogId);

            builder.Property(s => s.ReceiverPhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(s => s.MessageContent)
                .IsRequired();

            builder.Property(s => s.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(s => s.Cost)
                .HasColumnType("decimal(18,2)");

            builder.Property(s => s.ErrorMessage)
                .HasMaxLength(500);

            builder.HasOne(s => s.SenderUser)
                .WithMany()
                .HasForeignKey(s => s.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull); // If user is deleted, keep log but set ID null
        }
    }
}
