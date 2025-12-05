using System;
using System.Linq;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(TutorzDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            // Look for any roles. If there are roles, the DB is already seeded.
            if (context.Roles.Any())
            {
                return;
            }

            // If empty, create the default roles
            var roles = new Role[]
            {
                new Role{ RoleId = Guid.NewGuid(), Name="Admin", Description="System Administrator", CreatedDate=DateTime.UtcNow, IsActive=true},
                new Role{ RoleId = Guid.NewGuid(), Name="Tutor", Description="Registered Tutor", CreatedDate=DateTime.UtcNow, IsActive=true},
                new Role{ RoleId = Guid.NewGuid(), Name="Student", Description="Student or Parent", CreatedDate=DateTime.UtcNow, IsActive=true},
                new Role{ RoleId = Guid.NewGuid(), Name="Institute", Description="Educational Institute", CreatedDate=DateTime.UtcNow, IsActive=true}
            };

            foreach (Role r in roles)
            {
                context.Roles.Add(r);
            }

            context.SaveChanges();
        }
    }
}