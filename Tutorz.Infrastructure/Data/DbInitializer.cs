using System;
using System.Collections.Generic;
using System.Linq;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Initialize(TutorzDbContext context)
        {
            // --- Layer 3: Seed MinTokenDate if it doesn't exist ---
            // This row is the backend enforcement for the "forced logout on deploy" strategy.
            // All JWT tokens issued BEFORE this date will be rejected with 401.
            // To force ALL users to re-login: run this SQL against the production DB:
            //   UPDATE AppSettings SET Value = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            //   WHERE [Key] = 'MinTokenDate'
            //
            // Set to Unix epoch (1970-01-01) by default so that NO tokens are ever
            // rejected until you deliberately update it on a deploy.
            if (!context.AppSettings.Any(s => s.Key == "MinTokenDate"))
            {
                context.AppSettings.Add(new AppSetting
                {
                    Key = "MinTokenDate",
                    Value = "1970-01-01T00:00:00Z", // Epoch = no tokens rejected by default
                    UpdatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            // Ensure Database Exists
            //context.Database.EnsureCreated(); When Run database first time, it will create the database and tables based on the models. After that, comment this line to avoid data loss.

            // Check if data already exists
            if (context.Users.Any())
            {
                return;
            }

            var roleAdmin = new Role { RoleId = Guid.NewGuid(), Name = "Admin", Description = "System Administrator" };
            var roleTutor = new Role { RoleId = Guid.NewGuid(), Name = "Tutor", Description = "Registered Tutor" };
            var roleStudent = new Role { RoleId = Guid.NewGuid(), Name = "Student", Description = "Student or Parent" };
            var roleInstitute = new Role { RoleId = Guid.NewGuid(), Name = "Institute", Description = "Educational Institute" };

            context.Roles.AddRange(roleAdmin, roleTutor, roleStudent, roleInstitute);
            context.SaveChanges();

            // Use Fully Qualified Name
            string dummyHash = BCrypt.Net.BCrypt.HashPassword("Test@123");

            // ==================================================
            // 1. ADMIN ACCOUNT (Added)
            // ==================================================
            var userAdmin = new User
            {
                UserId = Guid.NewGuid(),
                Email = "admin@tutorz.com",
                PhoneNumber = "0112223344",
                PasswordHash = dummyHash, // Password is: Test@123
                RoleId = roleAdmin.RoleId,
                IsActive = true
            };

            // Note: If you have a separate 'Admin' entity/table for profile details (like Tutor/Student),
            // uncomment the lines below and ensure 'Admins' DbSet exists in your context.
            /*
            var adminProfile = new Admin
            {
                AdminId = Guid.NewGuid(),
                UserId = userAdmin.UserId,
                FirstName = "System",
                LastName = "Administrator"
            };
            context.Admins.Add(adminProfile);
            */

            // Add Admin to the Users dbset
            context.Users.Add(userAdmin);
            context.SaveChanges();
            // ==================================================
        }
    }
}