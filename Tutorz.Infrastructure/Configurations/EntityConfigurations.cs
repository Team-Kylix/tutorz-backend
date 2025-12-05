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
            // Ensure Email is unique
            builder.HasIndex(u => u.Email).IsUnique();

            // Ensure Phone is unique
            builder.HasIndex(u => u.PhoneNumber).IsUnique();
        }
    }
}
