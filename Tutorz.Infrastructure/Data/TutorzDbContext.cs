using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;
using System.Reflection;

namespace Tutorz.Infrastructure.Data
{
    public class TutorzDbContext : DbContext
    {
        public TutorzDbContext(DbContextOptions<TutorzDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Institute> Institutions { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<UserSequence> UserSequences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Ensure RegistrationNumber is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.RegistrationNumber)
                .IsUnique();
        }
    }
}
