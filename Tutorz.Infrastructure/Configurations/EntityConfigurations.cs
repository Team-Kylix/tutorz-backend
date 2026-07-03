using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasIndex(u => u.PhoneNumber).IsUnique();
        }
    }

    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasKey(s => s.StudentId);
            builder.HasOne(s => s.User)
                   .WithMany() // User can have multiple students
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // If Parent User is deleted delete all student profiles
        }
    }
}
